namespace ElDesecho {
// Lo que lleva el periodista. Se guarda con cada punto de control.
public static class Equipment {
    public static bool Flashlight = true, Gps = true, Adrenaline, File, Goggles, Key, Antidote;
    // Dosis de adrenalina que se pueden usar a voluntad (capitulo 5).
    public static int AdrenalineDoses;
    // Piedras para lanzar y distraer a El Deshecho (maximo MaxStones).
    public static int Stones;
    public const int MaxStones = 5;
    // Caminos falsos ya probados en el capitulo 2 (se marcan con una X en el cruce).
    public static bool ShortPathFailed, FloodPathFailed;

    public static void Reset() {
        Flashlight = Gps = true;
        Adrenaline = File = Goggles = Key = Antidote = ShortPathFailed = FloodPathFailed = false;
        AdrenalineDoses = Stones = 0;
    }

    public static void Give(string item) {
        switch (item) {
            case "linterna": Flashlight = true; break;
            case "gps": Gps = true; break;
            case "adrenalina": Adrenaline = true; break;
            case "archivo": File = true; break;
            case "gafas": Goggles = true; break;
            case "llave": Key = true; break;
            case "antidoto": Antidote = true; break;
            case "dosis": AdrenalineDoses++; break;
            case "piedra": Stones = System.Math.Min(MaxStones, Stones + 1); break;
        }
    }

    public static void Store(SaveGame save) {
        save.flashlight = Flashlight;
        save.gps = Gps;
        save.file = File;
        save.goggles = Goggles;
        save.key = Key;
        save.antidote = Antidote;
        save.doses = AdrenalineDoses;
        save.stones = Stones;
    }

    public static void Restore(SaveGame save) {
        Flashlight = save.flashlight;
        Gps = save.gps;
        File = save.file;
        Goggles = save.goggles;
        Key = save.key;
        Antidote = save.antidote;
        AdrenalineDoses = save.doses;
        Stones = save.stones;
        Adrenaline = false;
    }
}
}
