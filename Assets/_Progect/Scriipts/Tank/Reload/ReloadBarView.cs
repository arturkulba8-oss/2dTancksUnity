using System;
using UnityEngine;

public class ReloadBarView : MonoBehaviour
{
    [Serializable]
    public class BarSet
    {
        public int cells = 1;        // сколько секций (патронов) в этой полоске: 1, 3, 5
        public Sprite[] frames;      // кадры от пустого (0) до полного (последний)
    }

    [SerializeField] TankReload reload;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] BarSet[] sets;
    [SerializeField] Vector2 worldOffset = new Vector2(0f, 0.8f);   // где над танком висит полоска

    Sprite[] current;
    bool visible;
    Transform follow;

    void Awake()
    {
        if (reload == null) reload = GetComponentInParent<TankReload>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        follow = reload.transform;
        sr.enabled = false;

        // Подспрайты из листа могут прийти в порядке _0, _1, _10, _11... — сортируем по номеру
        foreach (var s in sets)
            if (s.frames != null) Array.Sort(s.frames, (a, b) => FrameNumber(a).CompareTo(FrameNumber(b)));
    }

    void OnEnable()
    {
        reload.ReloadStarted += OnStarted;
        reload.ReloadFinished += OnFinished;
    }

    void OnDisable()
    {
        reload.ReloadStarted -= OnStarted;
        reload.ReloadFinished -= OnFinished;
    }

    void OnStarted(float duration)
    {
        current = Pick(reload.Capacity);          // лист выбирается по количеству патронов
        visible = current != null;
        sr.enabled = visible;
        if (visible) Refresh();
        else Debug.LogWarning("Нет кадров полоски для " + reload.Capacity + " патр.", this);
    }

    void OnFinished()
    {
        visible = false;
        sr.enabled = false;
    }

    void Update() { if (visible) Refresh(); }

    void Refresh()
    {
        int last = current.Length - 1;
        int index = Mathf.Clamp((int)(reload.BarFill * (last + 1)), 0, last);
        sr.sprite = current[index];
    }

    void LateUpdate()
    {
        // Полоска всегда горизонтальна и висит над танком, даже когда корпус поворачивается
        transform.rotation = Quaternion.identity;
        transform.position = follow.position + (Vector3)worldOffset;
    }

    // Точное совпадение по числу секций, иначе ближайший лист (при равенстве берётся больший)
    Sprite[] Pick(int capacity)
    {
        BarSet best = null; int bestDist = int.MaxValue;
        foreach (var s in sets)
        {
            if (s.frames == null || s.frames.Length == 0) continue;
            int d = Mathf.Abs(s.cells - capacity);
            if (d < bestDist || (d == bestDist && s.cells > best.cells)) { best = s; bestDist = d; }
        }
        return best?.frames;
    }

    static int FrameNumber(Sprite s)
    {
        if (s == null) return -1;
        int i = s.name.LastIndexOf('_');
        return int.TryParse(s.name.Substring(i + 1), out int n) ? n : -1;
    }
}