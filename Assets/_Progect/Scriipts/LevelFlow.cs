using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum LevelResult { None, Victory, Defeat }

// Конец уровня: победа (все враги убиты) или поражение (игрок потерял все сердца).
// Висит на ВСЕГДА АКТИВНОМ объекте игровой сцены (например, Canvas или пустой LevelFlow).
// Заменяет GameOverMenu: не оставляйте оба компонента одновременно.
public class LevelFlow : MonoBehaviour
{
    [SerializeField] TankSpawner spawner;                 // найдётся сам

    [Header("Куда вести после конца уровня")]
    [SerializeField] string resultScene = "level_end";    // пусто = показать панели в этой же сцене
    [SerializeField] GameObject winPanel;                 // нужны только если resultScene пустое
    [SerializeField] GameObject losePanel;

    [Header("Паузы (успеть увидеть взрыв)")]
    [SerializeField] float victoryDelay = 1.5f;
    [SerializeField] float defeatDelay = 1.5f;

    [Header("Заготовки: звук, анимации, счёт")]
    [SerializeField] UnityEvent onVictory;
    [SerializeField] UnityEvent onDefeat;

    public LevelResult Result { get; private set; }

    // Итог последнего уровня. Статическое поле живёт между сценами и не зависит от SaveGameSettings
    public static LevelResult LastResult { get; private set; } = LevelResult.None;

    bool finished;

    void Awake()
    {
        if (spawner == null) spawner = FindFirstObjectByType<TankSpawner>();
    }

    void OnEnable()
    {
        GameEvents.PlayerDied += OnPlayerDied;
        if (spawner != null) spawner.AllEnemiesDefeated += OnAllEnemiesDefeated;
    }

    void OnDisable()
    {
        GameEvents.PlayerDied -= OnPlayerDied;
        if (spawner != null) spawner.AllEnemiesDefeated -= OnAllEnemiesDefeated;
    }

    void OnPlayerDied() { Finish(LevelResult.Defeat, defeatDelay); }                  // сюда приходит TankDeath.DiePlayer()
    void OnAllEnemiesDefeated() { Finish(LevelResult.Victory, victoryDelay); }        // сюда приходит TankSpawner.EnemyRoutine()

    void Finish(LevelResult result, float delay)
    {
        if (finished) return;            // засчитывается то, что случилось первым
        finished = true;
        Result = result;
        LastResult = result;

        if (spawner != null) spawner.StopSpawning();

        if (result == LevelResult.Victory) onVictory?.Invoke(); else onDefeat?.Invoke();

        var save = SaveGameSettings.Instance;
        if (save != null) save.ReportResult(result == LevelResult.Victory);
        Debug.Log("LevelFlow: итог " + result + ", SaveGameSettings " + (save != null ? "есть" : "НЕТ (запуск мимо меню?)"));

        StartCoroutine(ShowAfter(result, delay));
    }

    IEnumerator ShowAfter(LevelResult result, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!string.IsNullOrEmpty(resultScene))
        {
            SceneManager.LoadScene(resultScene);          // отдельная сцена «конец уровня»
            yield break;
        }

        var panel = result == LevelResult.Victory ? winPanel : losePanel;    // или панель поверх игры
        if (panel != null) panel.SetActive(true);
    }

    // Для кнопок панелей (если не используете отдельную сцену)
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}