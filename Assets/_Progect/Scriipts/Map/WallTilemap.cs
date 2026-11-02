using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class WallType
{
    public TileBase tile;              // исходный тайл
    public int hp = 1;                 // 0 и меньше = неразрушаемая
    public TileBase[] damagedStates;   // тайлы после 1-го, 2-го... попадания
    public Color breakColor = Color.white;   // цвет обломков
}

[RequireComponent(typeof(Tilemap))]
public class WallTilemap : MonoBehaviour
{
    [SerializeField] WallType[] types;
    [SerializeField] GameObject breakEffect;     // префаб анимации разрушения (необязательно)

    // Срабатывает, когда стену повредили или разрушили: ИИ пересчитывает пути
    public event Action WallsChanged;

    Tilemap map;
    TilemapCollider2D tilemapCollider;
    CompositeCollider2D composite;
    readonly Dictionary<TileBase, WallType> lookup = new Dictionary<TileBase, WallType>();
    readonly Dictionary<Vector3Int, int> hits = new Dictionary<Vector3Int, int>();
    static readonly float[] probe = { 0.5f, 0.25f, 0f };

    public Tilemap Map
    {
        get { if (map == null) map = GetComponent<Tilemap>(); return map; }
    }

    void Awake()
    {
        tilemapCollider = GetComponent<TilemapCollider2D>();
        composite = GetComponent<CompositeCollider2D>();
        BuildLookup();
    }

    void BuildLookup()
    {
        lookup.Clear();
        if (types == null) return;
        foreach (var t in types)
        {
            if (t.tile != null) lookup[t.tile] = t;
            if (t.damagedStates != null)
                foreach (var d in t.damagedStates)
                    if (d != null) lookup[d] = t;
        }
    }

    // Сколько ударов осталось до разрушения клетки:
    //  0  - в клетке нет стены
    // -1  - стена неразрушаемая (сталь и любые неизвестные тайлы)
    // >0  - сколько попаданий ещё нужно
    public int RemainingHits(Vector3Int cell)
    {
        var tile = Map.GetTile(cell);
        if (tile == null) return 0;
        if (lookup.Count == 0) BuildLookup();
        if (!lookup.TryGetValue(tile, out var type) || type.hp <= 0) return -1;
        hits.TryGetValue(cell, out int h);
        return Mathf.Max(1, type.hp - h);
    }

    // point = место попадания пули, dir = направление её полёта
    public bool TryHit(Vector2 point, Vector2 dir)
    {
        foreach (float o in probe)
        {
            var cell = Map.WorldToCell(point + dir.normalized * o);
            var tile = Map.GetTile(cell);
            if (tile == null) continue;
            Apply(cell, tile);
            return true;
        }
        return false;
    }

    void Apply(Vector3Int cell, TileBase tile)
    {
        if (lookup.Count == 0) BuildLookup();
        if (!lookup.TryGetValue(tile, out var type) || type.hp <= 0) return;   // сталь и неизвестные тайлы

        hits.TryGetValue(cell, out int h);
        h++;

        if (h >= type.hp)
        {
            Map.SetTile(cell, null);
            RefreshCollider(cell);
            hits.Remove(cell);

            if (breakEffect != null)
            {
                var fx = Instantiate(breakEffect, Map.GetCellCenterWorld(cell), Quaternion.identity);
                var sr = fx.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = type.breakColor;
            }
        }
        else
        {
            hits[cell] = h;
            if (type.damagedStates != null && h - 1 < type.damagedStates.Length && type.damagedStates[h - 1] != null)
                Map.SetTile(cell, type.damagedStates[h - 1]);
        }

        WallsChanged?.Invoke();
    }

    // Без этого коллайдер иногда остаётся «призраком» на месте разрушенной клетки
    void RefreshCollider(Vector3Int cell)
    {
        Map.RefreshTile(cell);
        if (tilemapCollider != null) tilemapCollider.ProcessTilemapChanges();
        if (composite != null) composite.GenerateGeometry();
    }
}