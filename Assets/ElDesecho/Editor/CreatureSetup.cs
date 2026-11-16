using System;
using System.Linq;
using ElDesecho;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Prepara a El Deshecho (modelo traido de UMBRA) como prefab colocable en escena, en reposo.
public static class CreatureSetup {
    const string Folder = "Assets/ElDesecho/Creature";
    const string Model = Folder + "/Model/ElDeshecho.fbx";
    public const string PrefabPath = Folder + "/ElDeshecho.prefab";
    static readonly string[] Loops = { "Idle", "Patrol", "Chase", "Investigate", "Stunned", "Cast" };

    // Modelo nuevo (Tripo + animaciones encorvadas de Tools/Blender/desecho_tripo.py). false = modelo viejo de respaldo.
    const bool UseTripo = true;
    const string TripoFolder = Folder + "/Tripo";
    const string TripoModel = TripoFolder + "/ElDeshechoTripo.fbx";
    const float TripoHeight = 2.3f; // altura de pie aproximada en metros (encorvado queda en ~1,9 m)

    [MenuItem("El Desecho/Preparar criatura")]
    public static void Prepare() {
        if (UseTripo && AssetDatabase.LoadAssetAtPath<GameObject>(TripoModel) != null) { PrepareTripo(); return; }
        PrepareOld();
    }

    static void PrepareTripo() {
        var importer = (ModelImporter)AssetImporter.GetAtPath(TripoModel);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.optimizeGameObjects = false;
        var takes = importer.defaultClipAnimations;
        foreach (var take in takes) {
            take.name = take.name.Split('|').Last();
            take.loopTime = Loops.Contains(take.name);
            take.lockRootRotation = take.lockRootHeightY = take.lockRootPositionXZ = true;
            take.keepOriginalPositionY = true;
        }
        importer.clipAnimations = takes;
        importer.SaveAndReimport();

        // Textura de piel: 2048 en PC, 1024 ASTC en telefono.
        string texPath = TripoFolder + "/Piel.png";
        var tex = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (tex != null) {
            tex.maxTextureSize = 2048;
            var android = tex.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = 1024; android.format = TextureImporterFormat.ASTC_6x6;
            tex.SetPlatformTextureSettings(android);
            tex.SaveAndReimport();
        }
        // Material humedo y brillante (carne podrida con baba).
        string matPath = TripoFolder + "/PielDeshecho.mat";
        var skin = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (skin == null) { skin = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(skin, matPath); }
        skin.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        skin.SetColor("_BaseColor", new Color(.85f, .9f, .82f));
        skin.SetFloat("_Smoothness", .72f);
        skin.SetFloat("_Metallic", .05f);
        EditorUtility.SetDirty(skin);

        // Controlador: copia del original con las mismas transiciones, pero con las animaciones nuevas.
        string controllerPath = TripoFolder + "/ElDeshechoTripo.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.CopyAsset(Folder + "/ElDeshecho.controller", controllerPath);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        var clips = AssetDatabase.LoadAllAssetsAtPath(TripoModel).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
        int swapped = 0;
        foreach (var layer in controller.layers) swapped += Remap(layer.stateMachine, clips);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        var root = new GameObject("El Deshecho");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TripoModel));
        model.transform.SetParent(root.transform, false);
        var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in renderers) {
            r.updateWhenOffscreen = true;
            r.sharedMaterials = Enumerable.Repeat(skin, r.sharedMesh.subMeshCount).ToArray();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        // Escala: la malla de Tripo mide ~1 m; se lleva a la altura del personaje y se apoya en el suelo.
        var all = renderers[0].bounds;
        foreach (var r in renderers) all.Encapsulate(r.bounds);
        float height = all.size.y;
        if (height > .01f) model.transform.localScale *= TripoHeight / height;
        var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        root.AddComponent<CreatureIdle>();
        int triangles = renderers.Sum(r => r.sharedMesh.triangles.Length / 3);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"ELDESECHO_CRIATURA_OK (Tripo): {triangles} triangulos, {clips.Count} animaciones, {swapped} estados cambiados, altura original {height:0.00} m");
    }

    static int Remap(AnimatorStateMachine machine, System.Collections.Generic.Dictionary<string, AnimationClip> clips) {
        int n = 0;
        foreach (var child in machine.states) {
            var state = child.state;
            if (state.motion is AnimationClip clip && clips.TryGetValue(clip.name, out var fresh)) { state.motion = fresh; n++; }
            else if (state.motion is BlendTree tree) {
                var children = tree.children;
                for (int i = 0; i < children.Length; i++)
                    if (children[i].motion is AnimationClip c && clips.TryGetValue(c.name, out var f)) { children[i].motion = f; n++; }
                tree.children = children;
            }
        }
        foreach (var sub in machine.stateMachines) n += Remap(sub.stateMachine, clips);
        return n;
    }

    static void PrepareOld() {
        var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
        if (importer == null) throw new Exception("Falta " + Model);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importBlendShapes = true;
        importer.importCameras = false;
        importer.importLights = false;
        var takes = importer.defaultClipAnimations;
        foreach (var take in takes) {
            take.name = take.name.Split('|').Last();
            take.loopTime = Loops.Contains(take.name);
            take.lockRootRotation = take.lockRootHeightY = take.lockRootPositionXZ = true;
        }
        importer.clipAnimations = takes;
        importer.SaveAndReimport();

        var skin = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/Skin.mat");
        var slime = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/Slime.mat");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/ElDeshecho.controller");
        if (skin == null || slime == null || controller == null) throw new Exception("Faltan materiales o el controlador de animacion de la criatura");
        if (controller.animationClips.Any(c => c == null) || controller.animationClips.Length == 0) throw new Exception("El controlador de animacion perdio sus clips");

        var root = new GameObject("El Deshecho");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
        model.transform.SetParent(root.transform, false);
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            r.updateWhenOffscreen = true;
            r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.Contains("Slime") ? slime : skin).ToArray();
        }
        var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        root.AddComponent<CreatureIdle>();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        Debug.Log($"ELDESECHO_CRIATURA_OK: {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangulos, {mesh.blendShapeCount} deformaciones, {controller.animationClips.Length} animaciones");
    }
}
