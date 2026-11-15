using UnityEngine;

namespace ElDesecho {
// Baliza de alarma: un foco rojo que gira.
public sealed class Beacon : MonoBehaviour {
    public float speed = 220;
    void Update() => transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);
}
}
