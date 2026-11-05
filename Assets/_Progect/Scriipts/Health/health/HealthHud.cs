using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Сердечки игрока по центру сверху экрана. Висит на панели в Canvas.
public class HealthHud : MonoBehaviour
{
    [SerializeField] Image heartPrefab;          // префаб одного сердца (UI Image)
    [SerializeField] Sprite fullSprite;          // heart_full
    [SerializeField] Sprite emptySprite;         // heart_empty
    [SerializeField] Sprite[] popFrames;         // 10 кадров из heart_pop_sheet
    [SerializeField] float popFps = 16f;
    [SerializeField] string targetTag = "Player";

    readonly List<Image> slots = new List<Image>();
    readonly List<Coroutine> running = new List<Coroutine>();
    Health target;
    int shown;               // сколько полных сердец сейчас показано
    float nextSearch;

    void Awake() { HeartAnim.SortFrames(popFrames); }

    void Update()
    {
        if (target == null) TryBind();
    }

    void OnDestroy() { Unbind(); }

    void TryBind()
    {
        if (Time.unscaledTime < nextSearch) return;
        nextSearch = Time.unscaledTime + 0.3f;

        var p = GameObject.FindWithTag(targetTag);
        if (p == null) return;
        var h = p.GetComponent<Health>();
        if (h != null) Bind(h);
    }

    void Bind(Health h)
    {
        Unbind();
        target = h;
        target.Changed += OnChanged;
        Rebuild(h.Max, h.Current);
    }

    void Unbind()
    {
        if (target != null) target.Changed -= OnChanged;
        target = null;
    }

    void OnChanged(int current, int max)
    {
        if (max != slots.Count) { Rebuild(max, current); return; }

        if (current < shown)
        {
            for (int i = shown - 1; i >= current; i--) PopSlot(i);     // справа налево
        }
        else if (current > shown)
        {
            for (int i = shown; i < current; i++) FillSlot(i);
        }
        shown = current;
    }

    void Rebuild(int max, int current)
    {
        StopAllCoroutines();
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) Destroy(slots[i].gameObject);
        slots.Clear();
        running.Clear();

        for (int i = 0; i < max; i++)
        {
            var img = Instantiate(heartPrefab, transform);
            img.raycastTarget = false;                        // сердечки не перехватывают касания
            img.sprite = i < current ? fullSprite : emptySprite;
            slots.Add(img);
            running.Add(null);
        }
        shown = current;
    }

    void PopSlot(int index)
    {
        StopSlot(index);
        var img = slots[index];
        running[index] = StartCoroutine(HeartAnim.Pop(s => { if (img != null) img.sprite = s; }, popFrames, popFps, emptySprite));
    }

    void FillSlot(int index)
    {
        StopSlot(index);
        var img = slots[index];
        img.sprite = fullSprite;
        running[index] = StartCoroutine(HeartAnim.Punch(img.transform));
    }

    void StopSlot(int index)
    {
        if (running[index] != null) StopCoroutine(running[index]);
        running[index] = null;
        slots[index].transform.localScale = Vector3.one;
    }
}
