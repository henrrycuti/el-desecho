using System.Collections;
using UnityEngine;

namespace ElDesecho {
// Capitulo 5: la ultima persecucion. Al abrir la puerta el chirrido despierta a las criaturas; El Deshecho
// persigue al jugador por cuatro tramos (cada tramo es un punto de control) con musica que se acelera.
// Una masa pequena cierra el paso en el segundo tramo (antidoto). Al final, la luz del amanecer la hace retroceder.
public sealed class Chapter5Chase : MonoBehaviour {
    public CreatureBrain hunter, blocker;
    public Transform[] segmentStarts = new Transform[4];
    public Transform blockerAt, exitPoint, retreatTo;
    public EventMover door;
    AudioSource music, effects;
    bool ending;

    void OnEnable() => StoryEvents.Raised += OnEvent;
    void OnDisable() => StoryEvents.Raised -= OnEvent;

    void Awake() {
        // Capa 9: criaturas. Capa 10: puentes sobre las zanjas (solo para criaturas). Capa 11: cables y tuberias bajas (solo para el jugador).
        Physics.IgnoreLayerCollision(0, 10, true);
        Physics.IgnoreLayerCollision(9, 11, true);
        music = gameObject.AddComponent<AudioSource>();
        music.clip = Sounds.Drums("Persecucion");
        music.loop = true;
        music.volume = .55f;
        effects = gameObject.AddComponent<AudioSource>();
    }

    void Start() {
        var manager = ChapterManager.Instance;
        if (manager != null && manager.PendingStep >= 1) OpenDoorNow();
    }

    void OpenDoorNow() {
        if (door == null) return;
        door.transform.localRotation *= Quaternion.Euler(door.rotateBy);
        door.transform.localPosition += door.moveBy;
        door.enabled = false;
    }

    void OnEvent(string id) {
        var say = ObjectiveManager.Instance;
        var player = FirstPersonController.Current;
        switch (id) {
            case "puerta":
                effects.PlayOneShot(Sounds.Screech("Chirrido", 2.5f), .8f);
                player?.Shake(.8f);
                Equipment.AdrenalineDoses = Mathf.Max(Equipment.AdrenalineDoses, 2);
                say?.Say("El mecanismo chirrio con fuerza. El sonido viajo por los tuneles y desperto a las criaturas.");
                break;
            case "tramo-1": StartSegment(0, 1f, 3); break;
            case "tramo-2": StartSegment(1, 1.15f, 1); break;
            case "tramo-3": StartSegment(2, 1.3f, 1); break;
            case "tramo-4": StartSegment(3, 1.45f, 1); break;
            case "bloqueo":
                blocker.Hunting = true;
                blocker.Ambush(blockerAt.position, player.transform.position);
                say?.Say("¡Otra masa le cierra el paso! El antidoto (Q) puede detenerla.");
                break;
            case "salida":
                if (!ending) StartCoroutine(Ending());
                break;
        }
    }

    void StartSegment(int index, float pitch, float delay) {
        if (!music.isPlaying) music.Play();
        music.pitch = pitch;
        StartCoroutine(Release(index, delay));
    }

    IEnumerator Release(int index, float delay) {
        yield return new WaitForSeconds(delay);
        if (ending || FirstPersonController.Current == null) yield break;
        hunter.Hunting = true;
        hunter.Ambush(segmentStarts[index].position, FirstPersonController.Current.transform.position);
    }

    void Update() {
        var player = FirstPersonController.Current;
        if (music.isPlaying && !ending && player != null && Random.value < Time.deltaTime * .8f) player.Shake(.12f * music.pitch);
    }

    IEnumerator Ending() {
        ending = true;
        var player = FirstPersonController.Current;
        var say = ObjectiveManager.Instance;
        player.InputLocked = true;
        hunter.Hunting = false;
        say.Say("Detras de la compuerta brillaba la luz del amanecer. Salio a la superficie justo cuando la criatura llegaba.");
        if (hunter.State != CreatureState.Dormant) {
            hunter.Teleport(exitPoint.position + (retreatTo.position - exitPoint.position).normalized * 4);
            hunter.Script(retreatTo.position);
            player.LookAt(hunter.transform.position + Vector3.up * 1.5f);
        }
        for (float v = music.volume; v > 0; v -= Time.deltaTime * .3f) { music.volume = v; yield return null; }
        music.Stop();
        say.Say("La luz del sol hizo que la masa retrocediera hacia la oscuridad. No podia seguirlo al exterior.");
        yield return new WaitForSeconds(5);
        StoryEvents.Raise("final");
    }
}
}
