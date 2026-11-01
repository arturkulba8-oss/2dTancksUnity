using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;               // можно оставить пустым: найдётся по тегу Player
    [SerializeField] Tilemap boundsMap;              // сюда перетащить слой Ground
    [SerializeField] bool clampToMap = true;         // выключите, если нужна камера строго по центру везде
    [SerializeField] Vector2 offset = Vector2.zero;  // сдвиг от центра (0 = ровно по центру)

    Camera cam;
    Bounds worldBounds;

    void Awake()
    {
        cam = GetComponent<Camera>();
        RecalculateBounds();
    }

    // Вызовите этот метод, если карта строится уже во время игры
    public void RecalculateBounds()
    {
        if (boundsMap == null) return;
        boundsMap.CompressBounds();                       // сжать границы до реально занятых клеток
        var lb = boundsMap.localBounds;
        var center = boundsMap.transform.TransformPoint(lb.center);
        var size = Vector3.Scale(lb.size, boundsMap.transform.lossyScale);
        worldBounds = new Bounds(center, size);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            target = p.transform;
        }

        Vector2 pos = (Vector2)target.position + offset;

        if (clampToMap && boundsMap != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            pos.x = ClampAxis(pos.x, worldBounds.min.x + halfW, worldBounds.max.x - halfW, worldBounds.center.x);
            pos.y = ClampAxis(pos.y, worldBounds.min.y + halfH, worldBounds.max.y - halfH, worldBounds.center.y);
        }

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    // Если карта меньше экрана по этой оси, камера просто стоит в центре карты
    static float ClampAxis(float value, float min, float max, float mapCenter)
    {
        if (min > max) return mapCenter;
        return Mathf.Clamp(value, min, max);
    }
}