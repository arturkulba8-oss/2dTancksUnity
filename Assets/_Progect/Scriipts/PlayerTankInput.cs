using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TankMovement))]
public class PlayerTankInput : MonoBehaviour
{
    [SerializeField] TurretAim turret;           // объект, который вращается (turret или TurretPivot)

    TankMovement movement;
    InputAction move, aim, fire;
    TankShooter shooter;

    void Awake()
    {
        movement = GetComponent<TankMovement>();
        shooter = GetComponent<TankShooter>();

        // Движение: WASD (для ПК) + левый стик (экранный на телефоне)
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");

        // Прицел: стрелки (для ПК) + правый стик
        aim = new InputAction("Aim", InputActionType.Value);
        aim.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        aim.AddBinding("<Gamepad>/rightStick");

        // Выстрел: пробел + кнопка (экранная кнопка = Gamepad Button South)
        fire = new InputAction("Fire", InputActionType.Button);
        fire.AddBinding("<Keyboard>/space");
        fire.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable()  { move.Enable();  aim.Enable();  fire.Enable(); }
    void OnDisable() { move.Disable(); aim.Disable(); fire.Disable(); }

    void Update()
    {
        movement.MoveInput = move.ReadValue<Vector2>();
        if (turret != null) turret.AimInput = aim.ReadValue<Vector2>();


        if (fire.IsPressed() && shooter != null)
            shooter.Fire();   // потом заменим на создание снаряда
    }
}