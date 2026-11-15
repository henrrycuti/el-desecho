using UnityEngine;

namespace ElDesecho {
// Movimiento lento de una camara de cinematica (acercamiento o desplazamiento lateral).
public sealed class CameraDrift : MonoBehaviour {
    public Vector3 velocity = new(0, 0, .25f);
    void Update() => transform.position += transform.TransformDirection(velocity) * Time.deltaTime;
}
}
