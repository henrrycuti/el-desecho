using UnityEngine;

namespace ElDesecho {
// Punto donde aparece el jugador al entrar en la escena. El primero de la lista es el de por defecto.
public sealed class SpawnPoint : MonoBehaviour {
    public string id = "inicio";

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * .9f, .4f);
        Gizmos.DrawRay(transform.position + Vector3.up * .9f, transform.forward);
    }
}
}
