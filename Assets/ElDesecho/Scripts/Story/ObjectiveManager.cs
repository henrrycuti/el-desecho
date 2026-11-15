using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElDesecho {
// Un paso del guion dentro de una escena.
[Serializable]
public sealed class ObjectiveStep {
    [Tooltip("Texto del objetivo que se ve arriba en pantalla.")]
    [TextArea] public string objective;
    [Tooltip("Evento que completa el paso. Vacio = se completa al terminar sus frases.")]
    public string completeOn;
    [Tooltip("Cuantas veces debe ocurrir el evento (por ejemplo, 4 fotos).")]
    public int count = 1;
    [Tooltip("Frases que aparecen como subtitulos al empezar el paso.")]
    [TextArea] public string[] lines = new string[0];
    [Tooltip("Dialogo importante: el juego espera a que el jugador pulse continuar en cada frase.")]
    public bool dialogue;
    [Tooltip("Evento que se emite al empezar el paso (por ejemplo, para lanzar una secuencia).")]
    public string raiseOnStart;
    [Tooltip("Lugar que marca el indicador en pantalla. Vacio = sin marcador.")]
    public Transform marker;
    [Tooltip("Ruta del GPS: el indicador apunta al siguiente punto hasta llegar al ultimo.")]
    public Transform[] gpsRoute = new Transform[0];
    [Tooltip("Calidad de la senal del GPS (1 = perfecta, 0 = sin senal).")]
    [Range(0, 1)] public float gpsSignal = 1;
    [Tooltip("Al completarse: escena a cargar (vacio = seguir en esta).")]
    public string loadScene;
    public string loadSpawn;
    public string loadTitle;
    [Tooltip("Al completarse termina el capitulo con este titulo de cierre.")]
    public string completeChapterTitle;
    [Tooltip("Guardar punto de control al empezar este paso.")]
    public bool checkpoint;
    public string checkpointSpawn;
}

// Objetivos de la escena: muestra el objetivo actual, los subtitulos, los dialogos y el GPS,
// y avanza con los eventos de historia. Tambien coloca al jugador en su punto de aparicion.
public sealed class ObjectiveManager : MonoBehaviour {
    public List<ObjectiveStep> steps = new();
    public float gpsArrival = 3.5f;

    public static ObjectiveManager Instance { get; private set; }
    public int Index { get; private set; }
    public int Progress { get; private set; }
    public ObjectiveStep Current => Index < steps.Count ? steps[Index] : null;
    public int GpsIndex { get; private set; }
    public Transform GpsTarget => Current != null && GpsIndex < Current.gpsRoute.Length ? Current.gpsRoute[GpsIndex] : null;
    public string Subtitle => queue.Count > 0 ? queue[0].text : null;
    public bool InDialogue => queue.Count > 0 && queue[0].blocking;
    public bool DialoguePending => queue.Exists(l => l.blocking);
    public float ObjectiveShownAt { get; private set; }
    public IReadOnlyList<string> History => history;

    struct Line { public string text; public bool blocking; }
    readonly List<Line> queue = new();
    readonly List<string> history = new();
    float lineUntil;
    bool waitingForLines;

    void Awake() => Instance = this;
    void OnEnable() => StoryEvents.Raised += OnEvent;
    void OnDisable() => StoryEvents.Raised -= OnEvent;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start() {
        var manager = ChapterManager.Instance;
        string spawn = manager != null ? manager.PendingSpawn : null;
        var player = FindFirstObjectByType<FirstPersonController>();
        SpawnPoint chosen = null;
        foreach (var p in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.InstanceID))
            if (chosen == null || p.id == spawn) chosen = p;
        if (player != null && chosen != null) player.PlaceAt(chosen.transform);
        int first = manager != null ? Mathf.Clamp(manager.PendingStep, 0, Mathf.Max(0, steps.Count - 1)) : 0;
        StoryEvents.Raise("reanudar:" + first);
        Begin(first);
    }

    public bool Waiting(string id) => Current != null && !string.IsNullOrEmpty(id) && Current.completeOn == id && !InDialogue;

    // Anade una frase a la cola de subtitulos. "blocking" = dialogo que espera a que el jugador continue.
    public void Say(string text, bool blocking = false) {
        if (string.IsNullOrEmpty(text)) return;
        if (queue.Count == 0) lineUntil = Time.time + LineTime(text);
        queue.Add(new Line { text = text, blocking = blocking });
        history.Add(text);
        if (history.Count > 14) history.RemoveAt(0);
    }

    // Continuar: pasa a la siguiente frase (E, clic o boton USAR).
    public void Advance() {
        if (queue.Count == 0) return;
        queue.RemoveAt(0);
        if (queue.Count > 0) lineUntil = Time.time + LineTime(queue[0].text);
    }

    static float LineTime(string text) => Mathf.Max(3.5f, text.Length * .07f);

    void Begin(int index) {
        Index = index;
        Progress = 0;
        GpsIndex = 0;
        var step = Current;
        if (step == null) return;
        ObjectiveShownAt = Time.time;
        // Un dialogo importante empieza ya: las frases sueltas que quedaban pendientes se descartan.
        if (step.dialogue) queue.RemoveAll(l => !l.blocking);
        foreach (var line in step.lines) Say(line, step.dialogue);
        if (step.dialogue && queue.Count > 0) lineUntil = Time.time + LineTime(queue[0].text);
        waitingForLines = string.IsNullOrEmpty(step.completeOn);
        if (step.checkpoint && ChapterManager.Instance != null)
            ChapterManager.Instance.Checkpoint(SceneManager.GetActiveScene().name, step.checkpointSpawn, index);
        if (!string.IsNullOrEmpty(step.raiseOnStart)) StoryEvents.Raise(step.raiseOnStart);
    }

    void OnEvent(string id) {
        if (Current == null || string.IsNullOrEmpty(id) || Current.completeOn != id) return;
        Progress++;
        if (Progress < Mathf.Max(1, Current.count)) return;
        Complete();
    }

    void Complete() {
        var done = Current;
        var manager = ChapterManager.Instance;
        if (!string.IsNullOrEmpty(done.completeChapterTitle) && manager != null) {
            Index = steps.Count;
            manager.CompleteChapter(done.completeChapterTitle);
            return;
        }
        if (!string.IsNullOrEmpty(done.loadScene) && manager != null) {
            Index = steps.Count;
            manager.Transition(done.loadScene, done.loadSpawn, done.loadTitle);
            return;
        }
        Begin(Index + 1);
    }

    void Update() {
        if (queue.Count > 0 && !queue[0].blocking && Time.time >= lineUntil) Advance();
        if (waitingForLines && queue.Count == 0) {
            waitingForLines = false;
            Complete();
        }
        var player = FirstPersonController.Current;
        var target = GpsTarget;
        if (player != null && target != null && GpsIndex < Current.gpsRoute.Length - 1) {
            var flat = target.position - player.transform.position;
            flat.y = 0;
            if (flat.magnitude < gpsArrival) GpsIndex++;
        }
    }

    // Distancia que falta por la ruta del GPS, sumando los tramos restantes.
    public float GpsDistance(Vector3 from) {
        if (Current == null || GpsTarget == null) return 0;
        float total = Vector3.Distance(Flat(from), Flat(GpsTarget.position));
        for (int i = GpsIndex; i < Current.gpsRoute.Length - 1; i++)
            total += Vector3.Distance(Flat(Current.gpsRoute[i].position), Flat(Current.gpsRoute[i + 1].position));
        return total;
    }

    static Vector3 Flat(Vector3 v) => new(v.x, 0, v.z);
}
}
