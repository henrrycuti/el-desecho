using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Pantalla de juego: objetivo, marcador, GPS, subtitulos y dialogos, indicador de ruido, escondite,
// aviso de interaccion, pausa con historial de frases y controles tactiles.
public sealed class Hud : MonoBehaviour {
    bool paused;
    static float flashUntil;

    public static void Flash() => flashUntil = Time.unscaledTime + .35f;

    void Start() => ApplyCursor();

    void Update() {
        var player = FirstPersonController.Current;
        bool pausePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (player != null && player.Touch != null) pausePressed |= player.Touch.PausePressed;
        if (ChapterManager.TestMode) pausePressed = false;
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
            string text = step.objective + (step.count > 1 ? $" ({objectives.Progress}/{step.count})" : "");
            float pulse = Time.time - objectives.ObjectiveShownAt < 2.5f ? .6f : .4f;
            Ui.Fill(new Rect(w * .2f, 10 * s, w * .6f, 40 * s), new Color(0, 0, 0, pulse));
            Ui.Shadowed(new Rect(w * .2f, 18 * s, w * .6f, 40 * s), text, style);
        }

        DrawGoggles(s, w, h);
        if (!objectives.InDialogue) {
            DrawMarker(player, step, s);
            DrawGps(player, objectives, s, w);
            DrawStatus(player, s, h);
        }
        DrawSubtitle(objectives, s, w, h);

        if (player.Focus != null && !objectives.InDialogue) {
            var style = Ui.Label(Mathf.RoundToInt(20 * s), TextAnchor.MiddleCenter, FontStyle.Bold);
            string key = TouchControls.Enabled ? "USAR" : "E";
            Ui.Shadowed(new Rect(w * .3f, h * .56f, w * .4f, 40 * s), key + " · " + player.Focus.prompt, style);
        }
        GUI.Label(new Rect(w / 2 - 8, h / 2 - 12, 20, 24), "·", Ui.Label(Mathf.RoundToInt(24 * s)));

        player.Touch.Draw();
        if (!TouchControls.Enabled && step != null && Time.time - objectives.ObjectiveShownAt < 8 && objectives.Index == 0 && !objectives.InDialogue)
            Ui.Shadowed(new Rect(16 * s, h - 40 * s, w, 30 * s), "WASD caminar · Raton mirar · Shift correr · C agacharse · F linterna · E usar o continuar · Esc pausa", Ui.Label(Mathf.RoundToInt(15 * s)));

