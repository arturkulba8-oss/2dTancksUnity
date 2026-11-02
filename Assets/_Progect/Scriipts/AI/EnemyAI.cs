using System.Collections.Generic;
using UnityEngine;

public enum AIState { Patrol, Chase, Attack, BreakWall }

// Мозг вражеского танка: конечный автомат Patrol -> Chase -> Attack (+ BreakWall).
// Сам ничего не двигает: пишет MoveInput / AimInput и вызывает Fire(), как PlayerTankInput.
[RequireComponent(typeof(TankMovement))]
public class EnemyAI : MonoBehaviour
{
    [Header("Обзор и атака")]
    [SerializeField] float viewRange = 10f;          // с какого расстояния замечает игрока
    [SerializeField] float attackRange = 7f;         // с какого расстояния начинает стрелять
    [SerializeField, Range(0.1f, 1f)] float bushViewMultiplier = 0.35f;   // во сколько раз ближе видит игрока в листве
    [SerializeField] float loseSightTime = 3f;       // сколько ищет игрока после потери из виду
    [SerializeField] float minKeepDistance = 3f;     // ближе этого отступает
    [SerializeField] float aimTolerance = 6f;        // допуск наведения башни (градусы)
    [SerializeField] float reactionDelay = 0.3f;     // пауза между «увидел» и первым выстрелом

    [Header("Движение")]
    [SerializeField] float nodeReachDistance = 0.25f;   // насколько близко подъехать к центру клетки
    [SerializeField] float breakDistance = 1.35f;       // с какого расстояния начинает стрелять в стену
    [SerializeField] float repathInterval = 0.6f;       // как часто пересчитывать путь в погоне
    [SerializeField] float separationRadius = 1.8f;     // ближе этого враги расталкиваются
    [SerializeField] float separationWeight = 1.2f;     // сила расталкивания
    [SerializeField] float stuckTime = 1.2f;            // сколько стоять на месте, чтобы считаться застрявшим

    [Header("Патруль")]
    [SerializeField] float patrolRadius = 8f;
    [SerializeField] float patrolWait = 1.5f;

    [Header("Отладка")]
    [SerializeField] bool drawGizmos = true;

    const float SenseInterval = 0.1f;
    const float BushMinSight = 2f;

    public AIState State => state;

    // ссылки
    TankMovement movement;
    TankReload reload;
    TankShooter shooter;
    TurretAim turret;
    GridPathfinder pathfinder;
    Transform player;
    float nextPlayerSearch;

    // состояние
    AIState state = AIState.Patrol;
    bool canSee;
    float distToPlayer;
    float timeSinceSeen = 999f;
    Vector3 lastKnownPos;

    // путь
    readonly List<PathStep> path = new List<PathStep>();
    int pathIndex;
    bool needRepath;
    float repathTimer;
    Vector3Int wallCell;

    // таймеры
    float senseTimer;
    float waitTimer;
    float fireDelay;
    float lastReloadDuration = 2f;

    // застревание
    Vector2 stuckPos;
    float stuckSample;
    float stuckAccum;

    // луч
    ContactFilter2D filter;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    static readonly List<EnemyAI> All = new List<EnemyAI>();

    float MoveSpeed { get { return movement.MoveSpeed; } }

    void Awake()
    {
        movement = GetComponent<TankMovement>();
        reload = GetComponent<TankReload>();
        shooter = GetComponent<TankShooter>();
        turret = GetComponentInChildren<TurretAim>();

        filter = new ContactFilter2D();
        filter.useTriggers = false;        // вода и листья лучу не мешают
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        pathfinder = GridPathfinder.Instance;
        if (pathfinder == null)
        {
            Debug.LogError("EnemyAI: на сцене нет GridPathfinder (объект AIManager)", this);
            enabled = false;
            return;
        }
        if (pathfinder.Walls != null) pathfinder.Walls.WallsChanged += OnWallsChanged;
        if (reload != null) reload.ReloadStarted += OnReloadStarted;
        stuckPos = transform.position;
    }

