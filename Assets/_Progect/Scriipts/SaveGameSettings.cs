using UnityEngine;

public class SaveGameSettings : MonoBehaviour
{
    public static SaveGameSettings Instance;

    // Храним ID карты (например: 0 - Классика, 1 - Зима, 2 - Пустыня, 3 - Чернобыль)
    public int selectedMapId = 0;

    // Номер выбранного уровня (с 1)
    public int selectedLevelId = 0;

    public bool redactButton = false;

    // Итог последнего уровня (читает сцена level_end)
    public LevelResult lastResult = LevelResult.None;

    // Самый далёкий открытый уровень (сохраняется между запусками игры)
    public int highestUnlockedLevel = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            highestUnlockedLevel = PlayerPrefs.GetInt("highestUnlockedLevel", 1);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Вызывает LevelFlow в конце уровня
    public void ReportResult(bool victory)
    {
        lastResult = victory ? LevelResult.Victory : LevelResult.Defeat;
        if (!victory) return;

        int next = Mathf.Max(1, selectedLevelId) + 1;
        if (next > highestUnlockedLevel)
        {
            highestUnlockedLevel = next;
            PlayerPrefs.SetInt("highestUnlockedLevel", next);
            PlayerPrefs.Save();
        }
    }

    // Сохранить состояние (от 1 до 3) для конкретной карты и уровня
    public void SaveLevelState(int mapId, int levelId, int state)
    {
        string key = $"Map_{mapId}_Level_{levelId}_State";
        PlayerPrefs.SetInt(key, state);
        PlayerPrefs.Save();
    }

    // Получить состояние уровня (возвращает 0, если уровень еще не проходили)
    public int GetLevelState(int mapId, int levelId)
    {
        string key = $"Map_{mapId}_Level_{levelId}_State";
        return PlayerPrefs.GetInt(key, 0); 
    }
}