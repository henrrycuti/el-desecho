using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Texturas PBR reales (CC0 de ambientCG, ver Assets/ElDesecho/Texturas/CREDITOS.txt).
// Las cajas texturizadas reciben UV a escala del mundo para que los azulejos no se estiren.
public static partial class ElDesechoBuilder {
    // material -> (carpeta de textura, metros por repeticion, tinte)
    static readonly Dictionary<string, (string tex, float tile, Color tint)> Pbr = new() {
        ["Azulejo"] = ("Tiles040", 2, new Color(1, 1, 1)),
        ["AzulejoGris"] = ("Tiles040", 2, new Color(.78f, .8f, .83f)),
        ["PanelBlanco"] = ("Tiles141", 2.4f, new Color(.95f, .97f, 1)),
        ["TechoLab"] = ("Concrete034", 3, new Color(.85f, .86f, .88f)),
        ["Mesa"] = ("Metal032", 1.5f, new Color(.8f, .82f, .85f)),
        ["Metal"] = ("Metal032", 1.5f, new Color(.45f, .47f, .5f)),
        ["Puerta"] = ("Metal032", 2, new Color(.6f, .62f, .65f)),
        ["Casillero"] = ("Metal032", 1.5f, new Color(.5f, .62f, .58f)),
        ["Oxido"] = ("Metal064", 2, new Color(.8f, .75f, .7f)),
    };

    static readonly Dictionary<(Vector3, float), Mesh> boxMeshes = new();