    void OnDestroy()
    {
        if (pathfinder != null && pathfinder.Walls != null) pathfinder.Walls.WallsChanged -= OnWallsChanged;
        if (reload != null) reload.ReloadStarted -= OnReloadStarted;
    }

    void OnWallsChanged() { needRepath = true; }
    void OnReloadStarted(float duration) { lastReloadDuration = duration; }

    void Update()
    {
        if (!EnsurePlayer()) { SetInputs(Vector2.zero, Vector2.zero); return; }

        senseTimer -= Time.deltaTime;
        if (senseTimer <= 0f) { senseTimer = SenseInterval; Sense(); }

        switch (state)
        {
            case AIState.Patrol:    TickPatrol();    break;
            case AIState.Chase:     TickChase();     break;
            case AIState.Attack:    TickAttack();    break;
            case AIState.BreakWall: TickBreakWall(); break;
        }
    }

    // ------------------------------------------------------------ восприятие

    bool EnsurePlayer()
    {
        if (player != null) return true;
        if (Time.time < nextPlayerSearch) return false;
        nextPlayerSearch = Time.time + 0.5f;
        var p = GameObject.FindWithTag("Player");
        player = p != null ? p.transform : null;
        return player != null;
    }

    Vector2 Origin()
    {
        return turret != null ? (Vector2)turret.transform.position : (Vector2)transform.position;
    }

    void Sense()
    {
        Vector2 origin = Origin();
        Vector2 to = (Vector2)player.position - origin;
        distToPlayer = to.magnitude;

        float range = viewRange;
        if (PlayerInBush()) range = Mathf.Max(BushMinSight, viewRange * bushViewMultiplier);

        canSee = distToPlayer <= range && HasClearShot(origin, to);

        if (canSee)
        {
            lastKnownPos = player.position;
            timeSinceSeen = 0f;
        }
        else
        {
            timeSinceSeen += SenseInterval;
        }
    }

    bool PlayerInBush()
    {
        var b = pathfinder.Bushes;
        return b != null && b.HasTile(b.WorldToCell(player.position));
    }

    // true, если первый «чужой» коллайдер на линии огня это игрок
    bool HasClearShot(Vector2 origin, Vector2 to)
    {
        float dist = to.magnitude;
        if (dist < 0.01f) return true;

        hits.Clear();
        int n = Physics2D.Raycast(origin, to / dist, filter, hits, dist);
        for (int i = 0; i < n; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;
            var rb = col.attachedRigidbody;
            if (rb != null && rb.transform == transform) continue;   // свой танк
            return rb != null && rb.transform == player;             // игрок = чисто, всё остальное = преграда
        }
        return true;
    }

    // ------------------------------------------------------------ переходы

    void EnterPatrol()
    {
        state = AIState.Patrol;
        path.Clear();
        pathIndex = 0;
        waitTimer = 0.5f;
    }

    void EnterChase()
    {
        state = AIState.Chase;
        path.Clear();
        pathIndex = 0;
        needRepath = true;
    }

    void EnterAttack()
    {
        state = AIState.Attack;
        fireDelay = reactionDelay;
    }

    void EnterBreakWall(Vector3Int cell)
    {
        state = AIState.BreakWall;
        wallCell = cell;
    }

    // ------------------------------------------------------------ состояния

    void TickPatrol()
    {
        if (canSee) { EnterChase(); return; }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            Idle();
            return;
        }

        if (path.Count == 0)
        {
            if (!PickPatrolPath()) waitTimer = patrolWait;
            Idle();
            return;
        }

        Vector2 dir = FollowPath(false);
        if (path.Count == 0 || pathIndex >= path.Count)     // дошли (или путь сбросился)
        {
            path.Clear();
            waitTimer = patrolWait;
            Idle();
            return;
        }

        if (needRepath) { path.Clear(); needRepath = false; Idle(); return; }

