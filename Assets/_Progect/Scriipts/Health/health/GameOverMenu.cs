using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Заготовка меню проигрыша. Вешается на ВСЕГДА АКТИВНЫЙ объект (например Canvas), а не на саму панель.
public class GameOverMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;              // панель «Вы проиграли» (в Canvas, по умолчанию выключена)
    [SerializeField] float showDelay = 1.2f;        // пауза после гибели: успеть увидеть взрыв
    [SerializeField] bool pauseGame = false;
    [SerializeField] string mainMenuScene = "level_menu";     // имя сцены меню, когда она появится
    [SerializeField] UnityEvent onShown;            // звук, анимация и т. д.

    void OnEnable() { GameEvents.PlayerDied += OnPlayerDied; }
    void OnDisable() { GameEvents.PlayerDied -= OnPlayerDied; }

    void OnPlayerDied() { StartCoroutine(Show()); }

    IEnumerator Show()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        if (panel != null) panel.SetActive(true);
        if (pauseGame) Time.timeScale = 0f;
        onShown?.Invoke();
    }

    // Эти методы вешаются на кнопки панели (On Click)
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(mainMenuScene)) { Debug.LogWarning("GameOverMenu: не указана сцена меню", this); return; }
        SceneManager.LoadScene(mainMenuScene);
    }
}
