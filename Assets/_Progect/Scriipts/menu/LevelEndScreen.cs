using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Скрипт сцены «level_end»: показывает победу или поражение и ведёт дальше.
// Висит на Canvas этой сцены (всегда активный объект).
public class LevelEndScreen : MonoBehaviour
{

    [System.Serializable]
    public struct LevelUIIcon
    {
        public int levelId;
        public GameObject star1; // Иконка первой звезды/статуса
        public GameObject star2; // Иконка второй звезды
        public GameObject star3; // Иконка третьей звезды
    }

    [Header("Список кнопок/иконок КАРТОЧЕК ")]
    [SerializeField] private LevelUIIcon[] levelIcons;

    [Header("Звуки")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip loosSound;
    [SerializeField] AudioClip victorySound;
    [SerializeField, Range(0f, 1f)] float volume = 1f;


    [Header("Что показывать")]
    [SerializeField] GameObject victoryRoot;          // заголовок «Победа!» и т. п.
    [SerializeField] GameObject defeatRoot;           // заголовок «Поражение»
    [SerializeField] GameObject nextLevelButton;      // только при победе и если есть следующий уровень

    [Header("Сцены (названия должны совпадать с Build Profiles)")]
    [SerializeField] string gameScene = "FirstScene";
    [SerializeField] string levelMenuScene = "level_menu";
    [SerializeField] string mainMenuScene = "main_menu";

    [Header("Уровни")]
    [SerializeField] int totalLevels = 16;             // сколько всего уровней (длина mapFiles в MapLoader)

    [Header("Для теста, если сцену запустили напрямую")]
    [SerializeField] bool testVictory = false;

    void Start()
    {
        Time.timeScale = 1f;

        var save = SaveGameSettings.Instance;

        // Итог берём из LevelFlow (работает даже без меню), затем из SaveGameSettings.
        // Тестовое значение используется, только если уровня не было вообще (сцену открыли напрямую).
        LevelResult result = LevelFlow.LastResult;
        if (result == LevelResult.None && save != null) result = save.lastResult;
        bool victory = result != LevelResult.None ? result == LevelResult.Victory : testVictory;

        bool hasNext = victory && save != null && save.selectedLevelId < totalLevels;

        if (victoryRoot != null)
        {
            victoryRoot.SetActive(victory);
            if(victory)
                PlayShootSound(victorySound);
        }

        if (defeatRoot != null)
        {
            defeatRoot.SetActive(!victory);
            if(!victory)
                PlayShootSound(loosSound);
        } 
        
        if (nextLevelButton != null) nextLevelButton.SetActive(hasNext);

        LoadAndDisplayProgress();
    }

    // Эти методы вешаются на кнопки (On Click)
    public void Retry() { SceneManager.LoadScene(gameScene); }

    public void NextLevel()
    {
        var save = SaveGameSettings.Instance;
        if (save != null) save.selectedLevelId++;
        SceneManager.LoadScene(gameScene);
    }

    public void ToLevelMenu() { SceneManager.LoadScene(levelMenuScene); }
    public void ToMainMenu() { SceneManager.LoadScene(mainMenuScene); }

    void PlayShootSound(AudioClip sound)
    {
        if (audioSource != null && sound != null)
            audioSource.PlayOneShot(sound, volume);
    }

    public void LoadAndDisplayProgress()
    {
        int currentMap = SaveGameSettings.Instance != null ? SaveGameSettings.Instance.selectedMapId : 0;
        levelIcons[0].levelId = SaveGameSettings.Instance.selectedLevelId;
        levelIcons[1].levelId = SaveGameSettings.Instance.selectedLevelId;

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
