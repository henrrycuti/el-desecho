using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Pantalla de juego: objetivo arriba, marcador, GPS, subtitulos, aviso de interaccion, pausa y controles tactiles.
public sealed class Hud : MonoBehaviour {
    bool paused;

    void Start() => ApplyCursor();

    void Update() {
        var player = FirstPersonController.Current;
        bool pausePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (player != null && player.Touch != null) pausePressed |= player.Touch.PausePressed;
        if (pausePressed && ChapterManager.Instance != null && !ChapterManager.Instance.Busy) SetPaused(!paused);
        if (!paused && Cursor.lockState != CursorLockMode.Locked && !TouchControls.Enabled && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ApplyCursor();
    }

    void SetPaused(bool value) {
        paused = value;
        Time.timeScale = paused ? 0 : 1;
        AudioListener.pause = paused;
        ApplyCursor();
    }

    void ApplyCursor() {
        bool free = paused || TouchControls.Enabled;
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    void OnDestroy() {
        Time.timeScale = 1;
        AudioListener.pause = false;
    }

    void OnGUI() {
        var player = FirstPersonController.Current;
        var objectives = ObjectiveManager.Instance;
        float s = Ui.Scale, w = Screen.width, h = Screen.height;
        if (player == null || objectives == null) return;

        var step = objectives.Current;
        if (step != null && !string.IsNullOrEmpty(step.objective)) {
            var style = Ui.Label(Mathf.RoundToInt(20 * s), TextAnchor.UpperCenter);
            float pulse = Time.time - objectives.ObjectiveShownAt < 2.5f ? .55f : .35f;
            Ui.Fill(new Rect(w * .2f, 10 * s, w * .6f, 40 * s), new Color(0, 0, 0, pulse));
            Ui.Shadowed(new Rect(w * .2f, 18 * s, w * .6f, 40 * s), step.objective, style);
        }

        DrawMarker(player, step, s);
        DrawGps(player, objectives, s, w);

        var line = objectives.Subtitle;
        if (!string.IsNullOrEmpty(line)) {
            var style = Ui.Label(Mathf.RoundToInt(21 * s), TextAnchor.LowerCenter, FontStyle.Italic);
            Ui.Shadowed(new Rect(w * .12f, h - 150 * s, w * .76f, 90 * s), line, style);
        }

        if (player.Focus != null) {
            var style = Ui.Label(Mathf.RoundToInt(20 * s), TextAnchor.MiddleCenter, FontStyle.Bold);
            string key = TouchControls.Enabled ? "USAR" : "E";
            Ui.Shadowed(new Rect(w * .3f, h * .56f, w * .4f, 40 * s), key + " · " + player.Focus.prompt, style);
        }
        GUI.Label(new Rect(w / 2 - 8, h / 2 - 12, 20, 24), "·", Ui.Label(Mathf.RoundToInt(24 * s)));

        player.Touch.Draw();
        if (!TouchControls.Enabled && step != null && Time.time - objectives.ObjectiveShownAt < 6 && objectives.Index == 0)
            Ui.Shadowed(new Rect(16 * s, h - 40 * s, w, 30 * s), "WASD caminar · Raton mirar · Shift correr · C agacharse · F linterna · E usar · Esc pausa", Ui.Label(Mathf.RoundToInt(15 * s)));

        if (paused) DrawPause(s, w, h);
    }

    static void DrawMarker(FirstPersonController player, ObjectiveStep step, float s) {
        if (step == null || step.marker == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        var p = cam.WorldToScreenPoint(step.marker.position);
        if (p.z <= 0) return;
        float distance = Vector3.Distance(player.transform.position, step.marker.position);
        var style = Ui.Label(Mathf.RoundToInt(16 * s), TextAnchor.MiddleCenter, FontStyle.Bold);
        var r = new Rect(p.x - 40 * s, Screen.height - p.y - 34 * s, 80 * s, 40 * s);
        Ui.Shadowed(r, "◆\n" + Mathf.RoundToInt(distance) + " m", style);
    }

    static void DrawGps(FirstPersonController player, ObjectiveManager objectives, float s, float w) {
        var target = objectives.GpsTarget;
        if (target == null) return;
        float signal = objectives.Current.gpsSignal;
        var box = new Rect(w - 210 * s, 64 * s, 190 * s, 92 * s);
        Ui.Fill(box, new Color(.02f, .08f, .05f, .75f));
        var text = Ui.Label(Mathf.RoundToInt(16 * s), TextAnchor.UpperLeft, FontStyle.Bold);
        text.normal.textColor = new Color(.45f, 1, .6f);
        bool dropout = signal < 1 && Mathf.PerlinNoise(Time.time * 3, 7) > signal + .25f;
        string bars = new string('|', Mathf.RoundToInt(signal * 5)).PadRight(5, '.');
        GUI.Label(new Rect(box.x + 10 * s, box.y + 6 * s, box.width, 24 * s), "GPS  [" + bars + "]", text);
        if (dropout) {
            GUI.Label(new Rect(box.x + 10 * s, box.y + 36 * s, box.width, 40 * s), "BUSCANDO SENAL...", text);
            return;
        }
        float distance = objectives.GpsDistance(player.transform.position);
        GUI.Label(new Rect(box.x + 64 * s, box.y + 36 * s, box.width, 40 * s), Mathf.RoundToInt(distance) + " m", Ui.Label(Mathf.RoundToInt(26 * s), TextAnchor.UpperLeft, FontStyle.Bold));
        var to = target.position - player.transform.position;
        float angle = Vector3.SignedAngle(player.transform.forward, new Vector3(to.x, 0, to.z), Vector3.up);
        angle += (1 - signal) * Mathf.Sin(Time.time * 9) * 25;
        var center = new Vector2(box.x + 34 * s, box.y + 56 * s);
        var matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.Label(new Rect(center.x - 16 * s, center.y - 18 * s, 32 * s, 36 * s), "▲", Ui.Label(Mathf.RoundToInt(28 * s), TextAnchor.MiddleCenter));
        GUI.matrix = matrix;
    }

    void DrawPause(float s, float w, float h) {
        Ui.Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, .75f));
        GUI.Label(new Rect(0, h * .25f, w, 60 * s), "PAUSA", Ui.Label(Mathf.RoundToInt(40 * s), TextAnchor.MiddleCenter, FontStyle.Bold));
        float bw = Mathf.Min(380 * s, w - 40), x = (w - bw) / 2;
        var button = Ui.Button(Mathf.RoundToInt(20 * s));
        if (GUI.Button(new Rect(x, h * .45f, bw, 52 * s), "CONTINUAR", button)) SetPaused(false);
        if (GUI.Button(new Rect(x, h * .45f + 64 * s, bw, 52 * s), "MENU PRINCIPAL", button)) {
            SetPaused(false);
            ChapterManager.Instance.ToMenu();
        }
    }
}
}
