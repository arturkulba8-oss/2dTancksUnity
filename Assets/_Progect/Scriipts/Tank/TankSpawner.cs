using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSpawner : MonoBehaviour
{
    [Header("Точки (объект Spawns, который создал MapLoader)")]
    [SerializeField] Transform spawnsRoot;

    [Header("Игрок")]
    [SerializeField] GameObject playerPrefab;          // Tank_Player

    [Header("Враги")]
    [SerializeField] GameObject[] enemyPrefabs;        // Tank_Enemy_Red, Tank_Enemy_Sand...
    [SerializeField] bool randomEnemyType = true;
    [SerializeField] int totalEnemies = 10;            // сколько врагов за весь уровень
    [SerializeField] int maxAliveAtOnce = 4;           // сколько одновременно на карте
    [SerializeField] float spawnInterval = 3f;         // пауза между появлениями

    [Header("Проверка свободного места")]
    [SerializeField] float checkRadius = 0.7f;
    [SerializeField] LayerMask blockMask = ~0;         // что считается препятствием

    readonly List<Transform> enemySpawns = new();
    readonly List<GameObject> aliveEnemies = new();
    readonly Collider2D[] buffer = new Collider2D[8];
    Transform playerSpawn;
    int spawnedCount;

    public int EnemiesLeft => totalEnemies - spawnedCount + aliveEnemies.Count;   // осталось уничтожить

    void Start()
    {
        CollectSpawns();
        SpawnPlayer();
        StartCoroutine(EnemyRoutine());
    }

    void CollectSpawns()
    {
        enemySpawns.Clear();
        foreach (Transform t in spawnsRoot)
        {
            if (t.name.StartsWith("Spawn_Player")) playerSpawn = t;
            else if (t.name.StartsWith("Spawn_Enemy")) enemySpawns.Add(t);
        }
        if (playerSpawn == null) Debug.LogWarning("Нет точки Spawn_Player: нажмите Build Map в MapLoader", this);
    }

    void SpawnPlayer()
    {
        if (playerSpawn == null) return;

        var existing = GameObject.FindWithTag("Player");    // если игрок уже стоит в сцене, просто переносим
        if (existing != null) { existing.transform.position = playerSpawn.position; return; }

        if (playerPrefab != null) Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
    }

    IEnumerator EnemyRoutine()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (spawnedCount < totalEnemies)
        {
            aliveEnemies.RemoveAll(e => e == null);          // убрать уничтоженных
            if (aliveEnemies.Count < maxAliveAtOnce) TrySpawnEnemy();
            yield return wait;
        }
    }

    void TrySpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || enemySpawns.Count == 0) return;

        // перебираем точки в случайном порядке и берём первую свободную
        int start = Random.Range(0, enemySpawns.Count);
        for (int i = 0; i < enemySpawns.Count; i++)
        {
            var point = enemySpawns[(start + i) % enemySpawns.Count];
            if (!IsFree(point.position)) continue;

            var prefab = randomEnemyType
                ? enemyPrefabs[Random.Range(0, enemyPrefabs.Length)]
                : enemyPrefabs[spawnedCount % enemyPrefabs.Length];

            aliveEnemies.Add(Instantiate(prefab, point.position, Quaternion.identity));
            spawnedCount++;
            return;
        }
        // все точки заняты: попробуем на следующем тике
    }

    bool IsFree(Vector2 pos)
    {
        var filter = new ContactFilter2D { useTriggers = false };   // вода и листья не мешают
        filter.SetLayerMask(blockMask);
        return Physics2D.OverlapCircle(pos, checkRadius, filter, buffer) == 0;
    }
}