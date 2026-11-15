namespace ElDesecho {
// Lo que lleva el periodista. Se guarda con cada punto de control.
public static class Equipment {
    public static bool Flashlight = true, Gps = true, Adrenaline, File;
    // Caminos falsos ya probados en el capitulo 2 (se marcan con una X en el cruce).
    public static bool ShortPathFailed, FloodPathFailed;

    public static void Reset() {
        Flashlight = Gps = true;
        Adrenaline = File = ShortPathFailed = FloodPathFailed = false;
    }

    public static void Give(string item) {
        switch (item) {
            case "linterna": Flashlight = true; break;
            case "gps": Gps = true; break;
            case "adrenalina": Adrenaline = true; break;
            case "archivo": File = true; break;
        }
    }

    public static void Store(SaveGame save) {
        save.flashlight = Flashlight;
        save.gps = Gps;
        save.file = File;
    }

    public static void Restore(SaveGame save) {
        Flashlight = save.flashlight;
        Gps = save.gps;
        File = save.file;
        Adrenaline = false;
    }
}
}
