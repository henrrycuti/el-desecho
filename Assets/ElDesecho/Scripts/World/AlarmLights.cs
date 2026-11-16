using UnityEngine;

namespace ElDesecho {
// Alarma del laboratorio: las luces frias pasan a rojo y parpadean, y suena una sirena.
public sealed class AlarmLights : MonoBehaviour {
    public Light[] lights = new Light[0];
    public Renderer[] panels = new Renderer[0];
    public Material alarmPanel;
    public UnityEngine.Rendering.Volume alarmVolume;
    public Color ambientAfter = new(.2f, .035f, .03f);
    public Color fogAfter = new(.05f, 0, 0);
    public bool Active { get; private set; }
    AudioSource siren;

    public void Activate() {
        if (Active) return;
        Active = true;
        foreach (var l in lights) {
            if (l == null) continue;
            l.color = new Color(1, .08f, .05f);
            l.intensity *= 1.1f;
            if (l.GetComponent<FlickerLight>() == null) l.gameObject.AddComponent<FlickerLight>().depth = .75f;
        }
        foreach (var r in panels) if (r != null && alarmPanel != null) r.sharedMaterial = alarmPanel;
        if (alarmVolume != null) alarmVolume.weight = 1;
        // Balizas giratorias en algunas luces.
        for (int i = 0; i < lights.Length; i += 5) {
            if (lights[i] == null) continue;
            var beacon = new GameObject("Baliza").AddComponent<Light>();
            beacon.transform.SetParent(lights[i].transform, false);
            beacon.transform.localRotation = Quaternion.Euler(25, 0, 0);
            beacon.type = LightType.Spot;
            beacon.spotAngle = 35;
            beacon.range = 12;
            beacon.intensity = 6;
            beacon.color = new Color(1, .05f, .02f);
            beacon.shadows = LightShadows.None;
            beacon.gameObject.AddComponent<Beacon>();
        }
        RenderSettings.ambientLight = ambientAfter;
        RenderSettings.fogColor = fogAfter;
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = fogAfter;
        siren = gameObject.AddComponent<AudioSource>();
        siren.clip = MakeSiren();
        siren.loop = true;
        siren.volume = .16f;
        siren.Play();
        activatedAt = Time.time;
    }

    float activatedAt;

    // La alarma empieza fuerte y en unos 25 s queda de fondo, para no cansar.
    void Update() {
        if (siren != null) siren.volume = Mathf.Lerp(.16f, .06f, (Time.time - activatedAt) / 25);
    }

    // Alarma de instalacion de dos tonos graves, apagada y con eco, como oida a traves de los muros.
    static AudioClip MakeSiren() {
        const int rate = 22050;
        const float length = 2.4f;
        int n = (int)(rate * length);
        var data = new float[n];
        // Dos tonos de 0,7 s separados por silencios cortos (re y la bemol graves).
        var tones = new (float start, float freq)[] { (0, 293.7f), (1.2f, 207.7f) };
        foreach (var (start, freq) in tones)
            for (int i = 0; i < rate * .7f; i++) {
                float t = i / (float)rate;
                float env = Mathf.Clamp01(t / .04f) * Mathf.Clamp01((.7f - t) / .12f);
                // Onda cuadrada suave: solo armonicos impares bajos (sin chillido agudo).
                float s = Mathf.Sin(2 * Mathf.PI * freq * t) + Mathf.Sin(2 * Mathf.PI * freq * 3 * t) / 5 + Mathf.Sin(2 * Mathf.PI * freq * 5 * t) / 14;
                data[(int)(start * rate) + i] += s * env * .5f;
            }
        // Eco que da sensacion de pasillos largos (se repite en bucle sin corte).
        int delay = (int)(rate * .23f);
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < n; i++) data[i] += data[(i - delay + n) % n] * .35f;
        float peak = .001f;
        foreach (var s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
        for (int i = 0; i < n; i++) data[i] = data[i] / peak * .7f;
        var clip = AudioClip.Create("Alarma", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
}
