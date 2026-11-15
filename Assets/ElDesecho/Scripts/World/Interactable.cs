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
        StoryEvents.Raise(eventId);
        if (hideOnUse) gameObject.SetActive(false);
    }
}
}
