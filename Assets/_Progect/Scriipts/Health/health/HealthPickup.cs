using UnityEngine;

// Аптечка: игрок подъезжает и получает сердце (но не больше максимума).
// Префаб: Sprite Renderer + Collider 2D с галочкой Is Trigger + этот скрипт.
[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    [SerializeField] int healAmount = 1;
    [SerializeField] string targetTag = "Player";
    [SerializeField] bool onlyIfNotFull = true;     // при полном здоровье аптечка остаётся лежать
    [SerializeField] float lifetime = 12f;          // исчезает сама (0 = лежит вечно)
    [SerializeField] GameObject pickupEffect;       // необязательно

    void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other) { TryPickup(other); }
    void OnTriggerStay2D(Collider2D other) { TryPickup(other); }   // чтобы подобрать, если здоровье упало, пока стоим на ней

    void TryPickup(Collider2D other)
    {
        var health = other.GetComponentInParent<Health>();
        if (health == null || !health.CompareTag(targetTag)) return;
        if (onlyIfNotFull && health.IsFull) return;
        if (!health.Heal(healAmount)) return;

        if (pickupEffect != null) Instantiate(pickupEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
