using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElDesecho {
// Controla capitulos, guardado y transiciones: fundido a negro, titulo del capitulo y carga de escena.
// Existe una sola instancia durante todo el juego.
public sealed class ChapterManager : MonoBehaviour {
    const float FadeTime = .7f, TitleTime = 3.2f;

    public static ChapterManager Instance { get; private set; }
    public static bool TestMode { get; private set; }
    public float Fade { get; private set; } = 1;
    public Color FadeColor { get; private set; } = Color.black;
    public bool Busy { get; private set; }
    public int Chapter { get; private set; }
    public string PendingSpawn { get; private set; }
    public int PendingStep { get; private set; }
    public int TimesCaught { get; private set; }

    string title, subtitle;
    float titleUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot() {
        var go = new GameObject("Gestor de capitulos");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ChapterManager>();
        TestMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-elDesechoTest") >= 0;
        if (TestMode) go.AddComponent<AutoPlaytest>();
    }

    void Start() => StartCoroutine(FadeTo(0));

    public void NewGame() {
        SaveGame.Clear();
        Equipment.Reset();
        Chapter = 0;
        var first = Chapters.All[0];
        Go(first.firstScene, first.spawn, 0, first.title, "");
    }

    public void Continue() {
        var save = SaveGame.Load();
        if (save == null) { NewGame(); return; }
        Chapter = save.chapter;
        Equipment.Restore(save);
        var chapter = Chapters.All[Mathf.Clamp(save.chapter, 0, Chapters.All.Length - 1)];
        if (string.IsNullOrEmpty(save.scene)) {
            if (!chapter.Built) return;
            Go(chapter.firstScene, chapter.spawn, 0, chapter.title, "");
        } else {
            Go(save.scene, save.spawn, save.step, chapter.title, "Continuar");
        }
    }

    public static string NextChapterTitle() {
        var save = SaveGame.Load();
        return save == null ? null : Chapters.All[Mathf.Clamp(save.chapter, 0, Chapters.All.Length - 1)].title;
    }

    // Guarda un punto de control dentro del capitulo actual, con el equipo que se lleva en ese momento.
    public void Checkpoint(string scene, string spawn, int step) {
        var save = new SaveGame { chapter = Chapter, scene = scene, spawn = spawn, step = step };
        Equipment.Store(save);
        save.Store();
    }

    // Pasa a otra zona del mismo capitulo, con fundido y, si se indica, un titulo.
    public void Transition(string scene, string spawn, string titleCard = null) {
        Checkpoint(scene, spawn, 0);
        Go(scene, spawn, 0, titleCard, "");
    }

    // Termina un capitulo: guarda el siguiente y lo abre, o vuelve al menu si aun no esta construido.
    public void CompleteChapter(string closingTitle) {
        Chapter = Mathf.Min(Chapter + 1, Chapters.All.Length - 1);
        var next = Chapters.All[Chapter];
        var save = new SaveGame { chapter = Chapter, scene = next.firstScene, spawn = next.spawn, step = 0 };
        Equipment.Store(save);
        save.Store();
        if (next.Built) Go(next.firstScene, next.spawn, 0, next.title, closingTitle);
        else Go(Chapters.MenuScene, null, 0, closingTitle, next.title + " - proximamente");
    }

    // El Deshecho atrapo al jugador: vuelve al ultimo punto de control con el equipo que tenia alli.
    public void Caught() {
        if (Busy) return;
        TimesCaught++;
        var save = SaveGame.Load();
        string scene = save != null && !string.IsNullOrEmpty(save.scene) ? save.scene : SceneManager.GetActiveScene().name;
        if (save != null) Equipment.Restore(save);
        FadeColor = new Color(.25f, 0, 0);
        Go(scene, save?.spawn, save?.step ?? 0, "El Deshecho te encontro", "Vuelves al ultimo punto seguro");
    }

    public void ToMenu() => Go(Chapters.MenuScene, null, 0, null, null);

    void Go(string scene, string spawn, int step, string titleCard, string sub) {
        if (Busy) return;
        StartCoroutine(Load(scene, spawn, step, titleCard, sub));
    }

    IEnumerator Load(string scene, string spawn, int step, string titleCard, string sub) {
        Busy = true;
        yield return FadeTo(1);
        Time.timeScale = 1;
        AudioListener.pause = false;
        PendingSpawn = spawn;
        PendingStep = step;
        if (!string.IsNullOrEmpty(titleCard)) {
            title = titleCard;
            subtitle = sub;
            titleUntil = Time.unscaledTime + TitleTime;
        }
        yield return SceneManager.LoadSceneAsync(scene);
        while (Time.unscaledTime < titleUntil) yield return null;
        FadeColor = Color.black;
        yield return FadeTo(0);
        Busy = false;
    }

    IEnumerator FadeTo(float target) {
        while (!Mathf.Approximately(Fade, target)) {
            Fade = Mathf.MoveTowards(Fade, target, Time.unscaledDeltaTime / FadeTime);
            yield return null;
        }
    }

    void OnGUI() {
        GUI.depth = -1000;
        bool showTitle = Time.unscaledTime < titleUntil;
        if (Fade <= 0 && !showTitle) return;
        var old = GUI.color;
        GUI.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, showTitle ? 1 : Fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
        if (!showTitle) return;
        float scale = Ui.Scale;
        var big = Ui.Label(Mathf.RoundToInt(40 * scale), TextAnchor.MiddleCenter, FontStyle.Bold);
        var small = Ui.Label(Mathf.RoundToInt(22 * scale), TextAnchor.MiddleCenter);
        float alpha = Mathf.Clamp01((titleUntil - Time.unscaledTime) / .6f) * Mathf.Clamp01((TitleTime - (titleUntil - Time.unscaledTime)) / .6f);
        GUI.color = new Color(1, 1, 1, alpha);
        GUI.Label(new Rect(0, Screen.height * .4f, Screen.width, 60 * scale), title.ToUpperInvariant(), big);
        if (!string.IsNullOrEmpty(subtitle)) GUI.Label(new Rect(0, Screen.height * .4f + 64 * scale, Screen.width, 40 * scale), subtitle, small);
        GUI.color = old;
    }
}
}
