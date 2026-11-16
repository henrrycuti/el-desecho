using UnityEngine;

namespace ElDesecho {
// Piedra lanzada por el jugador para distraer a El Deshecho.
// Al primer golpe hace ruido en ese punto (mas fuerte sobre metal o agua) y la criatura va a investigarlo.
[RequireComponent(typeof(Rigidbody))]
public sealed class ThrownStone : MonoBehaviour {
    public float radius = 11, loudRadius = 15;
    bool landed;
    static AudioClip clack;

    void Start() => Destroy(gameObject, 10);

    void OnCollisionEnter(Collision hit) {
        if (landed) return;
        if (hit.collider.GetComponentInParent<FirstPersonController>() != null) return;
        landed = true;
        var r = hit.collider.GetComponent<Renderer>();
        string surface = r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "";
        bool loud = surface.Contains("Metal") || surface.Contains("Oxido") || surface.Contains("Agua") || surface.Contains("Mesa");
        Noise.EmitDistraction(transform.position, loud ? loudRadius : radius);
        if (clack == null) clack = Sounds.Thump("Piedra");
        AudioSource.PlayClipAtPoint(clack, transform.position, loud ? 1 : .7f);
        StoryEvents.Raise("piedra-lanzada");
    }
}
}
