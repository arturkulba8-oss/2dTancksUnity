using System;
using UnityEngine;

public enum ReloadMode { Fixed, Magazine, PerShell }

[Serializable]
public class ReloadSettings
{
    [Header("Fixed")]
    public float fixedTime = 3f;

    [Header("Magazine")]
    public int magazineSize = 3;
    public float shotInterval = 0.4f;        // пауза между выстрелами внутри обоймы
    public float magazineReloadTime = 5f;    // перезарядка пустой обоймы

    [Header("PerShell")]
    public int maxShells = 3;
    public float perShellTime = 2f;          // время на один снаряд

    public float shellShotInterval = 0.3f;   // пауза между выстрелами (добавить)
}

public abstract class ReloadLogic
{
    protected readonly ReloadSettings s;
    public event Action<float> ReloadStarted;   // параметр: длительность в секундах
    public event Action ReloadFinished;

    bool reloading; float start, duration;

    protected ReloadLogic(ReloadSettings settings) { s = settings; }

    public abstract bool CanFire { get; }
    public abstract void OnShot();     // вызывается после каждого выстрела
    public abstract void Tick();       // вызывается каждый кадр

    public float Progress => reloading ? Mathf.Clamp01((Time.time - start) / duration) : 1f;

    public virtual int Capacity => 1;                 // сколько патронов в полоске
    public virtual float BarFill => Progress;         // заполнение полоски, 0..1

    protected bool IsReloading => reloading;
    protected bool ReloadDone => reloading && Time.time >= start + duration;

    protected void BeginReload(float d)
    {
        reloading = true; start = Time.time; duration = Mathf.Max(0.01f, d);
        ReloadStarted?.Invoke(duration);
    }
    protected void EndReload() { reloading = false; ReloadFinished?.Invoke(); }
}

// 1) Фиксированная перезарядка после каждого выстрела
public class FixedReload : ReloadLogic
{
    public FixedReload(ReloadSettings s) : base(s) { }
    public override bool CanFire => !IsReloading;
    public override void OnShot() => BeginReload(s.fixedTime);
    public override void Tick() { if (ReloadDone) EndReload(); }
}

// 2) Обойма: N выстрелов, потом долгая перезарядка
public class MagazineReload : ReloadLogic
{
    int ammo; float nextShotAt;
    public MagazineReload(ReloadSettings s) : base(s) { ammo = s.magazineSize; }
    public override bool CanFire => !IsReloading && ammo > 0 && Time.time >= nextShotAt;
    public override void OnShot()
    {
        ammo--; nextShotAt = Time.time + s.shotInterval;
        if (ammo <= 0) BeginReload(s.magazineReloadTime);
    }
    public override void Tick() { if (ReloadDone) { ammo = s.magazineSize; EndReload(); } }

    public override int Capacity => s.magazineSize;
}

// 3) Снаряды заряжаются по одному
public class PerShellReload : ReloadLogic
{
    int ammo;
    float nextShotAt;

    public PerShellReload(ReloadSettings s) : base(s) { ammo = s.maxShells; }

    public override int Capacity => s.maxShells;
    public override float BarFill => Mathf.Clamp01((ammo + Progress) / s.maxShells);

    public override bool CanFire => ammo > 0 && Time.time >= nextShotAt;

    public override void OnShot()
    {
        ammo--;
        nextShotAt = Time.time + s.shellShotInterval;
        if (!IsReloading) BeginReload(s.perShellTime);
    }

    public override void Tick()
    {
        if (!ReloadDone) return;
        ammo++;
        if (ammo < s.maxShells) BeginReload(s.perShellTime);   // следующий снаряд
        else EndReload();
    }
}