using UnityEngine;

namespace ElDesecho {
// Sonidos sencillos generados por codigo (provisionales hasta tener grabaciones).
public static class Sounds {
    const int Rate = 22050;

    // Ruido filtrado: vapor, rafagas.
    public static AudioClip Noise(string name, float seconds, float smooth) {
        var data = new float[Mathf.CeilToInt(Rate * seconds)];
        float v = 0;
        var rng = new System.Random(name.Length);
        for (int i = 0; i < data.Length; i++) {
            v = Mathf.Lerp(v, (float)rng.NextDouble() * 2 - 1, smooth);
            data[i] = v * .6f;
        }
        return Make(name, data);
    }

    // Golpe sordo contra metal (los golpes detras de las paredes).
    public static AudioClip Thump(string name) {
        var data = new float[Rate / 2];
        for (int i = 0; i < data.Length; i++) {
            float t = i / (float)Rate;
            data[i] = (Mathf.Sin(2 * Mathf.PI * 55 * t) * .9f + Mathf.Sin(2 * Mathf.PI * 140 * t) * .3f) * Mathf.Exp(-t * 9);
        }
        return Make(name, data);
    }

    // Chirrido del mecanismo oxidado.
    public static AudioClip Screech(string name, float seconds) {
        var data = new float[Mathf.CeilToInt(Rate * seconds)];
        float phase = 0;
        var rng = new System.Random(3);
        for (int i = 0; i < data.Length; i++) {
            float t = i / (float)Rate;
            float freq = 900 + 500 * Mathf.Sin(t * 7) + (float)rng.NextDouble() * 120;
            phase += 2 * Mathf.PI * freq / Rate;
            data[i] = Mathf.Sign(Mathf.Sin(phase)) * .25f * Mathf.Clamp01(t * 4) * Mathf.Clamp01((seconds - t) * 3);
        }
        return Make(name, data);
    }

    // Ritmo de tambor para la persecucion: se acelera subiendo el "pitch" de la fuente.
    public static AudioClip Drums(string name) {
        var data = new float[Rate * 2];
        for (int beat = 0; beat < 8; beat++) {
            int start = beat * Rate / 4;
            bool strong = beat % 4 == 0;
            for (int i = 0; i < Rate / 5 && start + i < data.Length; i++) {
                float t = i / (float)Rate;
                data[start + i] += Mathf.Sin(2 * Mathf.PI * (strong ? 60 : 90) * t) * Mathf.Exp(-t * (strong ? 10 : 18)) * (strong ? .9f : .5f);
            }
        }
        return Make(name, data);
    }

    static AudioClip Make(string name, float[] data) {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
}
