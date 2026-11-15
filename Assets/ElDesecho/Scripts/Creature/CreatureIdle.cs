using UnityEngine;

namespace ElDesecho {
// Adaptador minimo de El Deshecho: lo deja quieto en reposo, respirando y con la boca entreabierta.
// No tiene inteligencia todavia; la persecucion se anadira en un capitulo posterior.
public sealed class CreatureIdle : MonoBehaviour {
    public float breathSpeed = 2.5f;
    Animator animator;
    SkinnedMeshRenderer skin;
    int breath = -1, mouth = -1;
    float seed;

    void Awake() {
        animator = GetComponentInChildren<Animator>();
        skin = GetComponentInChildren<SkinnedMeshRenderer>();
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
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetFloat("Speed", 0);
        if (skin == null) return;
        float t = Time.time * breathSpeed + seed;
        if (breath >= 0) skin.SetBlendShapeWeight(breath, (Mathf.Sin(t) + 1) * 40);
        if (mouth >= 0) skin.SetBlendShapeWeight(mouth, 12 + Mathf.Sin(t * .7f) * 6);
    }
}
}
