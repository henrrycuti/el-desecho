using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElDesecho {
// Prueba automatica (argumento -elDesechoTest): recorre el prologo caminando con el cuerpo fisico del jugador.
// Menu -> Calle -> tapa de mantenimiento -> escalera -> Alcantarilla siguiendo el GPS -> puerta del laboratorio -> menu.
// Guarda capturas en Verification/ y termina con codigo 0 si todo funciono.
public sealed class AutoPlaytest : MonoBehaviour {
    const float Timeout = 300;
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
        Debug.LogError("ELDESECHO_PRUEBA_ERROR: " + why + " (escena " + SceneManager.GetActiveScene().name + ")");
        Application.Quit(1);
    }

    IEnumerator Shot(string name) {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
        yield return new WaitForSecondsRealtime(.4f);
    }

    IEnumerator WaitScene(string scene) {
        var manager = ChapterManager.Instance;
        float until = Time.realtimeSinceStartup + 30;
        while (SceneManager.GetActiveScene().name != scene || manager.Busy) {
            if (Time.realtimeSinceStartup > until) { Fail("No se cargo " + scene); yield break; }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(.5f);
    }

    IEnumerator Walk(Vector3 target, float reach = 1.2f) {
        var player = FirstPersonController.Current;
        float best = float.MaxValue, lastProgress = Time.time;
        player.BotWalkTo(target);
        while (!failed) {
            var d = target - player.transform.position;
            d.y = 0;
            float distance = d.magnitude;
            if (distance < reach) break;
            if (distance < best - .2f) { best = distance; lastProgress = Time.time; }
            if (Time.time - lastProgress > 5) { Fail("El jugador se atasco en " + player.transform.position + " camino a " + target); break; }
            yield return null;
        }
        player.BotWalkTo(null);
    }

    IEnumerator Use(string id) {
        Interactable item = null;
        foreach (var candidate in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            if (candidate.eventId == id) item = candidate;
        if (item == null) { Fail("No existe el interactuable " + id); yield break; }
        var player = FirstPersonController.Current;
        var center = item.GetComponent<Collider>().bounds.center;
        yield return Walk(center, 1.4f);
        player.LookAt(center);
        yield return null;
        if (!player.TryInteract()) Fail("No se pudo usar: " + item.prompt);
    }

    IEnumerator Run() {
        var manager = ChapterManager.Instance;
        yield return WaitScene(Chapters.MenuScene);
        yield return Shot("01-menu");
        manager.NewGame();

        yield return WaitScene("P0_Calle");
        var objectives = ObjectiveManager.Instance;
        if (objectives == null || objectives.Current == null) { Fail("La calle no tiene objetivos"); yield break; }
        yield return Shot("02-calle");
        var route = FindFirstObjectByType<BotRoute>();
        if (route == null) { Fail("Falta la ruta de prueba de la calle"); yield break; }
        foreach (var point in route.points) yield return Walk(point.position);
        yield return Shot("03-callejon");
        yield return Use("tapa");
        yield return Use("bajar");

        yield return WaitScene("P1_Alcantarilla");
        objectives = ObjectiveManager.Instance;
        yield return Shot("04-alcantarilla");
        var gps = objectives.Current.gpsRoute;
        if (gps.Length < 3) { Fail("El GPS no tiene ruta"); yield break; }
        for (int i = 0; i < gps.Length && !failed; i++) {
            yield return Walk(gps[i].position, 1.4f);
            if (i == gps.Length / 2) yield return Shot("05-gps");
        }
        yield return new WaitForSeconds(.5f);
        if (objectives.Current == null || objectives.Current.completeOn != "puerta-laboratorio") { Fail("Al final del GPS no se pide abrir la puerta"); yield break; }
        yield return Shot("06-puerta");
        yield return Use("puerta-laboratorio");

        yield return WaitScene(Chapters.MenuScene);
        var save = SaveGame.Load();
        if (save == null || save.chapter != 1) { Fail("No se guardo el paso al capitulo 1"); yield break; }
        yield return Shot("07-menu-final");
        if (failed) yield break;
        Debug.Log("ELDESECHO_PRUEBA_OK: menu, calle, tapa, escalera, alcantarilla con GPS, puerta del laboratorio y guardado del capitulo 1");
        Application.Quit(0);
    }
}
}
