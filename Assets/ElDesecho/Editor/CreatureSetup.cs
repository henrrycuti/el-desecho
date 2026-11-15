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

    [MenuItem("El Desecho/Preparar criatura")]
    public static void Prepare() {
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
