using System;
using System.Collections;
using UnityEngine;

// Общие приёмы для сердечек: покадровое «лопание» и «пульс» при лечении.
public static class HeartAnim
{
    // Кадры из спрайт-листа могут прийти в порядке _0, _1, _10, _2... — сортируем по номеру
    public static void SortFrames(Sprite[] frames)
    {
        if (frames == null) return;
        Array.Sort(frames, (a, b) => Number(a).CompareTo(Number(b)));
    }

    static int Number(Sprite s)
    {
        if (s == null) return -1;
        int i = s.name.LastIndexOf('_');
        return int.TryParse(s.name.Substring(i + 1), out int n) ? n : -1;
    }

    // Проигрывает кадры, в конце ставит finalSprite (пустое сердце)
    public static IEnumerator Pop(Action<Sprite> setSprite, Sprite[] frames, float fps, Sprite finalSprite)
    {
        float step = 1f / Mathf.Max(1f, fps);
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                setSprite(frames[i]);
                yield return new WaitForSecondsRealtime(step);
            }
        }
        setSprite(finalSprite);
    }

    // Короткое увеличение и возврат (появилось новое сердце)
    public static IEnumerator Punch(Transform t, float amount = 1.3f, float time = 0.25f)
    {
        if (t == null) yield break;
        Vector3 baseScale = t.localScale;
        float e = 0f;
        while (e < time)
        {
            if (t == null) yield break;
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / time);
            t.localScale = baseScale * (1f + (amount - 1f) * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (t != null) t.localScale = baseScale;
    }
}
