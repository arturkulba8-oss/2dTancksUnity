using UnityEngine;

// Управление корпусом. Один и тот же код для игрока и для врагов: они просто задают MoveInput.
// Правило: стик (или ИИ) указывает направление. Если оно в передней половине круга от носа танка,
// танк поворачивает нос к нему и едет вперёд. Если в задней половине, танк едет назад
// и поворачивает к нему корму. Поэтому танк никогда не разворачивается больше чем на 90 градусов.
[RequireComponent(typeof(Rigidbody2D))]
public class TankMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float turnSpeed = 220f;     // градусов в секунду
    [SerializeField] Animator hullAnimator;      // объект Hull

    [Header("Езда задом")]
    [SerializeField, Range(30f, 150f)] float reverseAngle = 90f;       // дальше этого угла от носа едем задом
    [SerializeField] float reverseHysteresis = 12f;                    // запас на границе, чтобы не переключалось туда-сюда
    [SerializeField, Range(0.2f, 1f)] float reverseSpeedFactor = 0.7f; // задом медленнее

    // Сюда пишет контроллер (игрок или ИИ)
    public Vector2 MoveInput { get; set; }

    public float MoveSpeed => moveSpeed;
    public bool IsReversing => reversing;

    Rigidbody2D rb;
    bool reversing;
    static readonly int SpeedHash = Animator.StringToHash("speed");

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void FixedUpdate()
    {
        float surface = SurfaceZone.GetMultiplier(rb.position);   // вода замедляет, лёд ускоряет

        float speed = 0f;
        float angle = rb.rotation;

        if (MoveInput.sqrMagnitude > 0.04f)          // мёртвая зона стика
        {
            // Угол носа, если бы ехали вперёд к стику (-90, т.к. спрайт смотрит вверх)
            float desired = Mathf.Atan2(MoveInput.y, MoveInput.x) * Mathf.Rad2Deg - 90f;

            // Насколько стик отклонён от носа: 0 = прямо перед, 180 = ровно сзади
            float fromNose = Mathf.Abs(Mathf.DeltaAngle(angle, desired));

            // Переключение вперёд/назад с запасом, чтобы на границе не дёргалось
            if (reversing) { if (fromNose < reverseAngle - reverseHysteresis) reversing = false; }
            else           { if (fromNose > reverseAngle + reverseHysteresis) reversing = true; }

            // Едем вперёд: нос к стику. Едем назад: корма к стику (нос смотрит в противоположную сторону)
            float target = reversing ? desired + 180f : desired;
            angle = Mathf.MoveTowardsAngle(angle, target, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(angle);

            // Газ даём только когда корпус почти довернулся
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, target));
            float k = Mathf.Clamp01(1f - diff / 90f);

            speed = moveSpeed * surface * Mathf.Clamp01(MoveInput.magnitude) * k;
            if (reversing) speed *= -reverseSpeedFactor;      // отрицательная скорость = назад
        }

        Vector2 forward = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        rb.linearVelocity = forward * speed;
        rb.angularVelocity = 0f;

        if (hullAnimator != null) hullAnimator.SetFloat(SpeedHash, Mathf.Abs(speed));   // гусеницы крутятся при любой езде
    }
}