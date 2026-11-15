using UnityEngine;

namespace ElDesecho {
// Musica de tension original (generada por codigo):
// - Capa de amenaza: latido y zumbido grave que suben cuanto mas cerca esta una criatura activa.
// - Capa de persecucion: tambores rapidos cuando una criatura persigue al jugador.
// Ambas bajan poco a poco cuando la criatura se rinde o se aleja.
public sealed class ThreatMusic : MonoBehaviour {
    public float hearRange = 22, fadeSpeed = .6f;
    AudioSource dread, chase;
    float dreadVolume, chaseVolume;

    void Start() {
        dread = Make(Heartbeat(), .0f);
        chase = Make(Sounds.Drums("Persecucion"), .0f);
    }

    AudioSource Make(AudioClip clip, float volume) {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.volume = volume;
        s.spatialBlend = 0;
        s.Play();
        return s;
    }

    void Update() {
        var player = FirstPersonController.Current;
        float closeness = 0;
        bool chasing = false;
        if (player != null)
            foreach (var c in FindObjectsByType<CreatureBrain>(FindObjectsSortMode.None)) {
                if (c.State is CreatureState.Dormant or CreatureState.Stunned) continue;
                float d = Vector3.Distance(c.transform.position, player.transform.position);
                closeness = Mathf.Max(closeness, Mathf.Clamp01(1 - d / hearRange));
                if (c.State == CreatureState.Chase && d < hearRange * 1.5f) chasing = true;
            }
        float targetDread = closeness * .7f, targetChase = chasing ? .55f : 0;
        dreadVolume = Mathf.MoveTowards(dreadVolume, targetDread, Time.deltaTime * fadeSpeed);
        chaseVolume = Mathf.MoveTowards(chaseVolume, targetChase, Time.deltaTime * (chasing ? 2 : fadeSpeed * .5f));
        dread.volume = dreadVolume;
        dread.pitch = 1 + closeness * .5f;
        chase.volume = chaseVolume;
        chase.pitch = 1 + closeness * .25f;
    }

    // Latido doble sobre un zumbido grave que oscila: dura 1 s y se repite.
    static AudioClip Heartbeat() {
        const int rate = 22050;
        var data = new float[rate];
        for (int i = 0; i < data.Length; i++) {
            float t = i / (float)rate;
            float drone = Mathf.Sin(2 * Mathf.PI * 46 * t) * .25f + Mathf.Sin(2 * Mathf.PI * 69.5f * t) * .12f * (1 + Mathf.Sin(t * 6.28f));
            float beat = 0;
            foreach (float at in new[] { 0f, .22f }) {
                float k = t - at;
                if (k >= 0 && k < .18f) beat += Mathf.Sin(2 * Mathf.PI * 52 * k) * Mathf.Exp(-k * 22) * (at == 0 ? 1 : .7f);
            }
            data[i] = drone + beat * .9f;
        }
        var clip = AudioClip.Create("Latido", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
}
