using System.Collections.Generic;
using UnityEngine;

namespace ElDesecho {
public enum CreatureState { Dormant, Patrol, Investigate, Chase, Stunned, Scripted }

// El Deshecho: ciego, caza por el oido. Patrulla respirando y arrastrandose (el jugador lo oye venir),
// va a investigar el ultimo ruido, persigue si el ruido es fuerte y cercano y se rinde tras un rato de silencio.
// Se mueve por las celdas del escenario (CellGrid) y no cabe en conductos ni escondites.
[RequireComponent(typeof(CharacterController))]
public sealed class CreatureBrain : MonoBehaviour {
    public float patrolSpeed = 1.3f, investigateSpeed = 2.2f, chaseSpeed = 3.7f;
    public float chaseHearing = 9, giveUpSeconds = 5, catchDistance = 1.25f, listenSeconds = 3.5f;
    [Tooltip("Celdas donde patrulla (vacio = todo el mapa).")]
    public Vector2Int[] patrolArea = new Vector2Int[0];
    public bool startDormant;
    public AudioClip breathClip, stepClip, alertClip, attackClip;

    public CreatureState State { get; private set; } = CreatureState.Patrol;
    public float LastHeardAt { get; private set; } = -100;
    public bool Harmless { get; set; }

    CharacterController body;
    Animator animator;
    AudioSource voice, steps;
    readonly List<Vector2Int> path = new();
    Vector3 target;
    float listenUntil, stunUntil, stepTimer, graceUntil;
    CellGrid grid;

    void Awake() {
        body = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        voice = gameObject.AddComponent<AudioSource>();
        steps = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { voice, steps }) {
            s.spatialBlend = 1;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2;
            s.maxDistance = 26;
            s.playOnAwake = false;
        }
        voice.loop = true;
        voice.clip = breathClip;
    }

    void OnEnable() => Noise.Heard += OnNoise;
    void OnDisable() => Noise.Heard -= OnNoise;

    void Start() {
        grid = CellGrid.Instance;
        Harmless |= ChapterManager.TestMode;
        var player = FirstPersonController.Current;
        if (player != null) Physics.IgnoreCollision(body, player.GetComponent<CharacterController>());
        if (startDormant) SetDormant(true);
        else { if (voice.clip != null) voice.Play(); PickPatrol(); }
    }

    public void SetDormant(bool dormant) {
        State = dormant ? CreatureState.Dormant : CreatureState.Patrol;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = !dormant;
        body.enabled = !dormant;
        if (dormant) voice.Stop();
        else { if (voice.clip != null && !voice.isPlaying) voice.Play(); PickPatrol(); }
    }

    // Aparece en un punto y persigue un lugar concreto (para las escenas preparadas del guion).
    public void Ambush(Vector3 from, Vector3 toward) {
        if (State == CreatureState.Dormant) SetDormant(false);
        Teleport(from);
        graceUntil = Time.time + 1;
        StartChase(toward);
    }

    public void Teleport(Vector3 position) {
        body.enabled = false;
        transform.position = position;
        body.enabled = true;
        path.Clear();
    }

    public void Stun(float seconds) {
        State = CreatureState.Stunned;
        stunUntil = Time.time + seconds;
        path.Clear();
        if (animator != null) animator.SetBool("Stunned", true);
    }

    // Control directo desde una secuencia: camina hacia un punto, sin escuchar.
    public void Script(Vector3 destination) {
        if (State == CreatureState.Dormant) SetDormant(false);
        State = CreatureState.Scripted;
        target = destination;
        path.Clear();
    }

    public void Release(CreatureState next = CreatureState.Patrol) {
        State = next;
        graceUntil = Time.time + 4;
        if (next == CreatureState.Patrol) PickPatrol();
    }

    public void PlayAttack() {
        if (animator != null) animator.SetTrigger("Attack");
        if (attackClip != null) voice.PlayOneShot(attackClip, 1);
    }

    void OnNoise(Vector3 position, float radius) {
        if (State is CreatureState.Dormant or CreatureState.Stunned or CreatureState.Scripted) return;
        float distance = Vector3.Distance(Flat(position), Flat(transform.position));
        if (distance > radius) return;
        LastHeardAt = Time.time;
        if (distance < chaseHearing && radius >= 5 || State == CreatureState.Chase) StartChase(position);
        else Investigate(position);
    }

