using UnityEngine;

namespace ElDesecho {
// Zona que cambia cuanto ruido hacen los pasos (agua profunda, rejillas metalicas) y la velocidad.
[RequireComponent(typeof(BoxCollider))]
public sealed class NoiseZone : MonoBehaviour {
    public float noiseMultiplier = 2;
    public float speedMultiplier = 1;
    public string label = "Agua";

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player != null) player.EnterZone(this);
    }

    void OnTriggerExit(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player != null) player.ExitZone(this);
    }
}
}
