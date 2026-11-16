using UnityEngine;

namespace ElDesecho {
// Musica de tension original (generada por codigo):
// - Capa de amenaza: latido y zumbido grave que suben cuanto mas cerca esta una criatura activa.
// - Capa de persecucion: tambores rapidos cuando una criatura persigue al jugador.
// Ambas bajan poco a poco cuando la criatura se rinde o se aleja.
public sealed class ThreatMusic : MonoBehaviour {
    public float hearRange = 22, fadeSpeed = .6f;
    // Ambiente de fondo de cada capitulo (0 = sin ambiente). Cambia la nota base y el caracter.
    public int mood = 1;
    public float ambientVolume = .32f;
    AudioSource dread, chase, ambient;
    float dreadVolume, chaseVolume;

    void Start() {
        dread = Make(Heartbeat(), .0f);
        chase = Make(Sounds.Drums("Persecucion"), .0f);
        if (mood > 0) ambient = Make(Ambient(mood), ambientVolume);
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
        // La musica de tension tapa al ambiente cuando la criatura se acerca.
        if (ambient != null) ambient.volume = ambientVolume * Mathf.Clamp01(1 - dreadVolume * 1.1f - chaseVolume * 1.4f);
    }

    // Ambiente original de 32 s que se repite: zumbido grave, cuerdas lentas desafinadas,
    // golpes metalicos lejanos con eco y goteo. Cada "mood" usa otra nota y otro ritmo.
    static AudioClip Ambient(int mood) {
        const int rate = 22050;
        const float length = 32;
        int n = (int)(rate * length);
        var data = new float[n];
        var rnd = new System.Random(mood * 977);
        float root = mood switch { 1 => 41.2f, 2 => 36.7f, 3 => 43.65f, 4 => 38.9f, 5 => 46.25f, _ => 32.7f };
        // Intervalos de las cuerdas: quinta, octava, segunda menor (disonancia).
        float[] ratios = { 1, 1.5f, 2, 2.12f, 3 };
        for (int i = 0; i < n; i++) {
            float t = i / (float)rate;
            float loop = 2 * Mathf.PI * t / length;
            float drone = Mathf.Sin(2 * Mathf.PI * root * t) * .3f + Mathf.Sin(2 * Mathf.PI * root * .5f * t) * .2f;
            float strings = 0;
            for (int k = 0; k < ratios.Length; k++) {
                float f = root * 2 * ratios[k];
                float swell = .5f + .5f * Mathf.Sin(loop * (k + 1) + k * 1.7f);
                // Dos osciladores algo desafinados por voz (efecto coro).
                strings += (Mathf.Sin(2 * Mathf.PI * f * t) + Mathf.Sin(2 * Mathf.PI * f * 1.004f * t + k)) * swell * swell * .06f;
            }
            data[i] = drone * (.8f + .2f * Mathf.Sin(loop * 2)) + strings;
        }
        // Golpes metalicos lejanos: tono inarmonico que decae, con dos ecos.
        int hits = 3 + mood % 3;
        for (int h = 0; h < hits; h++) {
            int at = (int)(rnd.NextDouble() * (n - rate * 4));
            float f = 140 + (float)rnd.NextDouble() * 260;
            for (int echo = 0; echo < 3; echo++) {
                int start = at + echo * (int)(rate * .37f);
                float gain = .22f * Mathf.Pow(.45f, echo);
                for (int j = 0; j < rate * 2.5f && start + j < n; j++) {
                    float k = j / (float)rate;
                    float tone = Mathf.Sin(2 * Mathf.PI * f * k) + .6f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * k) + .3f * Mathf.Sin(2 * Mathf.PI * f * 5.4f * k);
                    data[start + j] += tone * Mathf.Exp(-k * 2.2f) * gain;
                }
            }
        }
        // Goteo: gotas cortas y agudas.
        for (int d = 0; d < 9; d++) {
            int at = (int)(rnd.NextDouble() * (n - rate));
            float f = 900 + (float)rnd.NextDouble() * 700;
            for (int j = 0; j < rate * .12f; j++) {
                float k = j / (float)rate;
                data[at + j] += Mathf.Sin(2 * Mathf.PI * (f + k * 3000) * k) * Mathf.Exp(-k * 45) * .08f;
            }
        }
        // Normalizar y suavizar el empalme del bucle.
        float peak = .001f;
        foreach (var s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
        int fade = rate / 4;
        for (int i = 0; i < n; i++) {
            data[i] = data[i] / peak * .8f;
            if (i < fade) data[i] *= i / (float)fade;
            if (i > n - fade) data[i] *= (n - i) / (float)fade;
        }
        var clip = AudioClip.Create("Ambiente" + mood, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
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