    void StartChase(Vector3 position) {
        if (State != CreatureState.Chase) {
            if (animator != null) animator.SetTrigger("Alert");
            if (alertClip != null) voice.PlayOneShot(alertClip, .9f);
        }
        State = CreatureState.Chase;
        LastHeardAt = Time.time;
        RouteTo(position);
    }

    void Investigate(Vector3 position) {
        State = CreatureState.Investigate;
        listenUntil = 0;
        RouteTo(position);
    }

    void PickPatrol() {
        if (grid == null) return;
        var cells = patrolArea.Length > 0 ? new List<Vector2Int>(patrolArea) : grid.OpenCells(true);
        if (cells.Count == 0) return;
        RouteTo(grid.Center(cells[Random.Range(0, cells.Count)]));
    }

    void RouteTo(Vector3 position) {
        target = position;
        path.Clear();
        if (grid == null) return;
        var to = grid.ToCell(position);
        if (!grid.Walkable(to, true)) return;
        path.AddRange(grid.Path(grid.ToCell(transform.position), to, true));
    }

    void Update() {
        if (State == CreatureState.Dormant) return;
        float speed = 0;
        switch (State) {
            case CreatureState.Stunned:
                if (Time.time >= stunUntil) {
                    if (animator != null) animator.SetBool("Stunned", false);
                    Release(CreatureState.Patrol);
                }
                break;
            case CreatureState.Scripted:
                speed = chaseSpeed;
                MoveToward(target, speed);
                break;
            case CreatureState.Chase:
                speed = chaseSpeed;
                if (Time.time - LastHeardAt > giveUpSeconds) Investigate(target);
                else if (!FollowPath(speed) && Flat(transform.position - target).magnitude > .6f) MoveToward(target, speed);
                break;
            case CreatureState.Investigate:
                if (FollowPath(investigateSpeed)) speed = investigateSpeed;
                else {
                    if (listenUntil == 0) listenUntil = Time.time + listenSeconds;
                    if (Time.time >= listenUntil) { State = CreatureState.Patrol; PickPatrol(); }
                }
                break;
            default:
                if (FollowPath(patrolSpeed)) speed = patrolSpeed;
                else PickPatrol();
                break;
        }
        if (animator != null) {
            animator.SetFloat("Speed", speed <= .05f ? 0 : speed < 2 ? 1 : 2, .15f, Time.deltaTime);
            animator.SetBool("Investigate", State == CreatureState.Investigate && speed <= .05f);
        }
        if (speed > .05f) {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0 && stepClip != null) { steps.PlayOneShot(stepClip, speed > 3 ? .8f : .45f); stepTimer = speed > 3 ? .33f : .6f; }
        }
        CheckCatch();
    }

    bool FollowPath(float speed) {
        while (path.Count > 0 && Flat(grid.Center(path[0]) - transform.position).magnitude < .4f) path.RemoveAt(0);
        if (path.Count == 0) return false;
        MoveToward(grid.Center(path[0]), speed);
        return true;
    }

    void MoveToward(Vector3 point, float speed) {
        var to = Flat(point - transform.position);
        if (to.magnitude < .05f) return;
        var dir = to.normalized;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 300 * Time.deltaTime);
        body.Move(dir * Mathf.Min(speed * Time.deltaTime, to.magnitude) + Vector3.down * 2 * Time.deltaTime);
    }

    void CheckCatch() {
        var player = FirstPersonController.Current;
        if (player == null || Harmless || player.Hidden || Time.time < graceUntil) return;
        if (State is CreatureState.Stunned or CreatureState.Scripted or CreatureState.Dormant) return;
        if (Flat(player.transform.position - transform.position).magnitude > catchDistance) return;
        // En un conducto o escondite donde la criatura no cabe, no puede alcanzarlo.
        if (grid != null && !grid.Walkable(grid.ToCell(player.transform.position), true)) return;
        var manager = ChapterManager.Instance;
        if (manager == null || manager.Busy) return;
        PlayAttack();
        player.InputLocked = true;
        player.Shake(1);
        manager.Caught();
    }

    static Vector3 Flat(Vector3 v) => new(v.x, 0, v.z);
}
}
