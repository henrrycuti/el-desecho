using UnityEngine;

namespace ElDesecho {
// Mueve o gira un objeto cuando ocurre un evento de historia (abrir una tapa, una compuerta, una puerta).
public sealed class EventMover : MonoBehaviour {
    public string eventId;
    public Vector3 moveBy;
    public Vector3 rotateBy;
    public float duration = 1.2f;

    Vector3 startPosition, endPosition;
    Quaternion startRotation, endRotation;
    float progress = -1;

    void OnEnable() => StoryEvents.Raised += OnEvent;
    void OnDisable() => StoryEvents.Raised -= OnEvent;

    void OnEvent(string id) {
        if (id != eventId || progress >= 0) return;
        startPosition = transform.localPosition;
        endPosition = startPosition + moveBy;
        startRotation = transform.localRotation;
        endRotation = startRotation * Quaternion.Euler(rotateBy);
        progress = 0;
    }

    void Update() {
        if (progress < 0 || progress >= 1) return;
        progress = Mathf.Min(1, progress + Time.deltaTime / duration);
        float t = Mathf.SmoothStep(0, 1, progress);
        transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
        transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
    }
}
}
