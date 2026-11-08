using UnityEngine;
using UnityEngine.UI;

public class LevelMenuUI : MonoBehaviour
{
    [System.Serializable]
    public struct LevelUIIcon
    {
        public int levelId;
        public GameObject star1; // Иконка первой звезды/статуса
        public GameObject star2; // Иконка второй звезды
        public GameObject star3; // Иконка третьей звезды
    }

    [Header("Список кнопок/иконок уровней на текущей карте")]
    [SerializeField] private LevelUIIcon[] levelIcons;

    void Start()
    {
        LoadAndDisplayProgress();
    }

    public void LoadAndDisplayProgress()
    {
        int currentMap = SaveGameSettings.Instance != null ? SaveGameSettings.Instance.selectedMapId : 0;

        foreach (var icon in levelIcons)
        {
            // Запрашиваем состояние (от 0 до 3) из PlayerPrefs через синглтон
            int state = SaveGameSettings.Instance.GetLevelState(currentMap, icon.levelId);

            // Отрисовываем в зависимости от состояния
            if (icon.star1 != null) icon.star1.SetActive(state >= 1);
            if (icon.star2 != null) icon.star2.SetActive(state >= 2);
            if (icon.star3 != null) icon.star3.SetActive(state >= 3);
        }
    }
}