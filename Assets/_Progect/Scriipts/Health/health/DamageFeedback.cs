using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Танк краснеет при попадании и мигает, пока действует неуязвимость.
[RequireComponent(typeof(Health))]
public class DamageFeedback : MonoBehaviour
{
    [SerializeField] Color flashColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] float flashTime = 0.1f;
    [SerializeField] float blinkInterval = 0.08f;                    // мигание в неуязвимости (0 = выключено)
    [SerializeField, Range(0.1f, 1f)] float blinkAlpha = 0.45f;

    Health health;
    readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    readonly List<Color> originals = new List<Color>();
    Coroutine routine;

    void Awake()
    {
        health = GetComponent<Health>();
        // только корпус (там Animator) и башня (там TurretAim): полоска перезарядки и сердечки не красятся
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.GetComponent<Animator>() != null || sr.GetComponent<TurretAim>() != null)
            {
                renderers.Add(sr);
                originals.Add(sr.color);
            }
        }
    }

    void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        Restore();
    }

    void OnDamaged(int amount, GameObject source)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Flash());
    }

    void OnDied(GameObject killer)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        Restore();
    }

    IEnumerator Flash()
    {
        for (int i = 0; i < renderers.Count; i++) if (renderers[i] != null) renderers[i].color = flashColor;
        yield return new WaitForSeconds(flashTime);
        Restore();

        if (blinkInterval > 0f)
        {
            float end = Time.time + Mathf.Max(0f, health.InvulnerableTime - flashTime);
            var wait = new WaitForSeconds(blinkInterval);
            while (Time.time < end)
            {
                SetAlpha(blinkAlpha);
                yield return wait;
                SetAlpha(1f);
                yield return wait;
            }
            Restore();
        }
        routine = null;
    }

    void SetAlpha(float a)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            var c = originals[i]; c.a = originals[i].a * a;
            renderers[i].color = c;
        }
    }

    void Restore()
    {
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].color = originals[i];
    }
}
