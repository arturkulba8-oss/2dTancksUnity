using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-100)]      // карта собирается РАНЬШЕ остальных скриптов сцены (камера, A*, спавнер)
public class MapLoader : MonoBehaviour
{
    [Header("Уровни (текстовые карты)")]
    [SerializeField] private TextAsset[] mapFiles;

    [Header("Tilemaps")]
    [SerializeField] Tilemap ground, water, ice, bushes;
    [SerializeField] private Tilemap[] walls;      // по одному слою стен на каждую тему (индекс = номер темы)
    [SerializeField] Transform spawnsRoot;

    [Header("Tiles (индекс = номер темы: 0 Классика, 1 Зима, 2 Пустыня, 3 Чернобыль)")]
    [SerializeField] TileBase[] groundTile, waterTile, iceTile, brickTile, blockTile, steelTile, bushTile;

    [Header("Запуск")]
    [SerializeField] bool buildOnStart = true;     // собирать карту при входе в сцену
    [Header("Для теста: если сцену запустили напрямую, без меню")]
    [SerializeField] int testMapId = 0;
    [SerializeField] int testLevelId = 1;

    void Awake()
    {
        if (buildOnStart) Build();
    }

    // Правая кнопка по заголовку компонента → Build Map
    [ContextMenu("Build Map")]
    public void Build()
    {
        // 1. Что выбрано в меню (если меню не проходили, берём тестовые значения)
        int mapId = testMapId;
        int levelId = testLevelId;
        if (SaveGameSettings.Instance != null)
        {
            mapId = SaveGameSettings.Instance.selectedMapId;
            levelId = SaveGameSettings.Instance.selectedLevelId;
        }
        mapId = Mathf.Max(0, mapId);

        if (mapFiles == null || mapFiles.Length == 0)
        {
            Debug.LogError("MapLoader: список mapFiles пуст", this);
            return;
        }
        int levelIndex = Mathf.Clamp(levelId - 1, 0, mapFiles.Length - 1);   // уровни нумеруются с 1
        TextAsset file = mapFiles[levelIndex];
        if (file == null)
        {
            Debug.LogError("MapLoader: в mapFiles[" + levelIndex + "] пусто", this);
            return;
        }

        // 2. Очистка: все слои и точки появления
        ClearAll();

        // 3. Стены: включаем слой выбранной темы, остальные выключены
        Tilemap wallMap = null;
        if (walls != null)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                if (walls[i] == null) continue;
                bool active = (i == mapId);
                walls[i].gameObject.SetActive(active);       // сначала включаем, потом ставим тайлы
                if (active) wallMap = walls[i];
            }
        }
        if (wallMap == null) Debug.LogWarning("MapLoader: для темы " + mapId + " нет слоя стен в массиве walls", this);

        // 4. Читаем карту
        var lines = file.text.Replace("\r", "").Split('\n');
        int h = 0, w = 0;
        foreach (var l in lines) if (l.Length > 0) { h++; w = Mathf.Max(w, l.Length); }

        int enemyCount = 0, row = 0;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            for (int c = 0; c < line.Length; c++)
            {
                var cell = new Vector3Int(c - w / 2, (h - 1 - row) - h / 2, 0);
                Put(ground, cell, Pick(groundTile, mapId));              // под всем лежит земля
                switch (line[c])
                {
                    case '~': Put(water, cell, Pick(waterTile, mapId)); break;
                    case 'I': Put(ice, cell, Pick(iceTile, mapId)); break;
                    case 'B': Put(wallMap, cell, Pick(brickTile, mapId)); break;
                    case 'S': Put(wallMap, cell, Pick(blockTile, mapId)); break;
                    case '#': Put(wallMap, cell, Pick(steelTile, mapId)); break;
                    case 'g': Put(bushes, cell, Pick(bushTile, mapId)); break;
                    case 'P': AddSpawn("Spawn_Player", cell); break;
                    case 'E': AddSpawn("Spawn_Enemy_" + (++enemyCount), cell); break;
                }
            }
            row++;
        }

        Debug.Log("MapLoader: уровень " + (levelIndex + 1) + ", тема " + mapId);
        MarkDirty();
    }

    // Убрать всё, что нарисовано (можно вызвать из меню компонента, чтобы не хранить карту в сцене)
    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        ClearAll();
        MarkDirty();
    }

    void ClearAll()
    {
        if (ground != null) ground.ClearAllTiles();
        if (water != null) water.ClearAllTiles();
        if (ice != null) ice.ClearAllTiles();
        if (bushes != null) bushes.ClearAllTiles();
        if (walls != null)
            foreach (var wm in walls)
                if (wm != null) wm.ClearAllTiles();     // чистим слои ВСЕХ тем, а не только выбранной

        if (spawnsRoot != null)
        {
            for (int i = spawnsRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(spawnsRoot.GetChild(i).gameObject);   // сразу, иначе старые точки попадут в спавнер
        }
    }

    static TileBase Pick(TileBase[] arr, int id)
    {
        return (arr != null && id >= 0 && id < arr.Length) ? arr[id] : null;
    }

    static void Put(Tilemap map, Vector3Int cell, TileBase tile)
    {
        if (map != null && tile != null) map.SetTile(cell, tile);
    }

    void AddSpawn(string name, Vector3Int cell)
    {
        var go = new GameObject(name);
        go.transform.SetParent(spawnsRoot);
        go.transform.position = ground.GetCellCenterWorld(cell);
    }

    void MarkDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;     // в игре сцену «грязнить» не нужно
        if (ground != null) UnityEditor.EditorUtility.SetDirty(ground);
        if (water != null) UnityEditor.EditorUtility.SetDirty(water);
        if (ice != null) UnityEditor.EditorUtility.SetDirty(ice);
        if (bushes != null) UnityEditor.EditorUtility.SetDirty(bushes);
        if (walls != null)
            foreach (var wm in walls)
                if (wm != null) UnityEditor.EditorUtility.SetDirty(wm);
        if (spawnsRoot != null) UnityEditor.EditorUtility.SetDirty(spawnsRoot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}