        if (Time.unscaledTime < flashUntil) Ui.Fill(new Rect(0, 0, w, h), new Color(1, 1, 1, (flashUntil - Time.unscaledTime) / .35f));
        if (paused) DrawPause(objectives, s, w, h);
    }

    static void DrawSubtitle(ObjectiveManager objectives, float s, float w, float h) {
        var line = objectives.Subtitle;
        if (string.IsNullOrEmpty(line)) return;
        var box = new Rect(w * .1f, h - 170 * s, w * .8f, 112 * s);
        Ui.Fill(box, new Color(0, 0, 0, objectives.InDialogue ? .78f : .55f));
        var style = Ui.Label(Mathf.RoundToInt(22 * s), TextAnchor.MiddleCenter);
        GUI.Label(new Rect(box.x + 18 * s, box.y + 8 * s, box.width - 36 * s, box.height - 34 * s), line, style);
        string hint = objectives.InDialogue ? (TouchControls.Enabled ? "Toca USAR para continuar ▸" : "E · Continuar ▸") : (TouchControls.Enabled ? "" : "E · saltar");
        var small = Ui.Label(Mathf.RoundToInt(15 * s), TextAnchor.LowerRight, FontStyle.Bold);
        small.normal.textColor = objectives.InDialogue ? new Color(1, .85f, .4f) : new Color(1, 1, 1, .5f);
        GUI.Label(new Rect(box.x, box.y, box.width - 14 * s, box.height - 8 * s), hint, small);
    }

    // Indicador de ruido (sutil) y estado: escondido, atrapado, adrenalina, sin linterna.
    static void DrawStatus(FirstPersonController player, float s, float h) {
        float level = player.Hidden ? Noise.PlayerLevel * .2f : Noise.PlayerLevel;
        var bar = new Rect(20 * s, h - 92 * s, 150 * s, 8 * s);
        Ui.Fill(bar, new Color(1, 1, 1, .12f));
        var color = Color.Lerp(new Color(.6f, .9f, .7f, .7f), new Color(1, .2f, .15f, .9f), level);
        Ui.Fill(new Rect(bar.x, bar.y, bar.width * level, bar.height), color);
        var small = Ui.Label(Mathf.RoundToInt(14 * s));
        small.normal.textColor = new Color(1, 1, 1, .6f);
        GUI.Label(new Rect(bar.x, bar.y - 20 * s, 200 * s, 20 * s), "RUIDO", small);
        string status = player.Trapped ? "¡ATRAPADO EN UNA TRAMPA!" : player.Hidden ? "ESCONDIDO" : player.BoostLeft > 0 ? "ADRENALINA " + Mathf.CeilToInt(player.BoostLeft) + " s" : null;
        if (status != null) Ui.Shadowed(new Rect(20 * s, h - 126 * s, 400 * s, 30 * s), status, Ui.Label(Mathf.RoundToInt(18 * s), TextAnchor.UpperLeft, FontStyle.Bold));
        else if (!Equipment.Flashlight) Ui.Shadowed(new Rect(20 * s, h - 126 * s, 400 * s, 30 * s), "Sin linterna", Ui.Label(Mathf.RoundToInt(15 * s)));
        DrawTools(player, s, h);
    }

    // Gafas, antidoto y adrenalina: solo lo que el jugador ya tiene.
    static void DrawTools(FirstPersonController player, float s, float h) {
        var tools = player.Tools;
        if (tools == null) return;
        var style = Ui.Label(Mathf.RoundToInt(15 * s));
        float y = h - 70 * s;
        string keys = TouchControls.Enabled ? "" : "G ";
        if (Equipment.Goggles) {
            var bar = new Rect(200 * s, y, 120 * s, 8 * s);
            Ui.Fill(bar, new Color(1, 1, 1, .12f));
            Ui.Fill(new Rect(bar.x, bar.y, bar.width * tools.Battery / tools.batterySeconds, bar.height), tools.Recharging ? new Color(1, .4f, .2f, .8f) : new Color(.4f, 1, .5f, .8f));
            GUI.Label(new Rect(bar.x, bar.y - 20 * s, 220 * s, 20 * s), keys + (tools.Recharging ? "GAFAS recargando" : "GAFAS"), style);
        }
        if (Equipment.Antidote) GUI.Label(new Rect(340 * s, y - 20 * s, 220 * s, 22 * s), (TouchControls.Enabled ? "" : "Q ") + (tools.AntidoteReadyIn > 0 ? "ANTIDOTO " + Mathf.CeilToInt(tools.AntidoteReadyIn) + " s" : "ANTIDOTO listo"), style);
        if (Equipment.AdrenalineDoses > 0) GUI.Label(new Rect(500 * s, y - 20 * s, 220 * s, 22 * s), (TouchControls.Enabled ? "" : "R ") + "ADRENALINA x" + Equipment.AdrenalineDoses, style);
        if (Equipment.Stones > 0) GUI.Label(new Rect(660 * s, y - 20 * s, 240 * s, 22 * s), (TouchControls.Enabled ? "" : "T ") + "PIEDRAS x" + Equipment.Stones, style);
    }

    // Vision de las gafas: tinte verde y, a traves de las paredes, criaturas (rojo), objetos (amarillo) y pasillos bloqueados (naranja).
    static void DrawGoggles(float s, float w, float h) {
        var tools = PlayerTools.Current;
        var cam = Camera.main;
        if (tools == null || !tools.GogglesOn || cam == null) return;
        Ui.Fill(new Rect(0, 0, w, h), new Color(.1f, .6f, .25f, .16f));
        for (float y = 0; y < h; y += 6 * s) Ui.Fill(new Rect(0, y, w, 1), new Color(0, 0, 0, .12f));
        foreach (var t in Object.FindObjectsByType<XRayTarget>(FindObjectsSortMode.None)) {
            if (!t.isActiveAndEnabled) continue;
            var p = cam.WorldToScreenPoint(t.Position);
            if (p.z <= 0 || p.z > 45) continue;
            var color = t.kind == XRayKind.Creature ? new Color(1, .2f, .15f) : t.kind == XRayKind.Item ? new Color(1, .9f, .3f) : new Color(1, .55f, .1f);
            string mark = t.kind == XRayKind.Creature ? "●" : t.kind == XRayKind.Item ? "◆" : "✕";
            var style = Ui.Label(Mathf.RoundToInt((t.kind == XRayKind.Creature ? 34 : 26) * s), TextAnchor.MiddleCenter, FontStyle.Bold);
            style.normal.textColor = color;
            var r = new Rect(p.x - 60 * s, h - p.y - 30 * s, 120 * s, 60 * s);
            GUI.Label(r, mark + "\n" + (string.IsNullOrEmpty(t.label) ? "" : t.label + " ") + Mathf.RoundToInt(p.z) + " m", style);
        }
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
        if (target == null || !Equipment.Gps) return;
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

    void DrawPause(ObjectiveManager objectives, float s, float w, float h) {
        Ui.Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, .8f));
        GUI.Label(new Rect(0, h * .08f, w, 60 * s), "PAUSA", Ui.Label(Mathf.RoundToInt(40 * s), TextAnchor.MiddleCenter, FontStyle.Bold));
        float bw = Mathf.Min(360 * s, w * .4f), x = w * .08f;
        var button = Ui.Button(Mathf.RoundToInt(20 * s));
        if (GUI.Button(new Rect(x, h * .3f, bw, 52 * s), "CONTINUAR", button)) SetPaused(false);
        if (GUI.Button(new Rect(x, h * .3f + 64 * s, bw, 52 * s), "MENU PRINCIPAL", button)) {
            SetPaused(false);
            ChapterManager.Instance.ToMenu();
        }
        float hx = x + bw + 40 * s, hw = w - hx - w * .06f;
        GUI.Label(new Rect(hx, h * .22f, hw, 30 * s), "Ultimas frases", Ui.Label(Mathf.RoundToInt(18 * s), TextAnchor.UpperLeft, FontStyle.Bold));
        var lineStyle = Ui.Label(Mathf.RoundToInt(15 * s));
        float y = h * .22f + 34 * s;
        var history = objectives.History;
        for (int i = Mathf.Max(0, history.Count - 8); i < history.Count; i++) {
            float lh = lineStyle.CalcHeight(new GUIContent(history[i]), hw);
            GUI.Label(new Rect(hx, y, hw, lh), "· " + history[i], lineStyle);
            y += lh + 6 * s;
        }
    }
}
}
