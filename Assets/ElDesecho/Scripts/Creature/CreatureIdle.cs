using UnityEngine;

namespace ElDesecho {
// Respiracion y boca de El Deshecho. Si no tiene cerebro (CreatureBrain), ademas lo deja quieto en reposo.
public sealed class CreatureIdle : MonoBehaviour {
    public float breathSpeed = 2.5f;
    Animator animator;
    SkinnedMeshRenderer skin;
    CreatureBrain brain;
    int breath = -1, mouth = -1;
    float seed;

    void Awake() {
        animator = GetComponentInChildren<Animator>();
        skin = GetComponentInChildren<SkinnedMeshRenderer>();
        brain = GetComponent<CreatureBrain>();
        seed = Random.value * 10;
        if (skin == null) return;
        var mesh = skin.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++) {
            string n = mesh.GetBlendShapeName(i);
            if (n.EndsWith("Breath")) breath = i;
            if (n.EndsWith("MawOpen")) mouth = i;
        }
    }

    void Update() {
        if (brain == null && animator != null && animator.runtimeAnimatorController != null) animator.SetFloat("Speed", 0);
        if (skin == null) return;
        bool hunting = brain != null && brain.State == CreatureState.Chase;
        float t = Time.time * (hunting ? breathSpeed * 2.5f : breathSpeed) + seed;
        if (breath >= 0) skin.SetBlendShapeWeight(breath, (Mathf.Sin(t) + 1) * 40);
        if (mouth >= 0) skin.SetBlendShapeWeight(mouth, hunting ? 75 + Mathf.Sin(t * 2) * 10 : 12 + Mathf.Sin(t * .7f) * 6);
    }
}
}
