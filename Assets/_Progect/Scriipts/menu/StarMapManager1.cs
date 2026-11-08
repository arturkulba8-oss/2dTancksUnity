using UnityEngine;
using UnityEngine.UI;

public class MapMenuUI : MonoBehaviour
{
    [System.Serializable]
    public struct LevelUIIcon
    {
        public int mapId;
        public GameObject star1; // Иконка первой звезды/статуса
        public GameObject star2; // Иконка второй звезды
        public GameObject star3; // Иконка третьей звезды
    }

    [Header("Список кнопок/иконок КАРТ")]
    [SerializeField] private LevelUIIcon[] MapIcons;

    void Start()
    {
        LoadAndDisplayProgres();
    }

    public void LoadAndDisplayProgres()
    {
        for(int i = 0; i < MapIcons.Length; i++)
        {
            int state = 0;
            for(int j = 0; j <= 16; j++)
            {
                state += SaveGameSettings.Instance.GetLevelState(i, j);
            }

            int total = 16;
            if((total - state) < (total / 3))
                state = 0;
            else if((total - state) < (total / 3) * 2 )
                state = 1;
            else if((total - state) < total )
                state = 2;
            else if((total - state) == total )
                state = 3;

            foreach (var icon in MapIcons)
            {
                // Отрисовываем в зависимости от состояния
                if (icon.star1 != null) icon.star1.SetActive(state >= 1);
                if (icon.star2 != null) icon.star2.SetActive(state >= 2);
                if (icon.star3 != null) icon.star3.SetActive(state >= 3);
            }   
        }
    }
}