using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float lifetime = 3f;
    [SerializeField] GameObject hitEffect;     // префаб HitExplosion

    GameObject owner;

    public event Action OnExplosionBullet;

    public void Init(GameObject shooter) { owner = shooter; }

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.up * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;                                   // другие пули и эффекты
        var rb = other.attachedRigidbody;
        if (owner != null && rb != null && rb.gameObject == owner) return;   // свой танк

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
            OnExplosionBullet?.Invoke();
        }
        Destroy(gameObject);
    }
}