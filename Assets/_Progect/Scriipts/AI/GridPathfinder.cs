using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Один шаг найденного пути
public struct PathStep
{
    public Vector3Int cell;
    public Vector3 world;     // центр клетки в мире
    public int wallHits;      // > 0: в клетке стена, которую надо пробить (столько попаданий)
}

// A* по клеткам Tilemap. Висит на одном объекте сцены (AIManager).
public class GridPathfinder : MonoBehaviour
{
    [Serializable]
    public class CostLayer
    {
        public Tilemap map;
        public float cost = 2f;    // во сколько раз клетка «дороже» обычной (вода 2, лёд 0.7)
    }

    public static GridPathfinder Instance { get; private set; }

    [SerializeField] Tilemap ground;
    [SerializeField] WallTilemap walls;
    [SerializeField] Tilemap bushes;               // необязательно: листья ухудшают видимость
    [SerializeField] CostLayer[] costLayers;       // вода, лёд и т. п.

    public Tilemap Ground => ground;
    public WallTilemap Walls => walls;
    public Tilemap Bushes => bushes;

    BoundsInt bounds;
    int width, height;
    bool[] groundOk;
    float[] g;
    int[] parent;
    int[] seenStamp;
    int[] closedStamp;
    int stamp;

    readonly List<int> heapNode = new List<int>();
    readonly List<float> heapKey = new List<float>();

    static readonly Vector3Int[] dirs =
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
    };

    void Awake()
    {
        Instance = this;
        // слой стен выбранной темы (MapLoader выключает остальные); поле в Inspector не нужно
        var activeWalls = FindFirstObjectByType<WallTilemap>();
        if (activeWalls != null) walls = activeWalls;
        Rebuild();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Запоминает, где вообще есть земля. Вызовите, если карту строите уже во время игры.
    public void Rebuild()
    {
        if (ground == null) { Debug.LogError("GridPathfinder: не указан слой Ground", this); return; }
        ground.CompressBounds();
        bounds = ground.cellBounds;
        width = bounds.size.x;
        height = bounds.size.y;
        int n = width * height;

        groundOk = new bool[n];
        g = new float[n];
        parent = new int[n];
        seenStamp = new int[n];
        closedStamp = new int[n];
        stamp = 0;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                groundOk[y * width + x] = ground.HasTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0));
    }

    int Idx(Vector3Int c) { return (c.y - bounds.yMin) * width + (c.x - bounds.xMin); }

    bool InBounds(Vector3Int c)
    {
        return c.x >= bounds.xMin && c.x < bounds.xMin + width && c.y >= bounds.yMin && c.y < bounds.yMin + height;
    }

    Vector3Int CellFromIdx(int idx) { return new Vector3Int(bounds.xMin + idx % width, bounds.yMin + idx / width, 0); }

    // Можно ли просто встать в эту клетку (земля есть, стены нет)
    public bool IsWalkable(Vector3Int cell)
    {
        if (groundOk == null || !InBounds(cell) || !groundOk[Idx(cell)]) return false;
        return walls == null || walls.RemainingHits(cell) == 0;
    }

    // Стоимость захода в клетку. false = пройти нельзя.
    bool CellCost(Vector3Int cell, float moveSpeed, Func<int, float> timeToDestroy, out float cost)
    {
        cost = 0f;
        if (!groundOk[Idx(cell)]) return false;

        cost = 1f;
        if (costLayers != null)
            foreach (var l in costLayers)
                if (l.map != null && l.map.HasTile(cell)) cost *= l.cost;

        if (walls != null)
        {
            int rh = walls.RemainingHits(cell);
            if (rh < 0) return false;                    // неразрушаемая стена
            if (rh > 0)
            {
                if (timeToDestroy == null) return false; // пробивать нельзя
                cost += timeToDestroy(rh) * moveSpeed;   // секунды -> «клетки пути»
            }
        }
        return true;
    }

    float Heuristic(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * 0.5f;   // не больше самой дешёвой клетки
    }

    // timeToDestroy(n) = сколько секунд нужно, чтобы выстрелами пробить стену из n попаданий.
    // Если null, все стены считаются непроходимыми.
    public bool FindPath(Vector3 fromWorld, Vector3 toWorld, float moveSpeed, Func<int, float> timeToDestroy, List<PathStep> result)
    {
        result.Clear();
        if (groundOk == null) return false;

        var start = ground.WorldToCell(fromWorld); start.z = 0;
        var goal = ground.WorldToCell(toWorld); goal.z = 0;
        if (!InBounds(start) || !InBounds(goal)) return false;
        if (!CellCost(goal, moveSpeed, timeToDestroy, out _)) return false;

        stamp++;
        heapNode.Clear();
        heapKey.Clear();

        int s = Idx(start);
        int t = Idx(goal);
        g[s] = 0f;
        parent[s] = -1;
        seenStamp[s] = stamp;
        HeapPush(s, Heuristic(start, goal));

        while (heapNode.Count > 0)
        {
            int cur = HeapPop();
            if (closedStamp[cur] == stamp) continue;
            closedStamp[cur] = stamp;

            if (cur == t)
            {
                Reconstruct(s, t, result);
                return true;
            }

            var cc = CellFromIdx(cur);
            for (int d = 0; d < dirs.Length; d++)
            {
                var nb = cc + dirs[d];
                if (!InBounds(nb)) continue;
                int ni = Idx(nb);
                if (closedStamp[ni] == stamp) continue;
                if (!CellCost(nb, moveSpeed, timeToDestroy, out float c)) continue;

                float ng = g[cur] + c;
                if (seenStamp[ni] != stamp || ng < g[ni])
                {
                    seenStamp[ni] = stamp;
                    g[ni] = ng;
                    parent[ni] = cur;
                    HeapPush(ni, ng + Heuristic(nb, goal));
                }
            }
        }
        return false;
    }

    void Reconstruct(int s, int t, List<PathStep> result)
    {
        int cur = t;
        while (cur != s && cur != -1)
        {
            var cell = CellFromIdx(cur);
            result.Add(new PathStep
            {
                cell = cell,
                world = ground.GetCellCenterWorld(cell),
                wallHits = walls != null ? Mathf.Max(0, walls.RemainingHits(cell)) : 0
            });
            cur = parent[cur];
        }
        result.Reverse();
    }

    // ---- минимальная двоичная куча (минимум наверху) ----
    void HeapPush(int node, float key)
    {
        heapNode.Add(node);
        heapKey.Add(key);
        int i = heapNode.Count - 1;
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heapKey[p] <= heapKey[i]) break;
            HeapSwap(i, p);
            i = p;
        }
    }

    int HeapPop()
    {
        int top = heapNode[0];
        int last = heapNode.Count - 1;
        heapNode[0] = heapNode[last];
        heapKey[0] = heapKey[last];
        heapNode.RemoveAt(last);
        heapKey.RemoveAt(last);

        int count = heapNode.Count;
        int i = 0;
        while (true)
        {
            int l = 2 * i + 1;
            int r = l + 1;
            int m = i;
            if (l < count && heapKey[l] < heapKey[m]) m = l;
            if (r < count && heapKey[r] < heapKey[m]) m = r;
            if (m == i) break;
            HeapSwap(i, m);
            i = m;
        }
        return top;
    }

    void HeapSwap(int a, int b)
    {
        int tn = heapNode[a]; heapNode[a] = heapNode[b]; heapNode[b] = tn;
        float tk = heapKey[a]; heapKey[a] = heapKey[b]; heapKey[b] = tk;
    }
}