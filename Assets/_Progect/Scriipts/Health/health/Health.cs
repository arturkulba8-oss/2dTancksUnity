using System;
using UnityEngine;

// Здоровье в «сердечках»: 1 сердце = 1 очко. Висит на любом танке (игрок и враги).
public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] int maxHealth = 4;         // сколько сердец всего (слотов на экране)
    [SerializeField, Min(1)] int startHealth = 3;       // сколько полных сердец в начале
    [SerializeField] float invulnerableTime = 0.15f;    // пауза после попадания, когда урон не проходит

    int current;
    bool initialized;
    float invulnerableUntil;

    public int Current { get { Init(); return current; } }
    public int Max => maxHealth;
    public bool IsDead { get { Init(); return current <= 0; } }
    public bool IsFull { get { Init(); return current >= maxHealth; } }
    public float InvulnerableTime => invulnerableTime;

    public event Action<int, int> Changed;           // (текущее, максимум)
    public event Action<int, GameObject> Damaged;    // (сколько урона, кто нанёс)
    public event Action<int> Healed;                 // (сколько восстановлено)
    public event Action<GameObject> Died;            // (кто убил)

    void Awake() { Init(); }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        startHealth = Mathf.Clamp(startHealth, 1, maxHealth);
        current = startHealth;
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        startHealth = Mathf.Clamp(startHealth, 1, maxHealth);
    }

    // true, если урон реально прошёл
    public bool TakeDamage(int amount, GameObject source = null)
    {
        Init();
        if (current <= 0 || amount <= 0 || Time.time < invulnerableUntil) return false;

        current = Mathf.Max(0, current - amount);
        invulnerableUntil = Time.time + invulnerableTime;

        Damaged?.Invoke(amount, source);
        Changed?.Invoke(current, maxHealth);
        if (current == 0) Died?.Invoke(source);
        return true;
    }

    // true, если что-то восстановилось
    public bool Heal(int amount)
    {
        Init();
        if (current <= 0 || amount <= 0 || current >= maxHealth) return false;

        int before = current;
        current = Mathf.Min(maxHealth, current + amount);

        Healed?.Invoke(current - before);
        Changed?.Invoke(current, maxHealth);
        return true;
    }

    // Поменять максимум во время игры (улучшения, бонусы)
    public void SetMax(int newMax, bool fillUp = false)
    {
        Init();
        maxHealth = Mathf.Max(1, newMax);
        current = fillUp ? maxHealth : Mathf.Min(current, maxHealth);
        Changed?.Invoke(current, maxHealth);
    }

    // Для проверки без стрельбы: в Play правая кнопка по заголовку компонента
    [ContextMenu("Тест: получить 1 урона")]
    void TestDamage() { invulnerableUntil = 0f; TakeDamage(1); }

    [ContextMenu("Тест: вылечить 1")]
    void TestHeal() { Heal(1); }
}
