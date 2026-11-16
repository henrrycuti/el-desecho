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
        // Alcantarilla: ladrillo viejo mojado, concreto rugoso.
        ["Muro"] = ("Bricks097", 2.5f, new Color(.42f, .47f, .4f)),
        ["Concreto"] = ("Concrete012", 3, new Color(.4f, .43f, .4f)),
        ["Suelo"] = ("Concrete012", 2.5f, new Color(.22f, .24f, .21f)),
        ["CementoSeco"] = ("Concrete012", 3, new Color(.72f, .68f, .6f)),
        ["Polvo"] = ("Concrete012", 2.5f, new Color(.6f, .55f, .47f)),
        // Calle.
        ["Asfalto"] = ("Asphalt033", 4, new Color(.32f, .33f, .35f)),
        ["Acera"] = ("PavingStones150", 2, new Color(.5f, .5f, .5f)),
        ["Edificio"] = ("Bricks094", 3, new Color(.38f, .36f, .38f)),
        ["Ladrillo"] = ("Bricks094", 2, new Color(.7f, .6f, .55f)),
    };

    // Agua: misma textura de ondas generada por codigo, muy brillante para reflejar.
    static readonly Dictionary<string, float> Waters = new() { ["Agua"] = 3, ["AguaProfunda"] = 4 };

    static void MakeWaterMaterials() {
        string path = $"{Root}/Texturas/Agua/Ondas.png";
        System.IO.Directory.CreateDirectory($"{Root}/Texturas/Agua");
        System.IO.File.WriteAllBytes(path, Ripples().EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var normal = Texture(path, true);
        foreach (var name in Waters.Keys) {
            if (!materials.ContainsKey(name)) continue;
            var m = M(name);
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", .35f);
            m.SetFloat("_Smoothness", .96f);
            m.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(m);
        }
    }

    // Mapa de normales que se repite sin costura: suma de ondas con frecuencias enteras.
    static Texture2D Ripples() {
        const int n = 256;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var waves = new (int a, int b, float phase, float amp)[] { (3, 1, 0, 1), (-2, 4, 1.3f, .8f), (5, -3, 2.1f, .5f), (1, 7, .7f, .35f), (-7, -2, 4, .3f), (9, 5, 2.8f, .2f) };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++) {
                float u = x / (float)n, v = y / (float)n, dx = 0, dy = 0;
                foreach (var w in waves) {
                    float c = Mathf.Cos(2 * Mathf.PI * (w.a * u + w.b * v) + w.phase) * w.amp;
                    dx += c * w.a; dy += c * w.b;
                }
                var normal = new Vector3(-dx * .03f, -dy * .03f, 1).normalized;
                t.SetPixel(x, y, new Color(normal.x * .5f + .5f, normal.y * .5f + .5f, normal.z * .5f + .5f, 1));
            }
        t.Apply();
        return t;
    }

    static readonly Dictionary<(Vector3, float), Mesh> boxMeshes = new();

    static void MakePbrMaterials() {
        decalMaterials.Clear();
        RendererFeatures();
        MakeWaterMaterials();
        foreach (var (name, (tex, _, tint)) in Pbr) {
            if (!materials.ContainsKey(name)) continue;
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
        float tile;
        if (Pbr.TryGetValue(material, out var info)) tile = info.tile;
        else if (!Waters.TryGetValue(material, out tile)) return;
        WorldUv(g, size, tile);
    }

    static void WorldUv(GameObject g, Vector3 size, float tile) {
        var info = (tile: tile, x: 0);
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

    // ---------- Modelos de Blender (Tools/Blender/modelos_props.py) ----------
    // Coloca un modelo FBX y le pone los materiales PBR del juego segun el nombre del material.
    static GameObject Prop(string model, Transform parent, Vector3 position, float yaw, bool solid = true) {
        string path = $"{Root}/Modelos/{model}.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new System.Exception("Falta el modelo " + path);
        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard || importer.isReadable || !importer.bakeAxisConversion) {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.bakeAxisConversion = true;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        g.name = model;
        g.transform.localPosition = position;
        g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var bounds = new Bounds(g.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in g.GetComponentsInChildren<Renderer>()) {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) {
                string name = mats[i] != null ? mats[i].name.Split('.')[0] : "";
                if (materials.TryGetValue(name, out var real)) mats[i] = real;
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
        if (solid) {
            var box = g.AddComponent<BoxCollider>();
            box.center = g.transform.InverseTransformPoint(bounds.center);
            var size = g.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
        return g;
    }

    // Utileria de alcantarilla en una celda (solo pegada a muros, nunca en el centro del paso).
    static void SewerProps(Transform cell, int open, float height) {
        int hash = Mathf.Abs((int)(cell.localPosition.x * 11 + cell.localPosition.z * 17)) % 10;
        for (int side = 0; side < 4; side++) {
            if ((open & (1 << side)) != 0) continue;
            var dir = Direction(side);
            var across = new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x));
            float yaw = Quaternion.LookRotation(-dir).eulerAngles.y;
            float inner = Cell / 2 - Wall;
            bool straight = open == (1 | 4) || open == (2 | 8);
            if (hash == 1 && straight) {
                Prop("Barril", cell, dir * (inner - .35f) + across * 1.4f, hash * 37, true);
                Prop("Barril", cell, dir * (inner - .35f) + across * .75f, hash * 53, true);
            } else if (hash == 4 || hash == 7) {
                // Tuberia con codo y valvula a media altura (por encima de la cabeza no estorba: va a 2,3 m).
                Prop("Tuberia_Codo", cell, dir * (inner - .25f) + Vector3.up * 2.3f - across * .6f, yaw, false);
            } else if (hash == 6) {
                var grate = Prop("Rejilla", cell, dir * (inner - .02f) + Vector3.up * (height - 1.1f), yaw, false);
                grate.transform.localRotation = Quaternion.LookRotation(Vector3.up, -dir);
            }
            break; // solo el primer muro cerrado
        }
    }

    // Mobiliario de laboratorio contra el primer muro cerrado de la sala.
    static void LabProps(Transform cell, int open, int k, float cellSize, float wall) {
        for (int side = 0; side < 4; side++) {
            if ((open & (1 << side)) != 0) continue;
            var dir = Direction(side);
            float yaw = Quaternion.LookRotation(-dir).eulerAngles.y, inner = cellSize / 2 - wall;
            switch (k % 3) {
                case 0: Prop("Estante_Frascos", cell, dir * (inner - .25f), yaw); break;
                case 1: Prop("Camilla", cell, dir * (inner - .42f), yaw + 90); break;
                default: Prop("Carro_Instrumental", cell, dir * (inner - .3f), yaw + 15); break;
            }
            return;
        }
    }

    // Cilindros (tuberias, barrotes): la textura rodea el tubo y se repite a lo largo, sin estirarse.
    static void CylinderUv(GameObject g, float radius, float length, string material) {
        if (!Pbr.TryGetValue(material, out var info)) return;
        var key = (new Vector3(radius, length, -1), info.tile);
        if (!boxMeshes.TryGetValue(key, out var mesh) || mesh == null) {
            mesh = Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = "Cilindro UV";
            var uv = mesh.uv;
            var n = mesh.normals;
            float around = Mathf.Max(.25f, 2 * Mathf.PI * radius / info.tile), along = length / info.tile, cap = 2 * radius / info.tile;
            for (int i = 0; i < uv.Length; i++)
                uv[i] = Mathf.Abs(n[i].y) > .5f ? uv[i] * cap : new Vector2(uv[i].x * around, uv[i].y * along);
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
        {
            // Se regenera siempre: la textura sale del codigo de Splat.
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
        var rnd = new System.Random(kind == "Sangre" ? 11 : kind == "Baba" ? 23 : 37);
        Color tint = kind switch {
            "Sangre" => new Color(.42f, .02f, .02f), "Baba" => new Color(.3f, .62f, .16f),
            "Musgo" => new Color(.12f, .2f, .06f), "Escurrido" => new Color(.34f, .14f, .05f), _ => new Color(.1f, .06f, .05f),
        };
        // Gotas satelite (x, y, radio) y chorros que salen del centro (angulo, largo).
        var drops = new List<Vector3>();
        for (int i = 0; i < 22; i++) {
            float a = (float)rnd.NextDouble() * 6.283f, d = .22f + (float)rnd.NextDouble() * .24f;
            drops.Add(new Vector3(.5f + Mathf.Cos(a) * d, .5f + Mathf.Sin(a) * d, .006f + (float)rnd.NextDouble() * .022f));
        }
        var streaks = new List<Vector2>();
        for (int i = 0; i < 7; i++) streaks.Add(new Vector2((float)rnd.NextDouble() * 6.283f, .3f + (float)rnd.NextDouble() * .16f));
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++) {
                float u = x / (float)n, v = y / (float)n, alpha;
                float noise = Mathf.PerlinNoise(u * 7 + kind.Length * 3, v * 7);
                if (kind == "Escurrido") {
                    // Chorretes de oxido que bajan desde arriba y se desvanecen.
                    alpha = 0;
                    for (int s = 0; s < 9; s++) {
                        float cx = .12f + s * .095f + Mathf.Sin(s * 12.9f) * .03f, len = .35f + Mathf.Abs(Mathf.Sin(s * 7.3f)) * .5f;
                        float w = .012f + Mathf.Abs(Mathf.Sin(s * 3.1f)) * .025f;
                        float down = (.95f - v) / len;
                        if (down < 0 || down > 1) continue;
                        float wobble = Mathf.Sin(v * 40 + s) * .004f;
                        alpha = Mathf.Max(alpha, Mathf.Clamp01((w * (1 - down * .6f) - Mathf.Abs(u - cx + wobble)) / .006f) * (1 - down));
                    }
                    alpha = Mathf.Max(alpha, Mathf.Clamp01((v - .88f) / .05f) * .8f) * (.6f + noise * .4f);
                } else if (kind == "Aranazo") {
                    // Cuatro surcos finos de garra, algo curvos, mas anchos en el centro.
                    alpha = 0;
                    for (int c = 0; c < 4; c++) {
                        float f = Mathf.Clamp01((v - .1f) / .8f);
                        float cx = .28f + c * .13f + f * .14f + Mathf.Sin(f * 3.1f) * .03f;
                        float w = (.006f + Mathf.Sin(f * Mathf.PI) * .014f) * (v > .1f && v < .9f ? 1 : 0);
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(1 - Mathf.Abs(u - cx) / Mathf.Max(w, 1e-4f)));
                    }
                    alpha = Mathf.Clamp01(alpha * 1.6f) * (.75f + noise * .25f);
                } else {
                    float dx = u - .5f, dy = v - .5f, r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float radius = .17f + Mathf.PerlinNoise(Mathf.Cos(ang) * 2 + 5, Mathf.Sin(ang) * 2 + kind.Length) * .1f;
                    alpha = Mathf.Clamp01((radius - r) / .012f);
                    foreach (var s in streaks) {
                        float da = Mathf.DeltaAngle(ang * Mathf.Rad2Deg, s.x * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        float width = .045f * (1 - r / s.y);
                        if (r < s.y) alpha = Mathf.Max(alpha, Mathf.Clamp01((width - Mathf.Abs(da) * r) / .006f));
                    }
                    foreach (var d in drops) {
                        float ex = u - d.x, ey = v - d.y;
                        alpha = Mathf.Max(alpha, Mathf.Clamp01((d.z - Mathf.Sqrt(ex * ex + ey * ey)) / .004f));
                    }
                }
                float edge = Mathf.Clamp01((1 - Mathf.Max(Mathf.Abs(u - .5f), Mathf.Abs(v - .5f)) * 2) / .06f);
                float k = .75f + noise * .45f;
                t.SetPixel(x, y, new Color(tint.r * k, tint.g * k, tint.b * k, alpha * .95f * edge));
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
