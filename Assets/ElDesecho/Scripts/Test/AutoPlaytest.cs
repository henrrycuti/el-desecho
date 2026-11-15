using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElDesecho {
// Prueba automatica (argumento -elDesechoTest): juega el prologo y los capitulos 1 y 2 caminando con el cuerpo
// fisico del jugador. La criatura no puede atraparlo en este modo, pero se comprueba que oye el ruido.
// Guarda capturas en Verification/ y termina con codigo 0 si todo funciono.
public sealed class AutoPlaytest : MonoBehaviour {
    const float Timeout = 900;
    string folder;
    bool failed;

    IEnumerator Start() {
        folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Verification"));
        Directory.CreateDirectory(folder);
        SaveGame.Clear();
        StartCoroutine(Watchdog());
        yield return Run();
    }

    IEnumerator Watchdog() {
        yield return new WaitForSecondsRealtime(Timeout);
        Fail("Tiempo agotado");
    }

    void Fail(string why) {
        if (failed) return;
        failed = true;
        Debug.LogError("ELDESECHO_PRUEBA_ERROR: " + why + " (escena " + SceneManager.GetActiveScene().name + ", objetivo " + ObjectiveManager.Instance?.Index + ")");
        Application.Quit(1);
    }

    IEnumerator Shot(string name) {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
        yield return new WaitForSecondsRealtime(.4f);
    }

    IEnumerator WaitScene(string scene) {
        var manager = ChapterManager.Instance;
        float until = Time.realtimeSinceStartup + 40;
        while (SceneManager.GetActiveScene().name != scene || manager.Busy) {
            if (Time.realtimeSinceStartup > until) { Fail("No se cargo " + scene + " (ocupado " + manager.Busy + ", fundido " + manager.Fade + ")"); yield break; }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(.5f);
    }

    IEnumerator Walk(Vector3 target, float reach = 1.2f) {
        var player = FirstPersonController.Current;
        float best = float.MaxValue, lastProgress = Time.time;
        player.BotWalkTo(target);
        while (!failed) {
            if (player == null) yield break; // la escena cambio (por ejemplo, al terminar el capitulo)
            var d = target - player.transform.position;
            d.y = 0;
            float distance = d.magnitude;
            if (distance < reach) break;
            if (distance < best - .2f) { best = distance; lastProgress = Time.time; }
            if (Time.time - lastProgress > (player.Trapped ? 9 : 5)) { Fail("El jugador se atasco en " + player.transform.position + " camino a " + target); break; }
            yield return null;
        }
        if (player != null) player.BotWalkTo(null);
    }

    // Camina por las celdas del mapa (si la escena tiene CellGrid) hasta un punto. Se agacha en los conductos.
    IEnumerator Travel(Vector3 target, float reach = 1.2f) {
        var grid = CellGrid.Instance;
        var player = FirstPersonController.Current;
        if (grid != null) {
            foreach (var cell in grid.Path(grid.ToCell(player.transform.position), grid.ToCell(target), false)) {
                player.BotCrouch = grid.At(cell) == 'd';
                yield return Walk(grid.Center(cell), 1.3f);
                if (failed || player == null) yield break;
            }
        }
        yield return Walk(target, reach);
        if (player != null) player.BotCrouch = false;
    }

    IEnumerator Use(Interactable item) {
        if (item == null) { Fail("Falta un objeto para usar"); yield break; }
        var player = FirstPersonController.Current;
        var center = item.GetComponent<Collider>().bounds.center;
        yield return Travel(center, 1.4f);
        player.LookAt(center);
        yield return null;
        if (!player.TryInteract()) Fail("No se pudo usar: " + item.prompt);
        yield return new WaitForSeconds(.3f);
    }

    IEnumerator Use(string id) {
        Interactable found = null;
        foreach (var candidate in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            if (candidate.eventId == id && candidate.Available) found = candidate;
        if (found == null) { Fail("No hay nada disponible para: " + id); yield break; }
        yield return Use(found);
    }

    IEnumerator SkipDialogue() {
        var objectives = ObjectiveManager.Instance;
        float until = Time.time + 30;
        yield return null;
        while (objectives.DialoguePending && Time.time < until) {
            yield return new WaitForSeconds(.4f);
            objectives.Advance();
        }
    }

    IEnumerator WaitStep(int index, float seconds) {
        float until = Time.time + seconds;
        while (ObjectiveManager.Instance.Index < index) {
            if (Time.time > until) { Fail("El guion no llego al paso " + index); yield break; }
            yield return null;
        }
    }

    // Capitulo desde el que empieza la prueba (-elDesechoTestDesde=3). 0 = desde el menu.
    static int StartChapter() {
        foreach (var a in Environment.GetCommandLineArgs())
            if (a.StartsWith("-elDesechoTestDesde=") && int.TryParse(a.Substring(20), out int n)) return n;
        return 0;
    }

    IEnumerator Run() {
        var manager = ChapterManager.Instance;
        yield return WaitScene(Chapters.MenuScene);
        yield return Shot("01-menu");
        int from = StartChapter();
        if (from == 0) manager.NewGame();
        else {
            Equipment.Reset();
            if (from >= 4) Equipment.Goggles = Equipment.Key = Equipment.Antidote = true;
            var chapter = Chapters.All[from];
            var save = new SaveGame { chapter = from, scene = chapter.firstScene, spawn = chapter.spawn };
            Equipment.Store(save);
            save.Store();
            manager.Continue();
        }
        var sections = new Func<IEnumerator>[] { Prologue, Chapter1, Chapter2, Chapter3, Chapter4 };
        for (int i = Mathf.Max(0, from); i < sections.Length && !failed; i++) yield return sections[i]();
        if (failed) yield break;

        yield return WaitScene(Chapters.MenuScene);
        var saved = SaveGame.Load();
        if (saved == null || saved.chapter != sections.Length) { Fail("No se guardo el paso al capitulo " + sections.Length); yield break; }
        yield return Shot("99-menu-final");
        Debug.Log("ELDESECHO_PRUEBA_OK: capitulos " + from + " a " + (sections.Length - 1) + " completos y guardado del siguiente");
        Application.Quit(0);
    }

    IEnumerator Prologue() {
        yield return WaitScene("P0_Calle");
        yield return Shot("02-calle");
        var route = FindFirstObjectByType<BotRoute>();
        if (route == null) { Fail("Falta la ruta de prueba de la calle"); yield break; }
        foreach (var point in route.points) yield return Walk(point.position);
        yield return Use("tapa");
        yield return Use("bajar");
        yield return WaitScene("P1_Alcantarilla");
        var gps = ObjectiveManager.Instance.Current.gpsRoute;
        for (int i = 0; i < gps.Length && !failed; i++) yield return Walk(gps[i].position, 1.4f);
        yield return Shot("03-prologo-puerta");
        yield return Use("puerta-laboratorio");
    }

    IEnumerator Chapter1() {
        yield return WaitScene("C1_Laboratorio");
        if (!Equipment.Flashlight || !Equipment.Gps) { Fail("Al llegar al laboratorio deberia llevar linterna y GPS"); yield break; }
        yield return Shot("04-laboratorio");
        var grid = CellGrid.Instance;
        yield return Travel(grid.Center(new Vector2Int(7, 8)));
        foreach (var item in FindObjectsByType<Interactable>(FindObjectsSortMode.InstanceID))
            if (item.eventId == "foto" && !failed) yield return Use(item);
        yield return Shot("05-fotos");
        yield return WaitStep(2, 5);
        yield return Use("cientifico");
        yield return Shot("06-cientifico");
        yield return SkipDialogue();
        yield return WaitStep(4, 10);
        yield return new WaitForSeconds(4);
        yield return Shot("07-ataque");
        yield return WaitStep(5, 25);
        if (Equipment.Flashlight || Equipment.Gps) { Fail("Tras el ataque deberia haber perdido la linterna y el GPS"); yield break; }
        var creature = FindFirstObjectByType<CreatureBrain>();
        if (creature == null || creature.State == CreatureState.Dormant) { Fail("La criatura no esta activa tras el ataque"); yield break; }
        yield return new WaitForSeconds(5.5f);
        float before = creature.LastHeardAt;
        Noise.Emit(creature.transform.position + Vector3.right * 2, 8, false);
        yield return null;
        if (creature.LastHeardAt <= before || creature.State is not (CreatureState.Chase or CreatureState.Investigate)) { Fail("La criatura no reacciono al ruido (estado " + creature.State + ")"); yield break; }
        Debug.Log("ELDESECHO_PRUEBA: la criatura oyo el ruido y paso a " + creature.State);
        yield return Shot("08-laboratorio-rojo");
        yield return Use("linterna");
        if (!Equipment.Flashlight) { Fail("No se recupero la linterna"); yield break; }
        yield return Use("gps");
        if (!Equipment.Gps) { Fail("No se recupero el GPS"); yield break; }
        yield return Use("salida");
    }

    IEnumerator Chapter2() {
        var manager = ChapterManager.Instance;
        yield return WaitScene("C2_TresCaminos");
        yield return Shot("09-cruce");
        yield return Use("plano");
        yield return SkipDialogue();
        int stepBefore = ObjectiveManager.Instance.Index;
        int caughtBefore = manager.TimesCaught;
        manager.Caught();
        yield return WaitScene("C2_TresCaminos");
        if (manager.TimesCaught != caughtBefore + 1 || ObjectiveManager.Instance.Index != stepBefore) { Fail("Al ser atrapado no volvio al punto de control del cruce"); yield break; }
        yield return SkipDialogue();
        var grid = CellGrid.Instance;
        yield return Travel(grid.Center(new Vector2Int(12, 3)));
        yield return Shot("10-camino-largo");
        yield return Travel(grid.Center(new Vector2Int(14, 9)));
        yield return WaitStep(2, 5);
        yield return Use("adrenalina");
        if (FirstPersonController.Current.BoostLeft <= 0) { Fail("La adrenalina no hizo efecto"); yield break; }
        yield return Shot("11-persecucion");
        yield return Travel(grid.Center(grid.Find('Z')));
    }

    IEnumerator Chapter3() {
        yield return WaitScene("C3_Investigacion");
        yield return Shot("12-investigacion");
        var grid = CellGrid.Instance;
        yield return Travel(grid.Center(new Vector2Int(12, 4)));
        yield return WaitStep(1, 5);
        yield return Use("gafas");
        var tools = PlayerTools.Current;
        if (!Equipment.Goggles || !tools.ToggleGoggles() || !tools.GogglesOn) { Fail("Las gafas no se activan"); yield break; }
        yield return new WaitForSeconds(.6f);
        yield return Shot("13-gafas");
        if (tools.Battery >= tools.batterySeconds) { Fail("Las gafas no gastan bateria"); yield break; }
        tools.ToggleGoggles();
        yield return Use("cuerpo");
        yield return SkipDialogue();
        yield return WaitStep(4, 10);
        if (!Equipment.Key || !Equipment.Antidote) { Fail("El cuerpo no dio la llave y el antidoto"); yield break; }
        var player = FirstPersonController.Current;
        var small = FindFirstObjectByType<CreatureBrain>();
        small.Teleport(player.transform.position + player.transform.forward * 3);
        yield return null;
        bool used = tools.UseAntidote();
        bool stunned = false;
        foreach (var c in FindObjectsByType<CreatureBrain>(FindObjectsSortMode.None)) stunned |= c.State == CreatureState.Stunned;
        if (!used || !stunned) { Fail("El antidoto no detuvo a la criatura (usado " + used + ")"); yield break; }
        Debug.Log("ELDESECHO_PRUEBA: el antidoto detuvo a una criatura");
        yield return Shot("14-antidoto");
        yield return Use("puerta-reforzada");
    }

    IEnumerator Chapter4() {
        yield return WaitScene("C4_PuertaSalida");
        yield return Shot("15-sector-inundado");
        var grid = CellGrid.Instance;
        yield return Travel(grid.Center(new Vector2Int(19, 5)));
        yield return WaitStep(1, 5);
        PlayerTools.Current.ToggleGoggles();
        yield return new WaitForSeconds(.5f);
        yield return Shot("16-sector-seco-gafas");
        PlayerTools.Current.ToggleGoggles();
        yield return Travel(grid.Center(new Vector2Int(1, 9)));
        yield return WaitStep(2, 5);
        yield return Shot("17-vapor");
        var jet = FindFirstObjectByType<SteamJet>();
        if (jet == null) { Fail("No hay chorros de vapor"); yield break; }
        float until = Time.time + 6;
        bool sawActive = false;
        while (Time.time < until && !sawActive) { sawActive = jet.Active; yield return null; }
        if (!sawActive) { Fail("El vapor nunca se activa"); yield break; }
        yield return Travel(grid.Center(new Vector2Int(19, 11)));
    }
}
}
