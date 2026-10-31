using UnityEngine;

public class TurretAim : MonoBehaviour
{
    [SerializeField] float rotateSpeed = 400f;   // градусов в секунду

    // Сюда пишет контроллер (игрок или ИИ)
    public Vector2 AimInput { get; set; }

    float targetAngle;

    void Start() { targetAngle = transform.eulerAngles.z; }

    void Update()
    {
        // Если стик отпущен, башня остаётся в последнем положении
        if (AimInput.sqrMagnitude > 0.09f)
            targetAngle = Mathf.Atan2(AimInput.y, AimInput.x) * Mathf.Rad2Deg - 90f;

        float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);   // мировой угол, не зависит от корпуса
    }
}