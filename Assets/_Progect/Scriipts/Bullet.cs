using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float lifetime = 3f;
    [SerializeField] GameObject hitEffect;          // префаб HitExplosion
    [SerializeField] int damage = 1;                // сколько сердец снимает пуля
    [SerializeField] bool friendlyFire = false;     // false: пуля пролетает сквозь своих

    // Срабатывает в момент попадания, до исчезновения пули (на него подписан ExplosionBullet: звук взрыва)
    public event Action OnExplosionBullet;

    GameObject owner;
    string ownerTag;

    public void Init(GameObject shooter)
    {
        owner = shooter;
        ownerTag = shooter != null ? shooter.tag : null;    // запоминаем сразу: стрелявший может погибнуть раньше пули
    }

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.up * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;                                   // вода, листья, аптечки, другие пули

        var rb = other.attachedRigidbody;
        if (owner != null && rb != null && rb.gameObject == owner) return;   // свой танк

        var health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            // союзники (одинаковый тег): пуля летит дальше
            if (!friendlyFire && ownerTag != null && health.CompareTag(ownerTag)) return;
            health.TakeDamage(damage, owner);
        }
        else
        {
            var walls = other.GetComponent<WallTilemap>();
            if (walls != null) walls.TryHit(transform.position, transform.up);
        }

        OnExplosionBullet?.Invoke();
        if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}