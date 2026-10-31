using UnityEngine;
using System;

public class TankShooter : MonoBehaviour
{
    [SerializeField] Transform firePoint;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] GameObject muzzleFlashPrefab;
    [SerializeField] float failSoundDelay = 0.3f;      // защита: пауза после выстрела, когда «щелчок» не играет
    [SerializeField] float failSoundInterval = 0.2f;   // как часто повторять звук при зажатой кнопке

    TankReload reload;
    float lastShotTime = -999f;
    float nextSound;

    public event Action OnTankFired;
    public event Action OnCooldownFailed;

    void Awake() { reload = GetComponent<TankReload>(); }

    public void Fire()
    {
        if (!reload.CanFire)
        {
            if (Time.time >= lastShotTime + failSoundDelay && Time.time >= nextSound)
            {
                nextSound = Time.time + failSoundInterval;
                OnCooldownFailed?.Invoke();
            }
            return;
        }

        lastShotTime = Time.time;
        reload.RegisterShot();

        var bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.Init(gameObject);

        if (muzzleFlashPrefab != null)
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);

        OnTankFired?.Invoke();
    }
}