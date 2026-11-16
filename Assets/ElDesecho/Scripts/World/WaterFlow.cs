using UnityEngine;

namespace ElDesecho {
// Mueve despacio la textura de ondas del agua para que parezca que corre.
public sealed class WaterFlow : MonoBehaviour {
    public Material[] materials;
    public Vector2 speed = new(.02f, .035f);

    void Update() {
        var offset = speed * Time.time;
        foreach (var m in materials)
            if (m != null) m.SetTextureOffset("_BaseMap", offset);
    }
}
}
