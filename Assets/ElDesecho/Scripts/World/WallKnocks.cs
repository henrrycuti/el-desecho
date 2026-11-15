using UnityEngine;

namespace ElDesecho {
// Golpes detras de las paredes: la criatura sigue los pasos del jugador por los conductos.
// Suenan cerca del jugador, a un lado, cada cierto tiempo mientras se mueve.
public sealed class WallKnocks : MonoBehaviour {
    public float minGap = 9, maxGap = 16;
    float next;
    bool said;
    AudioSource source;
    AudioClip thump;

    void Start() {
        next = Time.time + 6;
        thump = Sounds.Thump("Golpe");
        source = new GameObject("Golpes").AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.spatialBlend = 1;
        source.maxDistance = 20;
    }

    void Update() {
        var player = FirstPersonController.Current;
        if (player == null || Time.time < next) return;
        next = Time.time + Random.Range(minGap, maxGap);
        var side = player.transform.right * (Random.value < .5f ? -1 : 1) * 3.2f;
        source.transform.position = player.transform.position + side + Vector3.up * 2;
        for (int i = 0; i < 3; i++) source.PlayOneShot(thump, 1 - i * .2f);
        player.Shake(.15f);
        if (!said) {
            said = true;
            ObjectiveManager.Instance?.Say("Golpes detras de las paredes. Algo lo seguia por los conductos.");
        }
    }
}
}
