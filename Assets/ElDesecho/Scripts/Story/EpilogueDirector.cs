using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace ElDesecho {
// Epilogo: la linea de tiempo (Timeline + Cinemachine) mueve las camaras; este componente pone los subtitulos,
// hace crecer a la masa, abre su boca, la hace rugir, funde a negro y muestra los creditos.
// E, Esc o un toque saltan al final. Al terminar se marca el juego como completado y se vuelve al menu.
public sealed class EpilogueDirector : MonoBehaviour {
    [Serializable] public struct Line { public float at; public string text; }

    public PlayableDirector director;
    public Line[] lines = new Line[0];
    public Transform grower;
    public float growFrom = 34, growTo = 41, roarAt = 43, blackAt = 46, creditsLength = 12;
    public AudioClip roar;
    public SkinnedMeshRenderer mouth;

    public float Time01 => director != null ? (float)director.time : 0;
    public bool Finished { get; private set; }

    float creditsStart = -1;
    bool roared;
    AudioSource source;
    int mouthIndex = -1;

    void Start() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        source = gameObject.AddComponent<AudioSource>();
        if (mouth != null)
            for (int i = 0; i < mouth.sharedMesh.blendShapeCount; i++)
                if (mouth.sharedMesh.GetBlendShapeName(i).EndsWith("MawOpen")) mouthIndex = i;
        director.Play();
    }

    public void Skip() {
        if (creditsStart < 0) {
            director.time = blackAt;
            director.Evaluate();
        } else creditsStart = -999;
    }

    void Update() {
        bool skip = !ChapterManager.TestMode && (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        if (skip) Skip();
        float t = Time01;
        if (grower != null && t >= growFrom) {
            float k = Mathf.Clamp01((t - growFrom) / (growTo - growFrom));
            grower.localScale = Vector3.one * Mathf.Lerp(.6f, 1.5f, k);
            grower.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 20) * 4 * (1 - k * .5f), 0);
        }
        if (mouthIndex >= 0) mouth.SetBlendShapeWeight(mouthIndex, t >= roarAt - 1.5f ? 100 : 10);
        if (!roared && t >= roarAt) {
            roared = true;
            if (roar != null) source.PlayOneShot(roar, 1);
        }
        if (creditsStart < 0 && creditsStart > -900 && t >= blackAt) {
            creditsStart = Time.unscaledTime;
            director.Pause();
        }
        if (creditsStart > 0 && Time.unscaledTime - creditsStart > creditsLength || creditsStart <= -900) Finish();
    }

    void Finish() {
        if (Finished) return;
        Finished = true;
        PlayerPrefs.SetInt("eldesecho.completado", 1);
        PlayerPrefs.Save();
        ChapterManager.Instance?.ToMenu();
    }

    void OnGUI() {
        float s = Ui.Scale, w = Screen.width, h = Screen.height;
        float t = Time01;
        if (creditsStart > 0 || t >= blackAt - 1) {
            float a = creditsStart > 0 ? 1 : Mathf.Clamp01(t - (blackAt - 1));
            Ui.Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, a));
            if (creditsStart > 0) {
                float k = Time.unscaledTime - creditsStart;
                var big = Ui.Label(Mathf.RoundToInt(54 * s), TextAnchor.MiddleCenter, FontStyle.Bold);
                var small = Ui.Label(Mathf.RoundToInt(22 * s), TextAnchor.MiddleCenter);
                float y = h * .7f - k * 40 * s;
                GUI.Label(new Rect(0, y, w, 80 * s), "EL DESECHO", big);
                string[] credits = { "Historia y direccion: Equipo Tecsup", "Diseno de la criatura: Equipo Tecsup", "Desarrollo: Equipo Tecsup con Claude", "Hecho con Unity", "", "Gracias por jugar" };
                for (int i = 0; i < credits.Length; i++) GUI.Label(new Rect(0, y + (110 + i * 40) * s, w, 36 * s), credits[i], small);
            }
        }
        string line = null;
        foreach (var l in lines) if (t >= l.at && t < l.at + Mathf.Max(3.5f, l.text.Length * .07f)) line = l.text;
        if (line != null && creditsStart < 0) {
            var box = new Rect(w * .1f, h - 150 * s, w * .8f, 100 * s);
            Ui.Fill(box, new Color(0, 0, 0, .6f));
            GUI.Label(box, line, Ui.Label(Mathf.RoundToInt(22 * s), TextAnchor.MiddleCenter));
        }
        if (!TouchControls.Enabled) GUI.Label(new Rect(w - 220 * s, h - 34 * s, 210 * s, 30 * s), "E · saltar", Ui.Label(Mathf.RoundToInt(15 * s), TextAnchor.MiddleRight));
    }
}
}
