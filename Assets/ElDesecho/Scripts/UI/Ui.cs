using UnityEngine;

namespace ElDesecho {
// Estilos comunes de la interfaz, escalados para que se lean igual en telefono y en PC.
public static class Ui {
    public static float Scale => Mathf.Max(.6f, Screen.height / 720f);

    public static GUIStyle Label(int size, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal) {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, fontStyle = style, wordWrap = true };
        s.normal.textColor = Color.white;
        return s;
    }

    public static GUIStyle Button(int size) => new(GUI.skin.button) { fontSize = size, wordWrap = true };

    public static void Fill(Rect r, Color c) {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // Texto con sombra para que se lea sobre escenas oscuras o claras.
    public static void Shadowed(Rect r, string text, GUIStyle style) {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0, 0, 0, .85f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = old;
        GUI.Label(r, text, style);
    }
}
}
