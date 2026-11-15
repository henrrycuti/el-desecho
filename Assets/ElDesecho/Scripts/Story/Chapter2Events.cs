using UnityEngine;

namespace ElDesecho {
// Capitulo 2: los tres caminos. El corto acaba en una puerta cerrada y la criatura aparece detras;
// el inundado acaba en un derrumbe; el largo sigue. La adrenalina lanza la persecucion final.
// Los caminos falsos quedan marcados con una X al volver al cruce.
public sealed class Chapter2Events : MonoBehaviour {
    public CreatureBrain creature;
    public Transform ambushFrom, chaseFrom;
    public GameObject shortMark, floodMark;

    void OnEnable() => StoryEvents.Raised += OnEvent;
    void OnDisable() => StoryEvents.Raised -= OnEvent;

    void Start() {
        if (shortMark != null) shortMark.SetActive(Equipment.ShortPathFailed);
        if (floodMark != null) floodMark.SetActive(Equipment.FloodPathFailed);
    }

    void OnEvent(string id) {
        var player = FirstPersonController.Current;
        var say = ObjectiveManager.Instance;
        if (player == null || say == null) return;
        switch (id) {
            case "corto-final":
                Equipment.ShortPathFailed = true;
                if (shortMark != null) shortMark.SetActive(true);
                say.Say("La puerta esta cerrada. El camino corto era una trampa.");
                say.Say("Detras, algo respira.");
                creature.Ambush(ambushFrom.position, player.transform.position);
                break;
            case "inundado-final":
                Equipment.FloodPathFailed = true;
                if (floodMark != null) floodMark.SetActive(true);
                say.Say("El tunel se derrumbo. No hay paso por aqui.");
                break;
            case "camino-largo":
                say.Say("Este camino casi no aparece en el plano. Parece el menos seguro... pero sigue hacia el exterior.");
                break;
            case "adrenalina":
                player.Boost(14);
                say.Say("Se aplico la adrenalina. El pulso se le disparo.");
                creature.chaseSpeed = 4.6f;
                creature.Ambush(chaseFrom.position, player.transform.position);
                break;
            case "conducto-entrada":
                say.Say("Una abertura estrecha en la pared. Agachate (C) para meterte.");
                break;
        }
    }
}
}
