using UnityEngine;

namespace ElDesecho {
// Trampa para animales escondida bajo el agua: atrapa al jugador unos segundos y le hace gritar.
[RequireComponent(typeof(BoxCollider))]
public sealed class Trap : MonoBehaviour {
    public float holdSeconds = 3, screamRadius = 45;
    public Transform jawLeft, jawRight;
    bool sprung;

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player == null || sprung) return;
        sprung = true;
        player.Trap(holdSeconds);
        Noise.Emit(transform.position, screamRadius);
        ObjectiveManager.Instance?.Say("¡Una trampa! El grito resuena por los tuneles...");
        if (jawLeft != null) jawLeft.localRotation = Quaternion.Euler(0, 0, -60);
        if (jawRight != null) jawRight.localRotation = Quaternion.Euler(0, 0, 60);
    }
}
}
