using UnityEngine;

namespace ElDesecho {
// Chorro de vapor caliente que se abre y se cierra a intervalos. Silba un momento antes de abrirse;
// si el jugador esta dentro mientras sale el vapor, se quema y vuelve al ultimo punto seguro.
[RequireComponent(typeof(BoxCollider))]
public sealed class SteamJet : MonoBehaviour {
    public float cycle = 4.5f, active = 1.6f, warning = .8f, offset;
    public ParticleSystem steam;
    public bool Active { get; private set; }
    bool inside;
    AudioSource hiss;

    void Start() {
        GetComponent<BoxCollider>().isTrigger = true;
        hiss = gameObject.AddComponent<AudioSource>();
        hiss.spatialBlend = 1;
        hiss.maxDistance = 14;
        hiss.rolloffMode = AudioRolloffMode.Linear;
        hiss.loop = true;
        hiss.clip = Sounds.Noise("Vapor", 1.5f, .25f);
    }

    void Update() {
        float t = Mathf.Repeat(Time.time + offset, cycle);
        bool warn = t >= cycle - active - warning && t < cycle - active;
        Active = t >= cycle - active;
        if (steam != null) {
            var emission = steam.emission;
            emission.rateOverTime = Active ? 120 : warn ? 12 : 0;
        }
        hiss.volume = Active ? .45f : warn ? .12f : 0;
        if (!hiss.isPlaying) hiss.Play();
        if (Active && inside && !ChapterManager.TestMode && ChapterManager.Instance != null && !ChapterManager.Instance.Busy) {
            FirstPersonController.Current?.Shake(1);
            ChapterManager.Instance.Caught("El vapor te quemo");
        }
    }

    void OnTriggerEnter(Collider other) { if (other.GetComponentInParent<FirstPersonController>() != null) inside = true; }
    void OnTriggerExit(Collider other) { if (other.GetComponentInParent<FirstPersonController>() != null) inside = false; }
}
}
