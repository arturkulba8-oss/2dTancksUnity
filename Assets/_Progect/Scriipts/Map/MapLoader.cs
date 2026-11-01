using UnityEngine;
using UnityEngine.Tilemaps;

public class MapLoader : MonoBehaviour
{
    [SerializeField] TextAsset mapFile;
    [Header("Tilemaps")]
    [SerializeField] Tilemap ground, water, walls, bushes;
    [SerializeField] Transform spawnsRoot;
    [Header("Tiles")]
    [SerializeField] TileBase groundTile, waterTile, brickTile, blockTile, steelTile, bushTile;
    

    // Правая кнопка по заголовку компонента → Build Map
    [ContextMenu("Build Map")]
    public void Build()
    {
        ground.ClearAllTiles(); water.ClearAllTiles(); walls.ClearAllTiles(); bushes.ClearAllTiles();
        for (int i = spawnsRoot.childCount - 1; i >= 0; i--)
        {
            var go = spawnsRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        var lines = mapFile.text.Replace("\r", "").Split('\n');
        int h = 0, w = 0;
        foreach (var l in lines) if (l.Length > 0) { h++; w = Mathf.Max(w, l.Length); }

        int enemyCount = 0, row = 0;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            for (int c = 0; c < line.Length; c++)
            {
                var cell = new Vector3Int(c - w / 2, (h - 1 - row) - h / 2, 0);
                ground.SetTile(cell, groundTile);                       // под всем лежит земля
                switch (line[c])
                {
                    case '~': water.SetTile(cell, waterTile); break;
                    case 'B': walls.SetTile(cell, brickTile); break;
                    case 'S': walls.SetTile(cell, blockTile); break;
                    case '#': walls.SetTile(cell, steelTile); break;
                    case 'g': bushes.SetTile(cell, bushTile); break;
                    case 'P': AddSpawn("Spawn_Player", cell); break;
                    case 'E': AddSpawn("Spawn_Enemy_" + (++enemyCount), cell); break;
                }
            }
            row++;
        }

#if UNITY_EDITOR
        foreach (var t in new[] { ground, water, walls, bushes }) UnityEditor.EditorUtility.SetDirty(t);
        UnityEditor.EditorUtility.SetDirty(spawnsRoot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void AddSpawn(string name, Vector3Int cell)
    {
        var go = new GameObject(name);
        go.transform.SetParent(spawnsRoot);
        go.transform.position = ground.GetCellCenterWorld(cell);
    }
}