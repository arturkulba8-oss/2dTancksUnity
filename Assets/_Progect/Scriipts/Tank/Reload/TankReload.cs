using System;
using UnityEngine;

public class TankReload : MonoBehaviour
{
    [SerializeField] ReloadMode mode = ReloadMode.Fixed;
    [SerializeField] ReloadSettings settings = new ReloadSettings();

    public event Action<float> ReloadStarted;
    public event Action ReloadFinished;

    ReloadLogic logic;
    ReloadMode builtMode;

    public ReloadSettings Settings => settings;
    public ReloadMode Mode { get => mode; set { mode = value; Rebuild(); } }
    public bool CanFire => logic.CanFire;
    public float Progress => logic.Progress;     // 0..1, можно привязать к полоске UI

    public int Capacity => logic.Capacity;
    public float BarFill => logic.BarFill;

    void Awake() { Rebuild(); }
    void Update() { logic.Tick(); }
    public void RegisterShot() { logic.OnShot(); }

    void OnValidate()   // смена режима в Inspector прямо во время игры
    {
        if (Application.isPlaying && logic != null && mode != builtMode) Rebuild();
    }

    void Rebuild()
    {
        if (logic != null) { logic.ReloadStarted -= OnStarted; logic.ReloadFinished -= OnFinished; }

        logic = mode switch
        {
            ReloadMode.Magazine => new MagazineReload(settings),
            ReloadMode.PerShell => new PerShellReload(settings),
            _                   => new FixedReload(settings),
        };
        builtMode = mode;

        logic.ReloadStarted += OnStarted; logic.ReloadFinished += OnFinished;
        ReloadFinished?.Invoke();    // сменили режим, значит старую анимацию гасим
    }

    void OnStarted(float d) => ReloadStarted?.Invoke(d);
    void OnFinished() => ReloadFinished?.Invoke();
}