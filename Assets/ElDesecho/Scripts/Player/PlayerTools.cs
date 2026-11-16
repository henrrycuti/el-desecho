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
            // Piedra: en PC se mantiene T para ver el arco y se suelta para lanzar; en el movil, boton PIEDRA.
            bool aiming = kb != null && kb.tKey.isPressed && Equipment.Stones > 0;
            ShowArc(aiming);
            if (kb != null && kb.tKey.wasReleasedThisFrame || human && touch.StonePressed) ThrowStone();
        } else ShowArc(false);
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

    // ---------- Piedras ----------
    const float ThrowSpeed = 11, ThrowLift = 3.5f;
    LineRenderer arc;

    Vector3 ThrowOrigin => player.head.position + player.head.forward * .45f - player.head.up * .1f;
    Vector3 ThrowVelocity => player.head.forward * ThrowSpeed + Vector3.up * ThrowLift;

    public bool ThrowStone() {
        if (Equipment.Stones <= 0) return false;
        Equipment.Stones--;
        var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stone.name = "Piedra lanzada";
        stone.transform.position = ThrowOrigin;
        stone.transform.localScale = new Vector3(.11f, .09f, .1f);
        var r = stone.GetComponent<Renderer>();
        r.material.color = new Color(.35f, .34f, .32f);
        var body = stone.AddComponent<Rigidbody>();
        body.mass = .4f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = ThrowVelocity;
        body.angularVelocity = Random.insideUnitSphere * 10;
        Physics.IgnoreCollision(stone.GetComponent<Collider>(), GetComponent<Collider>());
        stone.AddComponent<ThrownStone>();
        return true;
    }

    // Arco de lanzamiento: puntos de la trayectoria hasta que choca con algo.
    void ShowArc(bool on) {
        if (!on) { if (arc != null) arc.enabled = false; return; }
        if (arc == null) {
            arc = new GameObject("Arco de la piedra").AddComponent<LineRenderer>();
            arc.material = new Material(Shader.Find("Sprites/Default"));
            arc.startColor = new Color(1, 1, 1, .5f);
            arc.endColor = new Color(1, .8f, .3f, .9f);
            arc.startWidth = .02f;
            arc.endWidth = .06f;
            arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        arc.enabled = true;
        var points = new System.Collections.Generic.List<Vector3>();
        Vector3 p = ThrowOrigin, v = ThrowVelocity;
        points.Add(p);
        for (int i = 0; i < 40; i++) {
            var next = p + v * .05f;
            v += Physics.gravity * .05f;
            if (Physics.Linecast(p, next, out var hit, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<FirstPersonController>() == null) {
                points.Add(hit.point);
                break;
            }
            points.Add(next);
            p = next;
        }
        arc.positionCount = points.Count;
        arc.SetPositions(points.ToArray());
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
