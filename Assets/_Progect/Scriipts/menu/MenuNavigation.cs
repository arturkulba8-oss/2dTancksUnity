using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{

    [SerializeField] GameObject settings;
    [SerializeField] private Slider volumeSlider;

    [Header("Все элементы управления (кнопки и джойстики)")]
    [SerializeField] GameObject SaveButton;
    [SerializeField] GameObject PauseButton;
    [SerializeField] private UIMovableButton[] allControls; // Перетащите сюда все 3 кнопки и 2 джойстика

    int pauseFlag = 0;

    void Start()
    {
        // Загружаем сохраненную громкость (по умолчанию 1.0)
        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        // Подписываемся на изменение ползунка
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (SaveGameSettings.Instance.redactButton)
        {
            PauseButton.SetActive(false);
            EnableEditingMode();
            SaveButton.SetActive(true);
        }
    }


    public void EnableEditingMode()
    {
        Time.timeScale = 0f;
        foreach (var control in allControls)
        {
        
                control.Editable = true; // Разрешаем перетаскивание для каждого элемента
        }
    }

    // Этот метод вызывается по кнопке «Сохранить»
    public void SaveAndDisableEditing()
    {
        foreach (var control in allControls)
        {
            if (control != null)
            {
                control.SaveSettings(); // Сохраняем позицию и размер каждого элемента
                control.Editable = false; // Выключаем режим редактирования
            }
        }
        PauseButton.SetActive(true);
        SaveButton.SetActive(false);
        SaveGameSettings.Instance.redactButton = false;
        SceneManager.LoadScene("main_menu");
        settings.SetActive(true);
    }

    public void OpenSettingsButton()
    {
        SaveGameSettings.Instance.selectedMapId = 1;
        SaveGameSettings.Instance.selectedLevelId = 1;
        SaveGameSettings.Instance.redactButton = true;
        SceneManager.LoadScene("FirstScene"); // Загружаем игровую сцену
    }

     void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("GameVolume", value); // Сохраняем
        PlayerPrefs.Save();
    }
    // --- ПЕРЕХОДЫ ПО СЦЕНАМ ---
    public void pause()
    {
        if(pauseFlag % 2 == 0)
        {
            Time.timeScale = 0f;
            settings.SetActive(true);

        }
        else
        {
            Time.timeScale = 1f;
            settings.SetActive(false);
        }
            
        pauseFlag++;
    }

    // Вызывается кнопкой "Играть" на сцене main_menu
    public void OpenModeMenu()
    {
        SceneManager.LoadScene("menu_mode");
    }

    public void OpenSettings()
    {
        settings.SetActive(true);
    }

    public void closeSettings()
    {
        settings.SetActive(false);
    }

    // Вызывается кнопкой "Назад" на сцене menu_mode
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("main_menu");
    }

    // Вызывается кнопкой "Назад" на сцене map_menu
    public void BackToModeMenu()
    {
        SceneManager.LoadScene("menu_mode");
    }

    // --- ВЫБОР РЕЖИМА И КАРТЫ ---

    // Вызывается кнопками "Прохождение" / "Мультиплеер" на сцене menu_mode
    public void SelectMode()//string modeName//)
    {
        //if (GameManager.Instance != null)
        //{
        //    GameManager.Instance.selectedMode = modeName;
        //}
        SceneManager.LoadScene("map_menu"); // Переходим к выбору карты
    }

    // Вызывается при нажатии на плашку карты (Классика = 0, Зима = 1 и т.д.)
    public void SelectMap(int mapId)
    {
        if (SaveGameSettings.Instance != null)
        {
            SaveGameSettings.Instance.selectedMapId = mapId;
            Debug.Log("Выбрана карта №: " + mapId);
        }
    }

    public void BackToMapMenu()
    {
        SceneManager.LoadScene("map_menu");
    }

    public void OpenLevel()
    {
        SceneManager.LoadScene("level_menu"); // Переходим к выбору карты
    }

    public void SelectLevel(int levelId)
    {
        if (SaveGameSettings.Instance != null)
        {
            SaveGameSettings.Instance.selectedLevelId = levelId;
            Debug.Log("Выбрана уровень №: " + levelId);
        }
    }

    // Вызывается кнопкой "В бой" на сцене map_menu
    public void StartGame()
    {
        SceneManager.LoadScene("FirstScene"); // Загружаем игровую сцену
    }
}