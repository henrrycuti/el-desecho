namespace ElDesecho {
// Capitulos del juego, en el orden de Docs/HISTORIA_EL_DESECHO.md.
// Un capitulo sin escena todavia no esta construido.
public sealed class Chapter {
    public readonly string title, firstScene, spawn;
    public Chapter(string title, string firstScene, string spawn) {
        this.title = title;
        this.firstScene = firstScene;
        this.spawn = spawn;
    }
    public bool Built => !string.IsNullOrEmpty(firstScene);
}

public static class Chapters {
    public const string MenuScene = "Menu";
    public static readonly Chapter[] All = {
        new("Prologo: La mision", "P0_Calle", "inicio"),
        new("Capitulo 1: El laboratorio silencioso", null, null),
        new("Capitulo 2: Tres caminos", null, null),
        new("Capitulo 3: Lo que queda de la investigacion", null, null),
        new("Capitulo 4: La puerta de salida", null, null),
        new("Capitulo 5: La luz del dia", null, null),
        new("Epilogo: La verdad no se queda enterrada", null, null),
    };
}
}
