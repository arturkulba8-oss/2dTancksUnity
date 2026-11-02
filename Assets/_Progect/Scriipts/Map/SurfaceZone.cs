using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class SurfaceZone : MonoBehaviour
{
    [SerializeField] float speedMultiplier = 0.5f;   // вода 0.5, лёд 1.5

    Tilemap map;
    static readonly List<SurfaceZone> zones = new List<SurfaceZone>();

    void Awake() { map = GetComponent<Tilemap>(); }
    void OnEnable() { zones.Add(this); }
    void OnDisable() { zones.Remove(this); }

    // Множитель скорости в этой точке мира (1 = обычная земля)
    public static float GetMultiplier(Vector2 worldPos)
    {
        float result = 1f;
        foreach (var z in zones)
            if (z.map.HasTile(z.map.WorldToCell(worldPos))) result *= z.speedMultiplier;
        return result;
    }
}