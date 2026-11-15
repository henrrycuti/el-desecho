using UnityEngine;
using UnityEngine.InputSystem;

namespace ElDesecho {
// Jugador en primera persona: caminar, correr, agacharse, mirar, linterna e interactuar.
// Teclado y raton en PC; controles tactiles en telefono.
[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonController : MonoBehaviour {
    public Transform head;
    public Light flashlight;
    public float walkSpeed = 2.4f, runSpeed = 4.2f, crouchSpeed = 1.3f, gravity = 18;
    public float mouseSensitivity = .08f, touchSensitivity = .14f, interactDistance = 2.8f;
    public float standHeight = 1.75f, crouchHeight = 1.05f;

    public static FirstPersonController Current { get; private set; }
    public Interactable Focus { get; private set; }
    public bool Crouched { get; private set; }
    public bool InputLocked { get; set; }
    public TouchControls Touch { get; private set; }

    CharacterController body;
    float pitch, verticalSpeed, bobPhase;
    Vector3? botTarget;

    void Awake() {
        Current = this;
        body = GetComponent<CharacterController>();
        Touch = gameObject.AddComponent<TouchControls>();
        mouseSensitivity *= PlayerPrefs.GetFloat("eldesecho.sensibilidad", 1);
        touchSensitivity *= PlayerPrefs.GetFloat("eldesecho.sensibilidad", 1);
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

    void Update() {
        bool paused = Time.timeScale == 0;
        bool locked = InputLocked || paused || (ChapterManager.Instance != null && ChapterManager.Instance.Busy);
        var kb = Keyboard.current;
        Vector2 move = Vector2.zero, look = Vector2.zero;
        bool run = false, crouch = Crouched, interact = false, light = false;
        if (!locked) {
            if (kb != null) {
                move = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
                run = kb.leftShiftKey.isPressed;
                crouch = kb.cKey.isPressed || kb.leftCtrlKey.isPressed;
                interact = kb.eKey.wasPressedThisFrame;
                light = kb.fKey.wasPressedThisFrame;
            }
            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked) {
                look = Mouse.current.delta.ReadValue() * mouseSensitivity;
                interact |= Mouse.current.leftButton.wasPressedThisFrame;
            }
            if (TouchControls.Enabled) {
                move += Touch.Move;
                look += Touch.Look * touchSensitivity;
                run |= Touch.RunHeld;
                crouch = Touch.CrouchHeld || crouch && kb != null && (kb.cKey.isPressed || kb.leftCtrlKey.isPressed);
                interact |= Touch.InteractPressed;
                light |= Touch.LightPressed;
            }
        }
        if (botTarget.HasValue) {
            var to = botTarget.Value - transform.position;
            to.y = 0;
            if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540 * Time.deltaTime);
            move = Vector2.up;
            run = false;
        }

        transform.Rotate(0, look.x, 0);
        pitch = Mathf.Clamp(pitch - look.y, -80, 80);
        SetCrouch(crouch);

        float speed = Crouched ? crouchSpeed : run ? runSpeed : walkSpeed;
        var direction = transform.TransformDirection(new Vector3(move.x, 0, move.y));
        direction = Vector3.ClampMagnitude(direction, 1);
        verticalSpeed = body.isGrounded ? -2 : verticalSpeed - gravity * Time.deltaTime;
        body.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        float moving = body.isGrounded ? direction.magnitude * speed : 0;
        bobPhase += moving * Time.deltaTime * 2.2f;
        float eye = Crouched ? crouchHeight - .12f : standHeight - .12f;
        head.localPosition = new Vector3(0, eye + Mathf.Sin(bobPhase * 2) * .025f * Mathf.Clamp01(moving), 0);
        head.localRotation = Quaternion.Euler(pitch, 0, Mathf.Sin(bobPhase) * .4f * Mathf.Clamp01(moving));

        UpdateFocus();
        if (light && flashlight != null) flashlight.enabled = !flashlight.enabled;
        if (interact && Focus != null) Focus.Use();
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
            var item = hit.collider.GetComponentInParent<Interactable>();
            if (item != null && item.Available) { Focus = item; return; }
            if (item == null && !hit.collider.isTrigger) return;
        }
    }
}
}
