using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSpawner : MonoBehaviour
{
    [Header("Точки (объект Spawns, который создал MapLoader)")]
    [SerializeField] Transform spawnsRoot;

    [Header("Игрок")]
    [SerializeField] GameObject playerPrefab;          // Tank_Player
    [SerializeField] float minDistanceFromPlayer = 10f;
    [Header("Враги")]
    [SerializeField] GameObject[] enemyPrefabs;        // Tank_Enemy_Red, Tank_Enemy_Sand...
    [SerializeField] bool randomEnemyType = true;
    [SerializeField] int totalEnemies = 10;            // сколько врагов за весь уровень
    public int TotalEnemies => totalEnemies;
    [SerializeField] int maxAliveAtOnce = 4;           // сколько одновременно на карте
    [SerializeField] float spawnInterval = 3f;         // пауза между появлениями

    [Header("Проверка свободного места")]
    [SerializeField] Vector2 checkSize = new Vector2(0.8f, 0.9f);   // чуть меньше самого танка
    [SerializeField] LayerMask blockMask = ~0;         // что считается препятствием



    readonly List<Transform> enemySpawns = new();
    readonly List<GameObject> aliveEnemies = new();
    readonly Collider2D[] buffer = new Collider2D[8];
    Transform playerSpawn;
    Transform cachedPlayerTransform; // Кэшируем ссылку на игрока для быстрого доступа
    int spawnedCount;

    public event System.Action AllEnemiesDefeated;

    public int EnemiesLeft
   {
       get
       {
           aliveEnemies.RemoveAll(e => e == null);      // убрать уничтоженных
           return (totalEnemies - spawnedCount) + aliveEnemies.Count;
       }
   }
    bool warned;
    void Start()
    {
        int levelId = SaveGameSettings.Instance.selectedLevelId;
        switch (levelId)
        {
            case 1:
                totalEnemies = 3;
                maxAliveAtOnce = 1;
                break;
            case 2:
                totalEnemies = 6;
                maxAliveAtOnce = 2;
                break;
            case 3:
                totalEnemies = 9;
                maxAliveAtOnce = 2;
                break;
            case 4:
                totalEnemies = 15;
                maxAliveAtOnce = 2;
                break;
            case 5:
                totalEnemies = 18;
                maxAliveAtOnce = 2;
                break;
            case 6:
                totalEnemies = 20;
                maxAliveAtOnce = 2;
                break;
            case 7:
                totalEnemies = 22;
                maxAliveAtOnce = 2;
                break;
            case 8:
                totalEnemies = 24;
                maxAliveAtOnce = 3;
                break;
            case 9:
                totalEnemies = 26;
                maxAliveAtOnce = 3;
                break;
            case 10:
                totalEnemies = 28;
                maxAliveAtOnce = 3;
                break;
            case 11:
                totalEnemies = 30;
                maxAliveAtOnce = 3;
                break;
            case 12:
                totalEnemies = 32;
                maxAliveAtOnce = 3;
                break;
            case 13:
                totalEnemies = 34;
                maxAliveAtOnce = 3;
                break;
            case 14:
                totalEnemies = 36;
                maxAliveAtOnce = 3;
                break;
            case 15:
                totalEnemies = 38;
                maxAliveAtOnce = 3;
                break;
            case 16:
                totalEnemies = 40;
                maxAliveAtOnce = 3;
                break;
            default:
                break;
        }
        
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
        if (existing != null) 
        { 
            existing.transform.position = playerSpawn.position; 
            cachedPlayerTransform = existing.transform;
            return; 
        }

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

       // всех выпустили: ждём, пока перебьют последних
       var poll = new WaitForSeconds(0.25f);
       while (true)
       {
           aliveEnemies.RemoveAll(e => e == null);
           if (aliveEnemies.Count == 0) break;
           yield return poll;
       }
       AllEnemiesDefeated?.Invoke();                        // ПОБЕДА: все враги убиты
   }

    public void StopSpawning() { StopAllCoroutines(); }

    void TrySpawnEnemy()
   {
       if (enemyPrefabs.Length == 0 || enemySpawns.Count == 0) return;

       if (cachedPlayerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) cachedPlayerTransform = playerObj.transform;
        }

       Collider2D lastBlocker = null;
       int start = Random.Range(0, enemySpawns.Count);
       for (int i = 0; i < enemySpawns.Count; i++)
       {
           var point = enemySpawns[(start + i) % enemySpawns.Count];

            if (cachedPlayerTransform != null)
            {
                float sqrDist = (point.position - cachedPlayerTransform.position).sqrMagnitude;
                // Используем sqrMagnitude вместо Vector2.Distance для оптимизации (чтобы не считать квадратный корень)
                if (sqrDist < minDistanceFromPlayer * minDistanceFromPlayer)
                {
                    continue; // Эта точка слишком близко к игроку, пропускаем её
                }
            }

           if (!IsFree(point.position, out lastBlocker)) continue;

           var prefab = randomEnemyType
               ? enemyPrefabs[Random.Range(0, enemyPrefabs.Length)]
               : enemyPrefabs[spawnedCount % enemyPrefabs.Length];

           aliveEnemies.Add(Instantiate(prefab, point.position, Quaternion.identity));
           spawnedCount++;
           warned = false;
           return;
       }

       // все точки заняты: скажем об этом один раз и назовём виновника
       if (!warned)
       {
           warned = true;
           Debug.LogWarning("Враг не появился: все точки заняты. Мешает: " + (lastBlocker != null ? lastBlocker.name : "?"), this);
       }
   }

   bool IsFree(Vector2 pos, out Collider2D blocker)
   {
       var filter = new ContactFilter2D { useTriggers = false };   // вода и листья не мешают
       filter.SetLayerMask(blockMask);
       int n = Physics2D.OverlapBox(pos, checkSize, 0f, filter, buffer);
       blocker = n > 0 ? buffer[0] : null;
       return n == 0;
   }
}