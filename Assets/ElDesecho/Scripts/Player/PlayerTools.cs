using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Herramientas del periodista:
// - Gafas de exploracion (G): ven criaturas, objetos y pasillos bloqueados a traves de las paredes.
//   Bateria de pocos segundos; si se vacia del todo tarda mucho mas en recargarse.
// - Antidoto (Q): detiene a la criatura mas cercana delante del jugador; el frasco tarda en volver a estar listo.
// - Adrenalina (R): cada dosis da unos segundos de carrera muy rapida.
public sealed class PlayerTools : MonoBehaviour {
    public float batterySeconds = 6, rechargePerSecond = .6f, emptyLockout = 9;
    public float antidoteRange = 8, antidoteStun = 6, antidoteCooldown = 18, doseSeconds = 7;

    public static PlayerTools Current { get; private set; }
    public bool GogglesOn { get; private set; }
    public float Battery { get; private set; }
    public bool Recharging => Time.time < lockedUntil;
    public float AntidoteReadyIn => Mathf.Max(0, antidoteReady - Time.time);

    float lockedUntil, antidoteReady;
    FirstPersonController player;

    void Awake() {
        Current = this;
        player = GetComponent<FirstPersonController>();
        Battery = batterySeconds;
    }

    void OnDestroy() { if (Current == this) Current = null; }

    void Update() {
        var kb = ChapterManager.TestMode ? null : Keyboard.current;
        var touch = player.Touch;
        bool human = !ChapterManager.TestMode && touch != null;
        bool blocked = player.InputLocked || Time.timeScale == 0;
        if (!blocked) {
            if (kb != null && kb.gKey.wasPressedThisFrame || human && touch.GogglesPressed) ToggleGoggles();
            if (kb != null && kb.qKey.wasPressedThisFrame || human && touch.AntidotePressed) UseAntidote();
            if (kb != null && kb.rKey.wasPressedThisFrame || human && touch.DosePressed) UseDose();
        }
        if (GogglesOn) {
            Battery -= Time.deltaTime;
            if (Battery <= 0) {
                Battery = 0;
                GogglesOn = false;
                lockedUntil = Time.time + emptyLockout;
                ObjectiveManager.Instance?.Say("La bateria de las gafas se agoto. Tardara en recargarse.");
            }
        } else if (!Recharging) {
            Battery = Mathf.Min(batterySeconds, Battery + rechargePerSecond * Time.deltaTime);
        }
    }

    public bool ToggleGoggles() {
        if (!Equipment.Goggles) return false;
        if (GogglesOn) { GogglesOn = false; return true; }
        if (Recharging || Battery < .5f) return false;
        GogglesOn = true;
        StoryEvents.Raise("gafas-usadas");
        return true;
    }

    public bool UseAntidote() {
        if (!Equipment.Antidote || AntidoteReadyIn > 0) return false;
        CreatureBrain best = null;
        float bestDistance = antidoteRange;
        foreach (var c in FindObjectsByType<CreatureBrain>(FindObjectsSortMode.None)) {
            if (c.State == CreatureState.Dormant) continue;
            var to = c.transform.position - transform.position;
            to.y = 0;
            if (to.magnitude > bestDistance) continue;
            if (to.magnitude > 2 && Vector3.Dot(to.normalized, transform.forward) < .3f) continue;
            best = c;
            bestDistance = to.magnitude;
        }
        if (best == null) {
            ObjectiveManager.Instance?.Say("No hay ninguna criatura lo bastante cerca para el antidoto.");
            return false;
        }
        best.Stun(antidoteStun);
        antidoteReady = Time.time + antidoteCooldown;
        Hud.Flash();
        ObjectiveManager.Instance?.Say("El antidoto la detuvo. No durara mucho.");
        StoryEvents.Raise("antidoto-usado");
        return true;
    }

    public bool UseDose() {
        if (Equipment.AdrenalineDoses <= 0) return false;
        Equipment.AdrenalineDoses--;
        player.Boost(doseSeconds);
        ObjectiveManager.Instance?.Say("Adrenalina. Quedan " + Equipment.AdrenalineDoses + " dosis.");
        return true;
    }
}
}
