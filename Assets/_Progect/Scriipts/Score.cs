using UnityEngine;
using TMPro;

public class EnemiesCounter : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] TankSpawner spawner;
    int shown = -1;

    void Update()
    {
        if (spawner == null) return;
        int left = spawner.EnemiesLeft;
        int total = spawner.TotalEnemies;
        if (left == shown) return;
        shown = left;
        label.text = $"Врагов: {left} из {total}";;
        
    }
}