using UnityEngine;

namespace ElDesecho {
// Algo que el jugador puede usar mirandolo de cerca: puertas, escaleras, tapas, objetos para recoger.
// Al usarse emite su evento de historia. Si "onlyWhenObjective" esta activo, solo responde cuando
// el objetivo actual espera ese evento, para no saltarse pasos del guion.
public sealed class Interactable : MonoBehaviour {
    public string prompt = "Usar";
    public string eventId;
    public bool onlyWhenObjective = true;
    public bool singleUse = true;
    public bool hideOnUse;
    [Tooltip("Objeto que recoge el jugador: linterna, gps, adrenalina, archivo.")]
    public string give;
    [Tooltip("Frase que aparece al usarlo.")]
    public string sayOnUse;
    [Tooltip("Hace la foto con un destello de camara.")]
    public bool photo;

    bool used;

    public bool Available {
        get {
            if (used && singleUse) return false;
            if (!onlyWhenObjective) return true;
            var objectives = ObjectiveManager.Instance;
            return objectives != null && objectives.Waiting(eventId);
        }
    }

    public void Use() {
        if (!Available) return;
        used = true;
        if (!string.IsNullOrEmpty(give)) Equipment.Give(give);
        if (photo) Hud.Flash();
        if (!string.IsNullOrEmpty(sayOnUse)) ObjectiveManager.Instance?.Say(sayOnUse);
        StoryEvents.Raise(eventId);
        if (hideOnUse) gameObject.SetActive(false);
    }
}
}
