using UnityEngine;
using UnityEngine.Events;

// Что происходит, когда у танка кончились сердца.
// Враг: взрыв, возможный дроп (аптечка), исчезает. Игрок: взрыв, управление отключается, событие GameEvents.PlayerDied.
[RequireComponent(typeof(Health))]
public class TankDeath : MonoBehaviour
{
    [SerializeField] bool isPlayer;

    [Header("Эффект гибели")]
    [SerializeField] GameObject deathEffect;            // например копия HitExplosion
    [SerializeField] float effectScale = 1.6f;

    [Header("Дроп (аптечка). Пусто = ничего не падает")]
    [SerializeField] GameObject dropPrefab;
    [SerializeField, Range(0f, 1f)] float dropChance = 1f;

    [Header("Заготовка: сюда можно повесить звук, счёт и т. д.")]
    [SerializeField] UnityEvent onDeath;

    Health health;
    bool dead;

    void Awake() { health = GetComponent<Health>(); }
    void OnEnable() { health.Died += OnDied; }
    void OnDisable() { health.Died -= OnDied; }

    void OnDied(GameObject killer)
    {
        if (dead) return;
        dead = true;

        if (deathEffect != null)
        {
            var fx = Instantiate(deathEffect, transform.position, Quaternion.identity);
            fx.transform.localScale *= effectScale;
        }
        onDeath?.Invoke();

        if (isPlayer) DiePlayer(); else DieEnemy(killer);
    }

    void DieEnemy(GameObject killer)
    {
        GameEvents.RaiseEnemyKilled(transform.position, killer);

        if (dropPrefab != null && Random.value <= dropChance)
            Instantiate(dropPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void DiePlayer()
    {
        var move = GetComponent<TankMovement>();
        if (move != null) move.MoveInput = Vector2.zero;

        // отключаем управление, стрельбу и перезарядку (Health и этот скрипт оставляем)
        foreach (var b in GetComponentsInChildren<MonoBehaviour>())
            if (b != this && b != health) b.enabled = false;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;   // танк «исчезает»

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        gameObject.tag = "Untagged";            // враги и аптечки больше не считают его игроком
        GameEvents.RaisePlayerDied();
    }
}
