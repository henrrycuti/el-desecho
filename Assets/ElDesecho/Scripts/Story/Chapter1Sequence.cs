using System.Collections;
using UnityEngine;

namespace ElDesecho {
// Capitulo 1: la alarma y el ataque. La criatura irrumpe, aplasta el GPS, el periodista cae y pierde la linterna,
// el cientifico la frena con una barra y la puerta de entrada se cierra: no hay vuelta atras.
public sealed class Chapter1Sequence : MonoBehaviour {
    public CreatureBrain creature;
    public Transform scientist, vent, entranceDoor, droppedFlashlight;
    public AlarmLights alarm;
    public int firstStepAfterAttack = 5;

    void OnEnable() => StoryEvents.Raised += OnEvent;
    void OnDisable() => StoryEvents.Raised -= OnEvent;

    void Start() {
        // Al volver a un punto de control despues del ataque, el laboratorio ya esta en alarma.
        var manager = ChapterManager.Instance;
        int step = manager != null ? manager.PendingStep : 0;
        if (step >= firstStepAfterAttack) AfterAttack();
        else if (creature != null) creature.SetDormant(true);
        if (scientist != null && step < firstStepAfterAttack) scientist.gameObject.SetActive(step >= 2);
    }

    void OnEvent(string id) {
        if (id == "alarma") StartCoroutine(Attack());
        if (id == "cientifico-aparece" && scientist != null) scientist.gameObject.SetActive(true);
        if (id == "archivo") Equipment.File = true;
    }

    void AfterAttack() {
        if (alarm != null) alarm.Activate();
        if (scientist != null) scientist.gameObject.SetActive(false);
        CloseDoor();
        if (creature != null) {
            creature.startDormant = false;
            creature.SetDormant(false);
        }
    }

    void CloseDoor() {
        if (entranceDoor != null) entranceDoor.localRotation = Quaternion.identity;
    }

    IEnumerator Attack() {
        var player = FirstPersonController.Current;
        var say = ObjectiveManager.Instance;
        player.InputLocked = true;
        alarm.Activate();
        say.Say("Las luces cambiaron a rojo. Una alarma comenzo a sonar en todo el edificio.");
        yield return new WaitForSeconds(2.2f);

        creature.Teleport(vent.position);
        creature.SetDormant(false);
        say.Say("Algo salio del conducto de ventilacion.");
        float until = Time.time + 6;
        while (Time.time < until) {
            var toPlayer = player.transform.position - creature.transform.position;
            toPlayer.y = 0;
            if (toPlayer.magnitude < 2) break;
            creature.Script(player.transform.position - toPlayer.normalized * 1.6f);
            player.LookAt(creature.transform.position + Vector3.up * 1.8f);
            yield return null;
        }
        player.LookAt(creature.transform.position + Vector3.up * 1.8f);
        creature.PlayAttack();
        player.Shake(1);
        Equipment.Gps = false;
        say.Say("La criatura le arrebato el GPS y lo aplasto contra el cemento.");
        yield return new WaitForSeconds(1.6f);

        Equipment.Flashlight = false;
        player.SetFlashlight(false);
        player.Shake(.8f);
        if (droppedFlashlight != null) {
            droppedFlashlight.gameObject.SetActive(true);
            droppedFlashlight.position = player.transform.position + player.transform.forward * .6f + Vector3.up * .3f;
            var rb = droppedFlashlight.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = -player.transform.right * 3 + Vector3.up;
        }
        say.Say("Cayo al suelo. La linterna rodo lejos, hacia la oscuridad.");
        yield return new WaitForSeconds(1.6f);

        if (scientist != null) scientist.position = creature.transform.position - creature.transform.right * 1.1f;
        creature.Stun(5);
        say.Say("El cientifico le clavo una barra de metal en el cuerpo. -¡CORRE!");
        yield return new WaitForSeconds(1.2f);
        CloseDoor();
        player.Shake(.5f);
        say.Say("La puerta por la que entro se cerro de golpe. No hay vuelta atras.");
        yield return new WaitForSeconds(.8f);
        if (scientist != null) scientist.gameObject.SetActive(false);
        player.InputLocked = false;
        StoryEvents.Raise("ataque-fin");
    }
}
}
