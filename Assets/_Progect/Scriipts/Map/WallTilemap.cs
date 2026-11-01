using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class WallType
{
    public TileBase tile;              // исходный тайл
    public int hp = 1;                 // 0 и меньше = неразрушаемая (сталь можно не добавлять вовсе)
    public TileBase[] damagedStates;   // тайлы после 1-го, 2-го... попадания
}

[RequireComponent(typeof(Tilemap))]
public class WallTilemap : MonoBehaviour
{
    [SerializeField] WallType[] types;
    [SerializeField] GameObject breakEffect;     // префаб анимации разрушения (необязательно)

    Tilemap map;

    TilemapCollider2D tilemapCollider;
    CompositeCollider2D composite;

    readonly Dictionary<TileBase, WallType> lookup = new();
    readonly Dictionary<Vector3Int, int> hits = new();
    static readonly float[] probe = { 0.5f, 0.25f, 0f };

    void Awake()
    {
        map = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        composite = GetComponent<CompositeCollider2D>();
        foreach (var t in types)
        {
            if (t.tile != null) lookup[t.tile] = t;
            if (t.damagedStates != null)
                foreach (var d in t.damagedStates) if (d != null) lookup[d] = t;
        }
    }

    // point = место попадания пули, dir = направление её полёта
    public bool TryHit(Vector2 point, Vector2 dir)
    {
        foreach (float o in probe)
        {
            var cell = map.WorldToCell(point + dir.normalized * o);
            var tile = map.GetTile(cell);
            if (tile == null) continue;
            Apply(cell, tile);
            return true;
        }
        return false;
    }

    void Apply(Vector3Int cell, TileBase tile)
    {
        if (!lookup.TryGetValue(tile, out var type) || type.hp <= 0) return;   // сталь и неизвестные тайлы

        hits.TryGetValue(cell, out int h);
        h++;

        if (h >= type.hp)
        {
            map.SetTile(cell, null);
            RefreshCollider(cell);
            hits.Remove(cell);
            hits.Remove(cell);
            if (breakEffect != null) Instantiate(breakEffect, map.GetCellCenterWorld(cell), Quaternion.identity);
        }
        else
        {
            hits[cell] = h;
            if (type.damagedStates != null && h - 1 < type.damagedStates.Length && type.damagedStates[h - 1] != null)
                map.SetTile(cell, type.damagedStates[h - 1]);
        }
    }

    void RefreshCollider(Vector3Int cell)
    {
        map.RefreshTile(cell);
        if (tilemapCollider != null) tilemapCollider.ProcessTilemapChanges();   // обновить форму сразу
        if (composite != null) composite.GenerateGeometry();                    // пересобрать склеенную форму
    }
}