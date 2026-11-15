using UnityEngine;

namespace ElDesecho {
// Silueta al fondo de un pasillo que solo se ve cuando parpadea la luz de emergencia y desaparece al acercarse.
public sealed class ShadowFigure : MonoBehaviour {
    public GameObject figure;
    public Light lamp;
    public float vanishDistance = 12;
    float visibleUntil, next;

    void Update() {
        var player = FirstPersonController.Current;
        bool near = player != null && Vector3.Distance(player.transform.position, transform.position) < vanishDistance;
        if (Time.time > next) {
            next = Time.time + Random.Range(2.5f, 6f);
            visibleUntil = Time.time + Random.Range(.15f, .4f);
            if (lamp != null) lamp.intensity = 3;
        }
        bool show = !near && Time.time < visibleUntil;
        if (figure != null && figure.activeSelf != show) figure.SetActive(show);
        if (lamp != null && Time.time >= visibleUntil) lamp.intensity = Mathf.MoveTowards(lamp.intensity, .3f, Time.deltaTime * 8);
    }
}
}
