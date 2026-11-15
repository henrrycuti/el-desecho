using UnityEngine;

namespace ElDesecho {
// Escondite: tuberia ancha, casillero o hueco bajo una pasarela. Dentro, el jugador casi no hace ruido
// y la criatura no puede alcanzarlo.
[RequireComponent(typeof(BoxCollider))]
public sealed class HidingSpot : MonoBehaviour {
    public bool needsCrouch = true;
    public string label = "Escondido";

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player != null) player.EnterHiding(this);
    }

    void OnTriggerExit(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player != null) player.ExitHiding(this);
    }
}
}
