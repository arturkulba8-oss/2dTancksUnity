using UnityEngine;

public class TankShotEffect : MonoBehaviour
{
    [SerializeField] TankShooter shooter;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip shootSound;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    void Awake()
    {
        // Если поля пустые, берём компоненты с этого же объекта
        if (shooter == null) shooter = GetComponent<TankShooter>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (shooter != null) shooter.OnTankFired += PlayShootSound;
    }

    void OnDisable()
    {
        if (shooter != null) shooter.OnTankFired -= PlayShootSound;
    }

    void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound, volume);
    }
}