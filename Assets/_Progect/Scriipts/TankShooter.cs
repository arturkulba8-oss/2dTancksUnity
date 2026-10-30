using UnityEngine;
using System; // Нужно для Action

public class TankShooter : MonoBehaviour
{
    [SerializeField] Transform firePoint;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] GameObject muzzleFlashPrefab;
    [SerializeField] float cooldown = 0.5f;

    float nextShot;
    float nextSound;
    bool firstCooldown;

    // --- НАШИ СОБЫТИЯ ("КРИКИ") ---
    public event Action OnTankFired;         // Событие успешного выстрела
    public event Action OnCooldownFailed;    // Событие «еще перезаряжаюсь» (осечка)

    public void Fire()
    {
        // Если время выстрела еще не наступило (перезарядка)
        if (Time.time < nextShot)
        {
            
            if(Time.time > nextSound && firstCooldown)
            {
                // "Орем" на весь мир, что попытались выстрелить во время кд!
                OnCooldownFailed?.Invoke();
                nextSound = Time.time + 0.2f;
            }

            if(Time.time > nextShot - cooldown + 0.3f)
            {
                firstCooldown = true;
            }
            
            
            
            return;
        }

        firstCooldown = false;

        nextShot = Time.time + cooldown;

        var bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.Init(gameObject);

        if (muzzleFlashPrefab != null)
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);

        // "Орем", что выстрел успешно совершен!
        OnTankFired?.Invoke();
    }
}
