using UnityEngine;

public class SaveGameSettings : MonoBehaviour
{
    public static SaveGameSettings Instance;

    // Храним ID карты (например: 0 - Классика, 1 - Зима, 2 - Пустыня, 3 - Чернобыль)
    public int selectedMapId = 0;
    
    // храним ноаер левела потом будем в мал лодер сразу брать карту потом искать текстовый левел для этой карты
    public int selectedLevelId = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Объект не уничтожится при смене сцен
        }
        else
        {
            Destroy(gameObject);
        }
    }
}