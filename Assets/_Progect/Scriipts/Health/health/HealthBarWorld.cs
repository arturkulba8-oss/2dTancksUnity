using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Сердечки над вражеским танком. Едут вместе с танком, но не поворачиваются.
// Висит на дочернем объекте танка (HealthBar). Выше полоски перезарядки, поэтому ей не мешает.
public class HealthBarWorld : MonoBehaviour
{
    [SerializeField] Health health;                 // найдётся у родителя сам
    [SerializeField] Sprite fullSprite;
    [SerializeField] Sprite emptySprite;
    [SerializeField] Sprite[] popFrames;
    [SerializeField] float popFps = 16f;

    [Header("Положение и размер")]
    [SerializeField] Vector2 offset = new Vector2(0f, 1.25f);   // полоска перезарядки на 0.8, сердечки выше
    [SerializeField] float heartWorldSize = 0.45f;              // ширина кадра 40x40 в мировых единицах
    [SerializeField] float spacing = 0.28f;                     // расстояние между центрами сердец
    [SerializeField] int sortingOrder = 25;                     // выше полоски перезарядки (20)
    [SerializeField] bool hideWhenFull = false;                 // показывать только после первого попадания

    readonly List<SpriteRenderer> slots = new List<SpriteRenderer>();
    readonly List<Coroutine> running = new List<Coroutine>();
    Transform root;
    int shown;

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        root = health != null ? health.transform : transform.parent;
        HeartAnim.SortFrames(popFrames);
    }

    void OnEnable()
    {
        if (health == null) { enabled = false; return; }
        health.Changed += OnChanged;
        Rebuild(health.Max, health.Current);
    }

    void OnDisable()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    void LateUpdate() { Follow(); }

    // Над танком, не вращается и не наследует его масштаб
    void Follow()
    {
        if (root == null) return;
        transform.rotation = Quaternion.identity;
        transform.position = root.position + (Vector3)offset;

        float parentScale = transform.parent != null ? Mathf.Max(0.0001f, transform.parent.lossyScale.x) : 1f;
        transform.localScale = new Vector3(1f / parentScale, 1f / parentScale, 1f);
    }

    void OnChanged(int current, int max)
    {
        if (max != slots.Count) { Rebuild(max, current); return; }

        if (current < shown)
        {
            for (int i = shown - 1; i >= current; i--) PopSlot(i);
        }
        else if (current > shown)
        {
            for (int i = shown; i < current; i++) FillSlot(i);
        }
        shown = current;
        UpdateVisibility();
    }

    void Rebuild(int max, int current)
    {
        StopAllCoroutines();
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) Destroy(slots[i].gameObject);
        slots.Clear();
        running.Clear();

        float nativeWidth = fullSprite != null ? fullSprite.rect.width / fullSprite.pixelsPerUnit : 0.4f;
        float scale = heartWorldSize / Mathf.Max(0.0001f, nativeWidth);

        for (int i = 0; i < max; i++)
        {
            var go = new GameObject("Heart_" + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((i - (max - 1) * 0.5f) * spacing, 0f, 0f);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.sprite = i < current ? fullSprite : emptySprite;

            slots.Add(sr);
            running.Add(null);
        }
        shown = current;
        Follow();
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        bool visible = !(hideWhenFull && health.IsFull);
        for (int i = 0; i < slots.Count; i++) slots[i].enabled = visible;
    }

    void PopSlot(int index)
    {
        StopSlot(index);
        var sr = slots[index];
        running[index] = StartCoroutine(HeartAnim.Pop(s => { if (sr != null) sr.sprite = s; }, popFrames, popFps, emptySprite));
    }

    void FillSlot(int index)
    {
        StopSlot(index);
        var sr = slots[index];
        sr.sprite = fullSprite;
        running[index] = StartCoroutine(HeartAnim.Punch(sr.transform));
    }

    void StopSlot(int index)
    {
        if (running[index] != null) StopCoroutine(running[index]);
        running[index] = null;
    }
}
