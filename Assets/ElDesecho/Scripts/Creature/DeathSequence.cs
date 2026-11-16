using System.Collections;
using UnityEngine;

namespace ElDesecho {
// Muerte del jugador cuando El Deshecho lo atrapa:
// la criatura lo agarra, la camara se va hacia atras y hacia arriba, el jugador ve la boca abierta
// que baja sobre el, mordida (golpe, sacudida, crujido), negro y vuelta al ultimo punto seguro.
public sealed class DeathSequence : MonoBehaviour {
    CreatureBrain creature;
    FirstPersonController player;
    Transform mouth;
    float started, shake;
    Vector3 headStart;
    bool bitten;

    public static void Play(CreatureBrain creature, FirstPersonController player) {
        if (player.GetComponent<DeathSequence>() != null) return;
        var d = player.gameObject.AddComponent<DeathSequence>();
        d.creature = creature;
        d.player = player;
    }

    IEnumerator Start() {
        started = Time.time;
        player.InputLocked = true;
        headStart = player.head.localPosition;
        mouth = FindHead(creature.transform);
        creature.Script(creature.transform.position);
        // La criatura se queda frente al jugador, a la distancia de sus brazos.
        var toPlayer = player.transform.position - creature.transform.position;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude < .01f) toPlayer = -player.transform.forward;
        creature.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
        creature.PlayAttack();
        Hit(.6f);

        yield return new WaitForSeconds(2.1f);
        // Mordida.
        bitten = true;
        Hit(1.6f);
        Crunch();
        yield return new WaitForSeconds(.15f);
        ChapterManager.Instance?.Caught(black: true);
    }

    void Hit(float amount) {
        shake = Mathf.Max(shake, amount);
        player.Shake(amount);
    }

    // La camara la mueve este script despues del controlador (LateUpdate), asi manda sobre el.
    void LateUpdate() {
        if (player == null || creature == null) return;
        float t = Time.time - started;
        var head = player.head;
        // 1) Te levanta y te echa hacia atras: la vista sube y se inclina.
        float lift = Mathf.SmoothStep(0, 1, t / .6f);
        head.localPosition = headStart + new Vector3(0, .25f * lift, -.25f * lift);
        // 2) Miras la boca que baja sobre ti.
        Vector3 target = mouth != null ? mouth.position : creature.transform.position + Vector3.up * 2.2f;
        target += Vector3.down * Mathf.SmoothStep(0, .5f, (t - .8f) / 1.2f);
        var look = Quaternion.LookRotation(target - head.position, Vector3.up);
        var wanted = look * Quaternion.Euler(0, 0, 14 * lift + (bitten ? 25 : 0));
        head.rotation = Quaternion.Slerp(head.rotation, wanted, 1 - Mathf.Exp(-(bitten ? 30 : 7) * Time.deltaTime));
        shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 1.5f);
        if (shake > 0) head.rotation *= Quaternion.Euler(Random.Range(-1f, 1f) * shake * 4, Random.Range(-1f, 1f) * shake * 4, 0);
        // La boca se acerca: la camara avanza un poco hacia ella antes de morder.
        if (!bitten && t > 1.2f) head.position = Vector3.Lerp(head.position, target, Time.deltaTime * .8f);
    }

    static Transform FindHead(Transform root) {
        Transform best = null;
        foreach (var t in root.GetComponentsInChildren<Transform>()) {
            string n = t.name.ToLowerInvariant();
            if (n.EndsWith("head") && !n.Contains("end") && !n.Contains("top")) return t;
            if (best == null && (n.Contains("jaw") || n.Contains("head"))) best = t;
        }
        return best;
    }

    // Crujido de mordida generado por codigo: golpe grave + ruido seco.
    void Crunch() {
        const int rate = 22050;
        var data = new float[rate / 2];
        var rnd = new System.Random(7);
        for (int i = 0; i < data.Length; i++) {
            float k = i / (float)rate;
            float thump = Mathf.Sin(2 * Mathf.PI * (70 - k * 60) * k) * Mathf.Exp(-k * 9);
            float crack = (float)(rnd.NextDouble() * 2 - 1) * Mathf.Exp(-k * 18) * (i % 900 < 450 ? 1 : .4f);
            data[i] = Mathf.Clamp(thump * .9f + crack * .7f, -1, 1);
        }
        var clip = AudioClip.Create("Mordida", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        var source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0;
        source.PlayOneShot(clip, 1);
    }
}
}
