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
    [Tooltip("Evento que completa el paso (de un StoryTrigger o un Interactable).")]
    public string completeOn;
    [Tooltip("Frases que aparecen como subtitulos al empezar el paso.")]
    [TextArea] public string[] lines = new string[0];
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

// Objetivos de la escena: muestra el objetivo actual, los subtitulos y el GPS, y avanza con los eventos de historia.
// Tambien coloca al jugador en el punto de aparicion indicado por el gestor de capitulos.
public sealed class ObjectiveManager : MonoBehaviour {
    public List<ObjectiveStep> steps = new();
    public float gpsArrival = 3.5f;

    public static ObjectiveManager Instance { get; private set; }
    public int Index { get; private set; }
    public ObjectiveStep Current => Index < steps.Count ? steps[Index] : null;
    public int GpsIndex { get; private set; }
    public Transform GpsTarget => Current != null && GpsIndex < Current.gpsRoute.Length ? Current.gpsRoute[GpsIndex] : null;
    public string Subtitle => subtitles.Count > 0 ? subtitles[0] : null;
    public float ObjectiveShownAt { get; private set; }

    readonly List<string> subtitles = new();
    float subtitleUntil;

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
        Begin(manager != null ? Mathf.Clamp(manager.PendingStep, 0, Mathf.Max(0, steps.Count - 1)) : 0);
    }

    public bool Waiting(string id) => Current != null && !string.IsNullOrEmpty(id) && Current.completeOn == id;

    void Begin(int index) {
        Index = index;
        GpsIndex = 0;
        var step = Current;
        if (step == null) return;
        ObjectiveShownAt = Time.time;
        subtitles.Clear();
        subtitles.AddRange(step.lines);
        subtitleUntil = Time.time + SubtitleTime(Subtitle);
        if (step.checkpoint && ChapterManager.Instance != null)
            ChapterManager.Instance.Checkpoint(SceneManager.GetActiveScene().name, step.checkpointSpawn, index);
    }

    static float SubtitleTime(string text) => text == null ? 0 : Mathf.Clamp(text.Length * .06f, 2.5f, 7f);

    void OnEvent(string id) {
        if (!Waiting(id)) return;
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
        if (subtitles.Count > 0 && Time.time >= subtitleUntil) {
            subtitles.RemoveAt(0);
            subtitleUntil = Time.time + SubtitleTime(Subtitle);
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
