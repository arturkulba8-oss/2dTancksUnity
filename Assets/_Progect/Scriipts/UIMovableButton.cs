using UnityEngine;
using UnityEngine.EventSystems;

public class UIMovableButton : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    public bool Editable { get; set; }          // по умолчанию false

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        LoadSettings();                         // строку enabled = false удалить
    }

    // Добавляем метод начала перетаскивания, чтобы объект вставал поверх остальных
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!Editable) return;
        transform.SetAsLastSibling();
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!Editable) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void ChangeSize(float scaleMultiplier)
    {
        rectTransform.localScale = Vector3.one * scaleMultiplier;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(gameObject.name + "_X", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(gameObject.name + "_Y", rectTransform.anchoredPosition.y);
        PlayerPrefs.SetFloat(gameObject.name + "_Scale", rectTransform.localScale.x);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(gameObject.name + "_X"))
        {
            float x = PlayerPrefs.GetFloat(gameObject.name + "_X");
            float y = PlayerPrefs.GetFloat(gameObject.name + "_Y");
            rectTransform.anchoredPosition = new Vector2(x, y);
        }

        if (PlayerPrefs.HasKey(gameObject.name + "_Scale"))
        {
            float scale = PlayerPrefs.GetFloat(gameObject.name + "_Scale");
            rectTransform.localScale = Vector3.one * scale;
        }
    }
}