    static void MakePbrMaterials() {
        decalMaterials.Clear();
        RendererFeatures();
        foreach (var (name, (tex, _, tint)) in Pbr) {
            var m = M(name);
            var color = Texture($"{Root}/Texturas/{tex}/{tex}_1K-JPG_Color.jpg", false);
            var normal = Texture($"{Root}/Texturas/{tex}/{tex}_1K-JPG_NormalGL.jpg", true);
            m.SetTexture("_BaseMap", color);
            m.SetColor("_BaseColor", tint);
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1);
            m.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(m);
        }
    }

    static Texture2D Texture(string path, bool normal) {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new System.Exception("Falta la textura " + path);
        bool changed = false;
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != type) { importer.textureType = type; changed = true; }
        if (importer.maxTextureSize != 1024) { importer.maxTextureSize = 1024; changed = true; }
        if (importer.anisoLevel != 4) { importer.anisoLevel = 4; changed = true; }
        var android = importer.GetPlatformTextureSettings("Android");
        if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6) {
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Cambia la malla del cubo por una con UV en metros (cada cara usa sus dos ejes).
    static void WorldUv(GameObject g, Vector3 size, string material) {
        if (!Pbr.TryGetValue(material, out var info)) return;
        var key = (size, info.tile);
        if (!boxMeshes.TryGetValue(key, out var mesh) || mesh == null) {
            var source = g.GetComponent<MeshFilter>().sharedMesh;
            mesh = Object.Instantiate(source);
            mesh.name = "Caja UV";
            var v = mesh.vertices;
            var n = mesh.normals;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++) {
                var p = Vector3.Scale(v[i], size) / info.tile;
                var a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                uv[i] = a.x > .5f ? new Vector2(p.z, p.y) : a.y > .5f ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
            }
            mesh.uv = uv;
            mesh.RecalculateTangents();
            boxMeshes[key] = mesh;
        }
        g.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // ---------- Efectos del renderer: SSAO ligero y calcomanias (decals) ----------
    static void RendererFeatures() {
        var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Mobile_Renderer.asset");
        if (data == null) throw new System.Exception("Falta Assets/Settings/Mobile_Renderer.asset");
        AddFeature<DecalRendererFeature>(data, "Calcomanias", null);
        AddFeature<ScreenSpaceAmbientOcclusion>(data, "Oclusion ambiental", so => {
            // Barato para telefono: media resolucion, solo profundidad, pocas muestras.
            so.FindProperty("m_Settings.Downsample").boolValue = true;
            so.FindProperty("m_Settings.Source").enumValueIndex = 0;
            so.FindProperty("m_Settings.Intensity").floatValue = 1.6f;
            so.FindProperty("m_Settings.Radius").floatValue = .3f;
            so.FindProperty("m_Settings.Samples").enumValueIndex = 2;
        });
    }

    static void AddFeature<T>(ScriptableRendererData data, string name, System.Action<SerializedObject> setup) where T : ScriptableRendererFeature {
        if (data.rendererFeatures.Exists(f => f is T)) return;
        var feature = ScriptableObject.CreateInstance<T>();
        feature.name = name;
        AssetDatabase.AddObjectToAsset(feature, data);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
        if (setup != null) { var fso = new SerializedObject(feature); setup(fso); fso.ApplyModifiedPropertiesWithoutUndo(); }
        var so = new SerializedObject(data);
        var list = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    static readonly Dictionary<string, Material> decalMaterials = new();

    // Texturas de calcomania generadas por codigo: sangre, baba verde y aranazos.
    static Material DecalMaterial(string kind) {
        if (decalMaterials.TryGetValue(kind, out var cached) && cached != null) return cached;
        string folder = $"{Root}/Texturas/Calcomanias";
        System.IO.Directory.CreateDirectory(folder);
        string png = $"{folder}/{kind}.png";
        if (!System.IO.File.Exists(png)) {
            System.IO.File.WriteAllBytes(png, Splat(kind).EncodeToPNG());
            AssetDatabase.ImportAsset(png);
            var importer = (TextureImporter)AssetImporter.GetAtPath(png);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = 512; android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
        var m = Save("Calcomania" + kind, new Material(Shader.Find("Shader Graphs/Decal")));
        m.SetTexture("Base_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(png));
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return decalMaterials[kind] = m;
    }

    static Texture2D Splat(string kind) {
        const int n = 256;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var rnd = new System.Random(kind.Length * 7919 + kind[0]);
        Color tint = kind switch { "Sangre" => new Color(.28f, .02f, .02f), "Baba" => new Color(.25f, .55f, .15f), _ => new Color(.08f, .07f, .06f) };
        var blobs = new List<Vector3>();
        if (kind == "Aranazo") {
            for (int c = 0; c < 4; c++) for (int i = 0; i < 60; i++) {
                float f = i / 59f;
                blobs.Add(new Vector3(.3f + c * .12f + f * .12f, .12f + f * .76f, .012f + Mathf.Sin(f * Mathf.PI) * .01f));
            }
        } else {
            blobs.Add(new Vector3(.5f, .5f, .26f));
            for (int i = 0; i < 14; i++) {
                float a = (float)rnd.NextDouble() * 6.283f, d = .1f + (float)rnd.NextDouble() * .3f;
                blobs.Add(new Vector3(.5f + Mathf.Cos(a) * d, .5f + Mathf.Sin(a) * d, .02f + (float)rnd.NextDouble() * .09f));
            }
        }
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++) {
                float u = x / (float)n, v = y / (float)n, field = 0;
                foreach (var b in blobs) { float dx = u - b.x, dy = v - b.y; field += b.z * b.z / (dx * dx + dy * dy + 1e-5f); }
                float noise = Mathf.PerlinNoise(u * 9 + kind.Length, v * 9);
                float alpha = Mathf.Clamp01((field - .9f + noise * .5f) * 2.5f);
                float edge = Mathf.Clamp01((1 - Mathf.Max(Mathf.Abs(u - .5f), Mathf.Abs(v - .5f)) * 2) / .15f);
                float k = .7f + noise * .5f;
                t.SetPixel(x, y, new Color(tint.r * k, tint.g * k, tint.b * k, alpha * .9f * edge));
            }
        t.Apply();
        return t;
    }

    // Calcomania sobre el suelo (mira hacia abajo) o sobre una pared (mira en la direccion dada).
    static DecalProjector Decal(string kind, Transform parent, Vector3 position, Vector2 size, float spin, Vector3? wallFacing = null) {
        var g = new GameObject("Calcomania " + kind);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = wallFacing is Vector3 f ? Quaternion.LookRotation(f) * Quaternion.Euler(0, 0, spin) : Quaternion.Euler(90, spin, 0);
        var d = g.AddComponent<DecalProjector>();
        d.material = DecalMaterial(kind);
        d.size = new Vector3(size.x, size.y, .6f);
        d.pivot = new Vector3(0, 0, .3f);
        d.fadeFactor = 1;
        return d;
    }
}
