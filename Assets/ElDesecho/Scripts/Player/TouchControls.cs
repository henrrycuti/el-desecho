using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Controles tactiles: joystick a la izquierda, mirar arrastrando a la derecha y botones de accion.
// Se activan solos en telefono; en PC se pueden ver con el argumento -tactil.
public sealed class TouchControls : MonoBehaviour {
    public static bool Enabled => Application.isMobilePlatform || Array.IndexOf(Environment.GetCommandLineArgs(), "-tactil") >= 0;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool LightPressed { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool RunHeld { get; private set; }
    public bool PausePressed { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool GogglesPressed { get; private set; }
    public bool AntidotePressed { get; private set; }
    public bool DosePressed { get; private set; }
    public bool StonePressed { get; private set; }

    const float StickRadius = 70;
    int moveId = -1, lookId = -1;
    Vector2 moveOrigin, movePoint;

    enum Action { Interact, Light, Crouch, Run, Pause, Jump, Goggles, Antidote, Dose, Stone }
    static readonly Action[] Actions = { Action.Interact, Action.Light, Action.Crouch, Action.Run, Action.Pause, Action.Jump, Action.Goggles, Action.Antidote, Action.Dose, Action.Stone };

    // Los botones de herramientas solo aparecen cuando el jugador ya las tiene.
    static bool Visible(Action a) => a switch {
        Action.Goggles => Equipment.Goggles,
        Action.Antidote => Equipment.Antidote,
        Action.Dose => Equipment.AdrenalineDoses > 0,
        Action.Stone => Equipment.Stones > 0,
        _ => true,
    };

    // Rectangulos en coordenadas de pantalla con origen arriba a la izquierda.
    static Rect ButtonRect(Action action) {
        float s = Ui.Scale, size = 92 * s, gap = 14 * s, w = Screen.width, h = Screen.height;
        return action switch {
            Action.Interact => new Rect(w - size - gap, h - size * 2 - gap * 2, size, size),
            Action.Light => new Rect(w - size * 2 - gap * 2, h - size - gap, size, size),
            Action.Crouch => new Rect(w - size - gap, h - size - gap, size, size),
            Action.Run => new Rect(w - size * 3 - gap * 3, h - size - gap, size, size),
            Action.Jump => new Rect(w - size * 2 - gap * 2, h - size * 2 - gap * 2, size, size),
            Action.Goggles => new Rect(w - size - gap, h - size * 3 - gap * 3, size, size * .8f),
            Action.Antidote => new Rect(w - size * 2 - gap * 2, h - size * 3 - gap * 3, size, size * .8f),
            Action.Dose => new Rect(w - size * 3 - gap * 3, h - size * 2 - gap * 2, size, size),
            Action.Stone => new Rect(w - size * 3 - gap * 3, h - size * 3 - gap * 3, size, size * .8f),
            _ => new Rect(w - size * .8f - gap, gap, size * .8f, size * .6f),
        };
    }

    string Label(Action action) => action switch {
        Action.Interact => "USAR",
        Action.Light => "LUZ",
        Action.Crouch => CrouchHeld ? "DE PIE" : "AGACHAR",
        Action.Run => RunHeld ? "CAMINAR" : "CORRER",
        Action.Jump => "SALTAR",
        Action.Goggles => "GAFAS",
        Action.Antidote => "ANTIDOTO",
        Action.Dose => "ADRENALINA",
        Action.Stone => "PIEDRA x" + Equipment.Stones,
        _ => "II",
    };

    void Update() {
        InteractPressed = LightPressed = PausePressed = JumpPressed = GogglesPressed = AntidotePressed = DosePressed = StonePressed = false;
        Look = Vector2.zero;
        var screen = Touchscreen.current;
        if (!Enabled || screen == null) { Move = Vector2.zero; return; }
        bool moveSeen = false, lookSeen = false;
        foreach (var touch in screen.touches) {
            var phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
            int id = touch.touchId.ReadValue();
            Vector2 p = touch.position.ReadValue();
            var gui = new Vector2(p.x, Screen.height - p.y);
            bool ended = phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
            if (phase == UnityEngine.InputSystem.TouchPhase.Began) {
                var hit = HitButton(gui);
                if (hit.HasValue) { Press(hit.Value); continue; }
                if (p.x < Screen.width * .45f && moveId < 0) { moveId = id; moveOrigin = movePoint = p; }
                else if (lookId < 0) lookId = id;
            }
            if (id == moveId) {
                moveSeen = !ended;
                movePoint = p;
            } else if (id == lookId) {
                lookSeen = !ended;
                if (!ended) Look += touch.delta.ReadValue();
            }
        }
        if (!moveSeen) moveId = -1;
        if (!lookSeen) lookId = -1;
        float radius = StickRadius * Ui.Scale;
        Move = moveId >= 0 ? Vector2.ClampMagnitude((movePoint - moveOrigin) / radius, 1) : Vector2.zero;
    }

    static Action? HitButton(Vector2 gui) {
        foreach (var a in Actions) if (Visible(a) && ButtonRect(a).Contains(gui)) return a;
        return null;
    }

    void Press(Action action) {
        switch (action) {
            case Action.Interact: InteractPressed = true; break;
            case Action.Light: LightPressed = true; break;
            case Action.Crouch: CrouchHeld = !CrouchHeld; break;
            case Action.Run: RunHeld = !RunHeld; break;
            case Action.Jump: JumpPressed = true; break;
            case Action.Goggles: GogglesPressed = true; break;
            case Action.Antidote: AntidotePressed = true; break;
            case Action.Dose: DosePressed = true; break;
            case Action.Stone: StonePressed = true; break;
            default: PausePressed = true; break;
        }
    }

    public void Draw() {
        if (!Enabled) return;
        var style = Ui.Label(Mathf.RoundToInt(18 * Ui.Scale), TextAnchor.MiddleCenter, FontStyle.Bold);
        foreach (var a in Actions) {
            if (!Visible(a)) continue;
            Ui.Fill(ButtonRect(a), new Color(0, 0, 0, .35f));
            GUI.Label(ButtonRect(a), Label(a), style);
        }
        if (moveId >= 0) {
            float radius = StickRadius * Ui.Scale;
            var center = new Vector2(moveOrigin.x, Screen.height - moveOrigin.y);
            Ui.Fill(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), new Color(1, 1, 1, .08f));
            var knob = center + new Vector2(Move.x, -Move.y) * radius;
            Ui.Fill(new Rect(knob.x - radius * .35f, knob.y - radius * .35f, radius * .7f, radius * .7f), new Color(1, 1, 1, .3f));
        }
    }
}
}
