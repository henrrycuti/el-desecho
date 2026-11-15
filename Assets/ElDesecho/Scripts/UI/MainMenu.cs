using UnityEngine;

namespace ElDesecho {
// Menu inicial: NUEVA PARTIDA, CONTINUAR y SALIR.
public sealed class MainMenu : MonoBehaviour {
    void Start() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1;
    }

    void OnGUI() {
        float s = Ui.Scale, w = Screen.width, h = Screen.height;
        var manager = ChapterManager.Instance;
        GUI.Label(new Rect(0, h * .16f, w, 90 * s), "EL DESECHO", Ui.Label(Mathf.RoundToInt(66 * s), TextAnchor.MiddleCenter, FontStyle.Bold));
        float bw = Mathf.Min(420 * s, w - 40), x = (w - bw) / 2, y = h * .46f;
        var button = Ui.Button(Mathf.RoundToInt(21 * s));
        GUI.enabled = manager != null && !manager.Busy;
        if (GUI.Button(new Rect(x, y, bw, 54 * s), "NUEVA PARTIDA", button)) manager.NewGame();
        y += 66 * s;
        var next = ChapterManager.NextChapterTitle();
        int chapter = SaveGame.Load()?.chapter ?? 0;
        bool canContinue = next != null && (Chapters.All[Mathf.Clamp(chapter, 0, Chapters.All.Length - 1)].Built || SaveGame.Load()?.scene?.Length > 0);
        GUI.enabled &= canContinue;
        if (GUI.Button(new Rect(x, y, bw, 54 * s), "CONTINUAR", button)) manager.Continue();
        GUI.enabled = true;
        if (next != null) {
            string note = canContinue ? next : next + " - proximamente";
            GUI.Label(new Rect(x - 80 * s, y + 56 * s, bw + 160 * s, 30 * s), note, Ui.Label(Mathf.RoundToInt(16 * s), TextAnchor.MiddleCenter));
        }
        y += 100 * s;
        if (PlayerPrefs.GetInt("eldesecho.completado", 0) == 1) {
            if (GUI.Button(new Rect(x, y, bw, 54 * s), "VER EPILOGO", button)) manager.PlayEpilogue();
            y += 66 * s;
        }
        if (GUI.Button(new Rect(x, y, bw, 54 * s), "SALIR", button)) Application.Quit();
    }
}
}
