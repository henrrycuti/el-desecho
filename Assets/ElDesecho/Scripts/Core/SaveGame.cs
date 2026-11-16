using System;
using UnityEngine;

namespace ElDesecho {
// Partida guardada: capitulo alcanzado, ultimo punto de control (escena, punto de aparicion y objetivo) y equipo.
[Serializable]
public sealed class SaveGame {
    const string Key = "eldesecho.partida";
    public int chapter;
    public string scene, spawn;
    public int step;
    public bool flashlight = true, gps = true, file, goggles, key, antidote;
    public int doses, stones;

    public static bool Exists => PlayerPrefs.HasKey(Key);

    public static SaveGame Load() {
        try {
            var text = PlayerPrefs.GetString(Key, "");
            if (text.Length > 0) return JsonUtility.FromJson<SaveGame>(text);
        } catch (Exception e) {
            Debug.LogWarning("Partida guardada ilegible: " + e.Message);
        }
        return null;
    }

    public void Store() {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    public static void Clear() {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
}
