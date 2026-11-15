using UnityEngine;

namespace ElDesecho {
// Caida a una zanja o a un pozo: vuelve al ultimo punto seguro.
[RequireComponent(typeof(BoxCollider))]
public sealed class DeathZone : MonoBehaviour {
    public string message = "Caiste a la zanja";

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        if (other.GetComponentInParent<FirstPersonController>() == null) return;
        ChapterManager.Instance?.Caught(message);
    }
}
}
