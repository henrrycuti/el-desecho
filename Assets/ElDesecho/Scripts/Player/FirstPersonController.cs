using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Jugador en primera persona: caminar, correr, agacharse, mirar, linterna e interactuar.
// Cada paso hace ruido (El Deshecho es ciego y caza por el oido): agachado casi nada, corriendo mucho,
// en agua profunda mas. Dentro de un escondite el ruido es casi cero.
[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonController : MonoBehaviour {
    public Transform head;
    public Light flashlight;
    public float walkSpeed = 2.4f, runSpeed = 4.2f, crouchSpeed = 1.3f, adrenalineSpeed = 6.6f, gravity = 18;
    public float mouseSensitivity = .08f, touchSensitivity = .14f, interactDistance = 2.8f;
    public float standHeight = 1.75f, crouchHeight = 1.05f;
    [Tooltip("Ruido de los pasos en este escenario (agua a los tobillos = mas).")]
    public float floorNoise = 1;

    public static FirstPersonController Current { get; private set; }
    public Interactable Focus { get; private set; }
    public bool Crouched { get; private set; }
    public bool InputLocked { get; set; }
    public TouchControls Touch { get; private set; }
    public bool Hidden => hiding.Exists(h => h != null && (!h.needsCrouch || Crouched));
    public string ZoneLabel => zones.Count > 0 ? zones[^1].label : null;
    public bool Trapped => Time.time < trappedUntil;
    public float BoostLeft => Mathf.Max(0, boostUntil - Time.time);
    public bool BotCrouch { get; set; }
    // Prueba automatica: salta al llegar cerca de este punto.
    public Vector3? BotJumpAt { get; set; }
    public float jumpSpeed = 6f;
    public PlayerTools Tools { get; private set; }

    CharacterController body;
    float pitch, verticalSpeed, bobPhase, stepTimer, trappedUntil, boostUntil, shake;
    Vector3? botTarget;
    readonly List<NoiseZone> zones = new();
    readonly List<HidingSpot> hiding = new();

    void Awake() {
        Current = this;
        body = GetComponent<CharacterController>();
        Touch = gameObject.AddComponent<TouchControls>();
        Tools = gameObject.AddComponent<PlayerTools>();
        mouseSensitivity *= PlayerPrefs.GetFloat("eldesecho.sensibilidad", 1);
        touchSensitivity *= PlayerPrefs.GetFloat("eldesecho.sensibilidad", 1);
    }

    void Start() {
        if (flashlight != null) flashlight.enabled = flashlight.enabled && Equipment.Flashlight;
    }

    void OnDestroy() { if (Current == this) Current = null; }

    public void PlaceAt(Transform spawn) {
        body.enabled = false;
        transform.SetPositionAndRotation(spawn.position, Quaternion.Euler(0, spawn.eulerAngles.y, 0));
        body.enabled = true;
        pitch = 0;
        verticalSpeed = 0;
    }

    // Usado por la prueba automatica: camina hacia un punto con el mismo cuerpo fisico del jugador.
    public void BotWalkTo(Vector3? target) => botTarget = target;

    public void LookAt(Vector3 point) {
        var d = point - head.position;
        transform.rotation = Quaternion.Euler(0, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0);
        pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        head.localRotation = Quaternion.Euler(pitch, 0, 0);
        UpdateFocus();
    }

    public bool TryInteract() {
        UpdateFocus();
        if (Focus == null) return false;
        Focus.Use();
        return true;
    }

    public void SetFlashlight(bool on) { if (flashlight != null) flashlight.enabled = on && Equipment.Flashlight; }
    public void Trap(float seconds) { trappedUntil = Time.time + seconds; Shake(.6f); }
    public void Boost(float seconds) => boostUntil = Time.time + seconds;
    public void Shake(float amount) => shake = Mathf.Max(shake, amount);
    public void Clatter(float radius) => Shake(.15f);
    public void EnterZone(NoiseZone z) { if (!zones.Contains(z)) zones.Add(z); }
    public void ExitZone(NoiseZone z) => zones.Remove(z);
    public void EnterHiding(HidingSpot h) { if (!hiding.Contains(h)) hiding.Add(h); }
    public void ExitHiding(HidingSpot h) => hiding.Remove(h);

    void Update() {
        var objectives = ObjectiveManager.Instance;
        bool paused = Time.timeScale == 0;
        bool dialogue = objectives != null && objectives.InDialogue;
        bool locked = InputLocked || paused || dialogue || Trapped || (ChapterManager.Instance != null && ChapterManager.Instance.Busy);
        // En la prueba automatica no se lee el teclado, el raton ni la pantalla tactil de la persona que mira.
        var kb = ChapterManager.TestMode ? null : Keyboard.current;
        var mouse = ChapterManager.TestMode ? null : Mouse.current;
        Vector2 move = Vector2.zero, look = Vector2.zero;
        bool run = false, crouch = locked && Crouched, interact = false, light = false, jump = false;
        bool advance = kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                       || mouse != null && mouse.leftButton.wasPressedThisFrame || !ChapterManager.TestMode && Touch.InteractPressed;
        if (!locked) {
            if (kb != null) {
                move = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
                run = kb.leftShiftKey.isPressed;
                crouch = kb.cKey.isPressed || kb.leftCtrlKey.isPressed;
                interact = kb.eKey.wasPressedThisFrame;
                light = kb.fKey.wasPressedThisFrame;
                jump = kb.spaceKey.wasPressedThisFrame;
            }
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked) {
                look = mouse.delta.ReadValue() * mouseSensitivity;
                interact |= mouse.leftButton.wasPressedThisFrame;
            }
            if (TouchControls.Enabled && !ChapterManager.TestMode) {
                move += Touch.Move;
                look += Touch.Look * touchSensitivity;
                run |= Touch.RunHeld;
                crouch |= Touch.CrouchHeld;
                interact |= Touch.InteractPressed;
                light |= Touch.LightPressed;
                jump |= Touch.JumpPressed;
            }
        } else if (Trapped) {
            crouch = Crouched;
        }
        if (dialogue && advance && !paused) objectives.Advance();
        if (botTarget.HasValue && !Trapped) {
            var to = botTarget.Value - transform.position;
            to.y = 0;
            if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540 * Time.deltaTime);
            move = Vector2.up;
            run = !Crouched;
            if (BotJumpAt.HasValue) {
                var j = BotJumpAt.Value - transform.position;
                j.y = 0;
                if (j.magnitude < 1.3f && body.isGrounded) { jump = true; BotJumpAt = null; }
            }
        }
        if (BotCrouch) crouch = true;

        transform.Rotate(0, look.x, 0);
        pitch = Mathf.Clamp(pitch - look.y, -80, 80);
        SetCrouch(crouch);

        float zoneSpeed = 1, zoneNoise = floorNoise;
        foreach (var z in zones) if (z != null) { zoneSpeed = Mathf.Min(zoneSpeed, z.speedMultiplier); zoneNoise = Mathf.Max(zoneNoise, z.noiseMultiplier); }
        bool boosted = Time.time < boostUntil;
        bool running = run && !Crouched;
        float speed = (Crouched ? crouchSpeed : running ? (boosted ? adrenalineSpeed : runSpeed) : boosted ? runSpeed : walkSpeed) * zoneSpeed;
        var direction = Vector3.ClampMagnitude(transform.TransformDirection(new Vector3(move.x, 0, move.y)), 1);
        if (jump && body.isGrounded && !Crouched) {
            verticalSpeed = jumpSpeed;
            Noise.Emit(transform.position, 8 * zoneNoise);
        } else verticalSpeed = body.isGrounded && verticalSpeed <= 0 ? -2 : verticalSpeed - gravity * Time.deltaTime;
        body.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        // Pasos: cada paso emite ruido segun como se mueve el jugador.
        float moving = body.isGrounded ? direction.magnitude * speed : 0;
        if (moving > .3f) {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0) {
                stepTimer = Crouched ? .7f : running ? .32f : .5f;
                float radius = (Crouched ? 2 : running ? 14 : 6) * zoneNoise;
                if (Hidden) radius *= .1f;
                Noise.Emit(transform.position, radius);
            }
        }
        Noise.Fade(Time.deltaTime);

        bobPhase += moving * Time.deltaTime * 2.2f;
        shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 1.5f);
        float eye = Crouched ? crouchHeight - .12f : standHeight - .12f;
        var jitter = shake > 0 ? Random.insideUnitSphere * shake * .05f : Vector3.zero;
        head.localPosition = new Vector3(0, eye + Mathf.Sin(bobPhase * 2) * .025f * Mathf.Clamp01(moving), 0) + jitter;
        head.localRotation = Quaternion.Euler(pitch + jitter.y * 40, jitter.x * 40, Mathf.Sin(bobPhase) * .4f * Mathf.Clamp01(moving));

        UpdateFocus();
        if (light && flashlight != null && Equipment.Flashlight) flashlight.enabled = !flashlight.enabled;
        if (interact && !dialogue) {
            if (Focus != null) Focus.Use();
            else if (objectives != null && objectives.Subtitle != null) objectives.Advance();
        }
    }

    void SetCrouch(bool value) {
        if (value == Crouched) return;
        if (!value && Physics.SphereCast(transform.position + Vector3.up * crouchHeight * .5f, body.radius * .9f, Vector3.up, out _, standHeight - crouchHeight * .5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        Crouched = value;
        body.height = value ? crouchHeight : standHeight;
        body.center = Vector3.up * body.height / 2;
    }

    void UpdateFocus() {
        // El primer objeto disponible en la linea de vista. Un objeto que aun no se puede usar no tapa a otro,
        // pero una pared si.
        Focus = null;
        var hits = Physics.RaycastAll(head.position, head.forward, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits) {
            if (hit.collider.GetComponentInParent<FirstPersonController>() != null) continue;
            if (hit.collider.GetComponentInParent<CreatureBrain>() != null) continue;
            var item = hit.collider.GetComponentInParent<Interactable>();
            if (item != null && item.Available) { Focus = item; return; }
            if (item == null && !hit.collider.isTrigger) return;
        }
    }
}
}
