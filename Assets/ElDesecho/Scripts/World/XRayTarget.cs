using UnityEngine;

namespace ElDesecho {
public enum XRayKind { Creature, Item, Blocked }

// Algo que las gafas de exploracion muestran a traves de las paredes.
public sealed class XRayTarget : MonoBehaviour {
    public XRayKind kind;
    public string label;
    public Vector3 offset = Vector3.up;
    public Vector3 Position => transform.position + offset;
}
}
