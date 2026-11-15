using UnityEngine;

namespace ElDesecho {
// Zona invisible: cuando el jugador entra, emite su evento de historia (una sola vez).
[RequireComponent(typeof(BoxCollider))]
public sealed class StoryTrigger : MonoBehaviour {
    public string eventId;
    bool fired;

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        if (fired || other.GetComponentInParent<FirstPersonController>() == null) return;
        fired = true;
        StoryEvents.Raise(eventId);
    }
}
}
