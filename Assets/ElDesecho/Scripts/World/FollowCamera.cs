using UnityEngine;

namespace ElDesecho {
// Mantiene un efecto (lluvia, goteo) centrado sobre la camara para no llenar todo el mapa de particulas.
public sealed class FollowCamera : MonoBehaviour {
    public Vector3 offset = new(0, 6, 0);

    void LateUpdate() {
        var player = FirstPersonController.Current;
        if (player != null) transform.position = player.head.position + offset;
    }
}
}
