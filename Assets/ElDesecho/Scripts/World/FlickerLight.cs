using UnityEngine;

namespace ElDesecho {
// Luz que parpadea de forma irregular, como una lampara de emergencia vieja.
[RequireComponent(typeof(Light))]
public sealed class FlickerLight : MonoBehaviour {
    public float speed = 7, depth = .6f;
    Light lamp;
    float baseIntensity, seed;

    void Awake() {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
        seed = Random.value * 100;
    }

    void Update() {
        float n = Mathf.PerlinNoise(seed, Time.time * speed);
        lamp.intensity = baseIntensity * (1 - depth + depth * (n > .3f ? n : 0));
    }
}
}
