using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] float lifetime = 0.4f;
    void Start() { Destroy(gameObject, lifetime); }
}