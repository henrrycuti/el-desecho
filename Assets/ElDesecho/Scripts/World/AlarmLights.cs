using UnityEngine;

namespace ElDesecho {
// Alarma del laboratorio: las luces frias pasan a rojo y parpadean, y suena una sirena.
public sealed class AlarmLights : MonoBehaviour {
    public Light[] lights = new Light[0];
    public Renderer[] panels = new Renderer[0];
    public Material alarmPanel;
    public UnityEngine.Rendering.Volume alarmVolume;
    public Color ambientAfter = new(.08f, .01f, .01f);
    public Color fogAfter = new(.05f, 0, 0);
    public bool Active { get; private set; }
    AudioSource siren;

    public void Activate() {
        if (Active) return;
        Active = true;
        foreach (var l in lights) {
            if (l == null) continue;
            l.color = new Color(1, .08f, .05f);
            l.intensity *= .7f;
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
        siren.volume = .22f;
        siren.Play();
    }

    static AudioClip MakeSiren() {
        const int rate = 22050;
        var data = new float[rate * 2];
        float phase = 0;
        for (int i = 0; i < data.Length; i++) {
            float t = i / (float)rate;
            float freq = 520 + 260 * Mathf.Sin(t * Mathf.PI);
            phase += 2 * Mathf.PI * freq / rate;
            data[i] = Mathf.Sin(phase) * .5f + Mathf.Sin(phase * 2) * .15f;
        }
        var clip = AudioClip.Create("Sirena", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
}
