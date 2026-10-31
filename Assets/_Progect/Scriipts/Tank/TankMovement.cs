using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TankMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float turnSpeed = 220f;     // градусов в секунду
    [SerializeField] Animator hullAnimator;      // объект Hull

    // Сюда пишет контроллер (игрок или ИИ)
    public Vector2 MoveInput { get; set; }

    Rigidbody2D rb;
    static readonly int SpeedHash = Animator.StringToHash("speed");

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void FixedUpdate()
    {
        float speed = 0f;
        float angle = rb.rotation;

        if (MoveInput.sqrMagnitude > 0.04f)          // мёртвая зона стика
        {
            // Угол, куда показывает стик (-90, т.к. спрайт смотрит вверх)
            float target = Mathf.Atan2(MoveInput.y, MoveInput.x) * Mathf.Rad2Deg - 90f;
            angle = Mathf.MoveTowardsAngle(angle, target, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(angle);

            // Едем вперёд, только когда корпус почти довернулся
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, target));
            float k = Mathf.Clamp01(1f - diff / 90f);
            speed = moveSpeed * Mathf.Clamp01(MoveInput.magnitude) * k;
        }

        Vector2 forward = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        rb.linearVelocity = forward * speed;
        rb.angularVelocity = 0f;

        if (hullAnimator != null) hullAnimator.SetFloat(SpeedHash, speed);
    }
}