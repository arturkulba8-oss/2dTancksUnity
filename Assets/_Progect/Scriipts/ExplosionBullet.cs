using UnityEngine;

public class ExplosionBullet : MonoBehaviour
{
    [SerializeField] Bullet bullet;

    [SerializeField] AudioClip explosionSound;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    void Awake()
    {
        // Если поля пустые, берём компоненты с этого же объекта
        if (bullet == null) bullet = GetComponent<Bullet>();
    }

    void OnEnable()
    {
        if (bullet != null) bullet.OnExplosionBullet += PlayExplosionSound;
    }

    void OnDisable()
    {
        if (bullet != null) bullet.OnExplosionBullet -= PlayExplosionSound;
    }

    void PlayExplosionSound()
{
    if (explosionSound == null) return;

    var go = new GameObject("ExplosionSound");
    go.transform.position = transform.position;

    var src = go.AddComponent<AudioSource>();
    src.clip = explosionSound;
    src.volume = volume;
    src.spatialBlend = 0f;   // 2D-звук, без затухания от расстояния до камеры
    src.Play();

    Destroy(go, explosionSound.length);
}
}