        SetInputs(Steer(dir), dir);
        CheckStuck(dir.sqrMagnitude > 0.01f);
    }

    void TickChase()
    {
        if (canSee && distToPlayer <= attackRange) { EnterAttack(); return; }
        if (!canSee && timeSinceSeen > loseSightTime) { EnterPatrol(); return; }

        Vector3 goal = canSee ? player.position : lastKnownPos;

        repathTimer -= Time.deltaTime;
        if (needRepath || repathTimer <= 0f || path.Count == 0)
        {
            repathTimer = repathInterval;
            needRepath = false;
            pathIndex = 0;
            pathfinder.FindPath(transform.position, goal, MoveSpeed, TimeToDestroy, path);
        }

        Vector2 dir = FollowPath(true);
        if (state != AIState.Chase) return;                 // FollowPath перевёл в BreakWall

        Vector2 aim = canSee ? ((Vector2)player.position - Origin()).normalized : dir;
        SetInputs(Steer(dir), aim);
        CheckStuck(dir.sqrMagnitude > 0.01f);
    }

    void TickAttack()
    {
        if (!canSee || distToPlayer > attackRange * 1.2f) { EnterChase(); return; }

        Vector2 aimDir = ((Vector2)player.position - Origin()).normalized;
        Vector2 move = Vector2.zero;
        if (distToPlayer < minKeepDistance)
            move = -aimDir;                                   // слишком близко: отступаем
        else if (reload.AmmoLeft <= 0 && reload.IsReloading && distToPlayer < attackRange * 0.7f)
            move = -aimDir;                                   // патронов нет, идёт перезарядка: отходим
        SetInputs(Steer(move), aimDir);

        if (fireDelay > 0f) { fireDelay -= Time.deltaTime; return; }
        if (Aimed(aimDir) && reload.CanFire) shooter.Fire();
    }

    void TickBreakWall()
    {
        if (canSee && distToPlayer <= attackRange) { EnterAttack(); return; }

        int remaining = pathfinder.Walls != null ? pathfinder.Walls.RemainingHits(wallCell) : 0;
        if (remaining <= 0) { EnterChase(); return; }       // стену пробили (или она оказалась стальной)

        Vector2 toWall = (Vector2)pathfinder.Ground.GetCellCenterWorld(wallCell) - Origin();
        if (toWall.magnitude > breakDistance * 1.8f) { EnterChase(); return; }   // нас отодвинуло

        SetInputs(Vector2.zero, toWall.normalized);
        if (Aimed(toWall) && reload.CanFire) shooter.Fire();
    }

    // ------------------------------------------------------------ движение по пути

    // Возвращает направление к следующей точке. allowWalls = можно ли останавливаться и пробивать стены.
    Vector2 FollowPath(bool allowWalls)
    {
        while (pathIndex < path.Count)
        {
            PathStep step = path[pathIndex];
            Vector2 to = (Vector2)step.world - (Vector2)transform.position;

            bool isWall = step.wallHits > 0 && pathfinder.Walls != null && pathfinder.Walls.RemainingHits(step.cell) > 0;
            if (isWall)
            {
                if (!allowWalls) { path.Clear(); pathIndex = 0; return Vector2.zero; }
                if (to.magnitude <= breakDistance) { EnterBreakWall(step.cell); return Vector2.zero; }
                return to.normalized;
            }

            if (to.sqrMagnitude <= nodeReachDistance * nodeReachDistance) { pathIndex++; continue; }
            return to.normalized;
        }
        return Vector2.zero;
    }

    bool PickPatrolPath()
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 p = (Vector2)transform.position + Random.insideUnitCircle * patrolRadius;
            var cell = pathfinder.Ground.WorldToCell(p);
            cell.z = 0;
            if (!pathfinder.IsWalkable(cell)) continue;

            Vector3 world = pathfinder.Ground.GetCellCenterWorld(cell);
            // null = стены не пробиваем
            if (pathfinder.FindPath(transform.position, world, MoveSpeed, null, path) && path.Count > 0)
            {
                pathIndex = 0;
                return true;
            }
        }
        path.Clear();
        return false;
    }

    void CheckStuck(bool wantsToMove)
    {
        stuckSample += Time.deltaTime;
        if (stuckSample < 0.3f) return;

        float moved = ((Vector2)transform.position - stuckPos).magnitude;
        stuckPos = transform.position;
        stuckSample = 0f;

        if (wantsToMove && moved < 0.05f) stuckAccum += 0.3f; else stuckAccum = 0f;

        if (stuckAccum >= stuckTime)
        {
            stuckAccum = 0f;
            needRepath = true;
        }
    }

    // ------------------------------------------------------------ стрельба и оценка времени

    bool Aimed(Vector2 dir)
    {
        return turret == null || Vector2.Angle(turret.transform.up, dir) <= aimTolerance;
    }

    // Цена стены в секундах: сколько мы потеряем, пока не сможем выстрелить в игрока после пробития.
    // Нужно сделать `shots` выстрелов по стене + ещё один по игроку. Считаем по РЕАЛЬНОМУ боезапасу.
    // Эту функцию A* использует как «цену» стены.
    float TimeToDestroy(int shots)
    {
        if (shots <= 0 || reload == null) return 0f;
        return TimeUntilNthShot(shots + 1) + 0.2f * shots;     // + время на наведение
    }

    float RemainingReload()
    {
        return reload.IsReloading ? Mathf.Max(0f, (1f - reload.Progress) * lastReloadDuration) : 0.3f;
    }

    // Через сколько секунд можно сделать n-й выстрел подряд
    float TimeUntilNthShot(int n)
    {
        var s = reload.Settings;
        int ammo = reload.AmmoLeft;
        float t = 0f;

        // 1. ждём первого выстрела
        switch (reload.Mode)
        {
            case ReloadMode.Magazine:
                if (ammo <= 0) { t = RemainingReload(); ammo = s.magazineSize; }      // обойма пуста: ждём перезарядку
                else if (!reload.CanFire) t = s.shotInterval * 0.5f;                  // пауза между выстрелами
                break;
            case ReloadMode.PerShell:
                if (ammo <= 0) { t = RemainingReload(); ammo = 1; }                   // ждём первый снаряд
                else if (!reload.CanFire) t = s.shellShotInterval * 0.5f;
                break;
            default:
                if (!reload.CanFire) t = RemainingReload();
                break;
        }
        ammo--;     // первый выстрел сделан

        // 2. остальные выстрелы
        for (int k = 2; k <= n; k++)
        {
            switch (reload.Mode)
            {
                case ReloadMode.Magazine:
                    if (ammo > 0) { t += s.shotInterval; ammo--; }
                    else { t += s.magazineReloadTime; ammo = s.magazineSize - 1; }
                    break;
                case ReloadMode.PerShell:
                    if (ammo > 0) { t += s.shellShotInterval; ammo--; }
                    else t += s.perShellTime;
                    break;
                default:
                    t += s.fixedTime;
                    break;
            }
        }
        return t;
    }

    // ------------------------------------------------------------ дистанция между врагами

    // Отталкивание от других врагов, чтобы не толпились
    Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        Vector2 me = transform.position;
        for (int i = 0; i < All.Count; i++)
        {
            var other = All[i];
            if (other == this) continue;
            Vector2 d = me - (Vector2)other.transform.position;
            float dist = d.magnitude;
            if (dist >= separationRadius) continue;
            if (dist < 0.001f) { push += Random.insideUnitCircle; continue; }
            push += d / dist * (1f - dist / separationRadius);
        }
        return push;
    }

    // Добавляет отталкивание к желаемому направлению движения
    Vector2 Steer(Vector2 dir)
    {
        Vector2 sep = Separation();
        if (sep.sqrMagnitude < 0.0001f) return dir;
        Vector2 result = dir + sep * separationWeight;
        return result.sqrMagnitude > 1f ? result.normalized : result;
    }

    // ------------------------------------------------------------ ввод

    void SetInputs(Vector2 move, Vector2 aim)
    {
        movement.MoveInput = move;
        if (turret != null) turret.AimInput = aim;
    }

    void Idle() { SetInputs(Steer(Vector2.zero), Vector2.zero); }

    // ------------------------------------------------------------ отладка

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        for (int i = pathIndex; i < path.Count; i++)
        {
            Gizmos.color = path[i].wallHits > 0 ? Color.red : Color.green;
            Vector3 from = i == pathIndex ? transform.position : path[i - 1].world;
            Gizmos.DrawLine(from, path[i].world);
        }
    }
}