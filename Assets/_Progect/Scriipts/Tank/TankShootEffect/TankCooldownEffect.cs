using UnityEngine;

public class TankCooldownEffect : MonoBehaviour
{
    [SerializeField] TankShooter shooter;
    [SerializeField] AudioSource audioSource;

    [SerializeField] AudioClip cooldownSound;
    [SerializeField, Range(0f, 1f)] float volume = 1f;



    void Awake()
    {
        // Если поля не заполнены в инспекторе, берём компоненты с этого же объекта
        if (shooter == null) shooter = GetComponent<TankShooter>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (shooter != null) shooter.OnCooldownFailed += PlayShootSound;
    }

    void OnDisable()
    {
        if (shooter != null) shooter.OnCooldownFailed -= PlayShootSound;
    }

    void PlayShootSound()
    {
        if (audioSource != null && cooldownSound != null)
            audioSource.PlayOneShot(cooldownSound, volume);
    }
}