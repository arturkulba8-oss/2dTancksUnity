using System;
using UnityEngine;

// Общие события игры. Подписывайтесь в OnEnable и обязательно отписывайтесь в OnDisable.
public static class GameEvents
{
    public static event Action PlayerDied;                    // игрок потерял все сердца
    public static event Action<Vector3, GameObject> EnemyKilled;   // (где погиб враг, кто убил)

    public static void RaisePlayerDied() { PlayerDied?.Invoke(); }
    public static void RaiseEnemyKilled(Vector3 position, GameObject killer) { EnemyKilled?.Invoke(position, killer); }
}
