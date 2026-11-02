using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    // --- ПЕРЕХОДЫ ПО СЦЕНАМ ---

    // Вызывается кнопкой "Играть" на сцене main_menu
    public void OpenModeMenu()
    {
        SceneManager.LoadScene("menu_mode");
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