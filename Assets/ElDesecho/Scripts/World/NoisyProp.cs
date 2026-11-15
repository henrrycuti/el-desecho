using UnityEngine;

namespace ElDesecho {
// Lata, botella o rejilla suelta: si el jugador la pisa sin agacharse, suena.
[RequireComponent(typeof(BoxCollider))]
public sealed class NoisyProp : MonoBehaviour {
    public float radius = 13;
    public Transform visual;
    float ready;

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other) {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player == null || Time.time < ready) return;
        ready = Time.time + 2;
        float r = player.Crouched ? radius * .35f : radius;
        Noise.Emit(transform.position, r);
        player.Clatter(r);
        if (visual != null) visual.localRotation *= Quaternion.Euler(0, 35, Random.Range(-25f, 25f));
    }
}
}
