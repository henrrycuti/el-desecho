using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElDesecho;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Construye el prologo de El Desecho: materiales, kit modular de alcantarilla, escenas y ejecutable.
// Menu El Desecho > Construir prologo (o -executeMethod ElDesechoBuilder.BuildPrologue).
// Las escenas se pueden editar despues a mano en Unity; volver a construir las reemplaza.
public static partial class ElDesechoBuilder {
    const string Root = "Assets/ElDesecho";
    const string Scenes = Root + "/Scenes";
    const string Mats = Root + "/Materials";
    const string Kit = Root + "/Prefabs/Alcantarilla";
    const float Cell = 6, TunnelHeight = 3.6f, ChamberHeight = 7.2f, Wall = .6f;

    static readonly Dictionary<string, Material> materials = new();

    // ---------- Laberinto de la alcantarilla ----------
    // # roca, . tunel, C camara grande, S escalera de llegada, L puerta del laboratorio.
    static readonly string[] SewerMap = {
        "###############",
        "#S....#.......#",
        "#.###.#.#####.#",
        "#.#...#.#...#.#",
        "#.#.#####.#.#.#",
        "#.#.##CCC.#...#",
        "#.#...CCC...###",
        "#.###.CCC.#...#",
        "#...#.###.#.#.#",
        "###.....#...#L#",
        "###############",
    };

    [MenuItem("El Desecho/Construir prologo")]
    public static void BuildPrologue() {
        ConfigureProject();
        MakeMaterials();
        MakeChapterMaterials();
        MakeLateMaterials();
        MakePbrMaterials();
        MakeKitPrefabs();
        BuildMenu();
        BuildStreet();
        BuildSewer();
        BuildLab();
        BuildThreePaths();
        BuildInvestigation();
        BuildExitDoor();
        BuildDawn();
        BuildEpilogue();
        EditorBuildSettings.scenes = new[] { "Menu", "P0_Calle", "P1_Alcantarilla", "C1_Laboratorio", "C2_TresCaminos", "C3_Investigacion", "C4_PuertaSalida", "C5_LuzDelDia", "E_Epilogo" }
            .Select(n => new EditorBuildSettingsScene($"{Scenes}/{n}.unity", true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ELDESECHO_PROLOGO_CONSTRUIDO");
    }

    public static void BuildWindows() {
        BuildPrologue();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/ElDesecho.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build fallido: " + report.summary.result);
        Debug.Log("ELDESECHO_BUILD_OK");
    }

    static void ConfigureProject() {
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (pipeline == null) throw new Exception("Falta Assets/Settings/Mobile_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        for (int i = 0; i < QualitySettings.names.Length; i++) {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        PlayerSettings.productName = "El Desecho";
        PlayerSettings.companyName = "Equipo Tecsup";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.equipotecsup.eldesecho");
        Directory.CreateDirectory(Scenes);
        Directory.CreateDirectory(Mats);
        Directory.CreateDirectory(Kit);
    }

    // ---------- Materiales ----------
    static void MakeMaterials() {
        materials.Clear();
        Lit("Asfalto", new Color(.06f, .065f, .07f), .82f);
        Lit("Acera", new Color(.22f, .22f, .21f), .55f);
        Lit("Edificio", new Color(.16f, .15f, .16f), .25f);
        Lit("Ladrillo", new Color(.24f, .13f, .09f), .35f);
        Lit("Ventana", new Color(.02f, .025f, .03f), .95f);
        Lit("Metal", new Color(.2f, .21f, .22f), .6f, .8f);
        Lit("Oxido", new Color(.3f, .14f, .06f), .3f, .4f);
        Lit("Coche", new Color(.12f, .14f, .2f), .8f, .3f);
        Lit("Concreto", new Color(.2f, .22f, .2f), .45f);
        Lit("Muro", new Color(.16f, .18f, .15f), .55f);
        Lit("Suelo", new Color(.08f, .09f, .07f), .7f);
        Lit("Mugre", new Color(.03f, .035f, .025f), .3f);
        Lit("Agua", new Color(.05f, .1f, .08f), .97f);
        Lit("Puerta", new Color(.3f, .32f, .34f), .5f, .7f);
        Lit("Barandilla", new Color(.35f, .3f, .1f), .5f, .6f);
        Emissive("LuzCalle", new Color(1, .65f, .3f), 3);
        Emissive("VentanaEncendida", new Color(1, .7f, .4f), .6f);
        Emissive("LuzVerde", new Color(.45f, 1, .55f), 2.5f);
        Emissive("LuzRoja", new Color(1, .1f, .08f), 3);
        Emissive("LetreroRojo", new Color(.9f, .08f, .05f), 1.5f);
        var rain = Save("Lluvia", new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")));
        rain.SetFloat("_Surface", 1);
        rain.SetFloat("_Blend", 0);
        rain.SetColor("_BaseColor", new Color(.7f, .75f, .85f, .35f));
        rain.renderQueue = (int)RenderQueue.Transparent;
        rain.SetOverrideTag("RenderType", "Transparent");
        rain.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rain.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        rain.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        rain.SetInt("_ZWrite", 0);
        EditorUtility.SetDirty(rain);
    }

    static Material TransparentLit(string name, Color color, float smooth) {
        var m = Lit(name, color, smooth);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Lit(string name, Color color, float smooth, float metal = 0) {
        var m = Save(name, new Material(Shader.Find("Universal Render Pipeline/Lit")));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Emissive(string name, Color color, float strength) {
        var m = Lit(name, color * .3f, .5f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * strength);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Save(string name, Material fresh) {
        string path = $"{Mats}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) {
            existing.shader = fresh.shader;
            Object.DestroyImmediate(fresh);
            materials[name] = existing;
            return existing;
        }
        AssetDatabase.CreateAsset(fresh, path);
        materials[name] = fresh;
        return fresh;
    }

    static Material M(string name) => materials[name];

    // ---------- Piezas basicas ----------
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool solid = true) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localScale = size;
        WorldUv(g, size, material);
        Finish(g, material, solid);
        return g;
    }

    static GameObject Cylinder(string name, Transform parent, Vector3 position, float radius, float length, Vector3 euler, string material, bool solid = false) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.transform.localScale = new Vector3(radius * 2, length / 2, radius * 2);
        Finish(g, material, solid);
        return g;
    }

    static void Finish(GameObject g, string material, bool solid) {
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = M(material);
        r.shadowCastingMode = ShadowCastingMode.Off;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }

    static Transform Group(string name, Transform parent = null) {
        var t = new GameObject(name).transform;
        if (parent != null) t.SetParent(parent, false);
        return t;
    }

    static Light PointLight(string name, Transform parent, Vector3 position, Color color, float range, float intensity, bool flicker = false) {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.localPosition = position;
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        if (flicker) light.gameObject.AddComponent<FlickerLight>();
        return light;
    }

    static StoryTrigger Trigger(string eventId, Transform parent, Vector3 position, Vector3 size) {
        var g = new GameObject("Disparador " + eventId);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        var box = g.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var trigger = g.AddComponent<StoryTrigger>();
        trigger.eventId = eventId;
        return trigger;
    }

    static Interactable Usable(GameObject g, string prompt, string eventId) {
        var item = g.AddComponent<Interactable>();
        item.prompt = prompt;
        item.eventId = eventId;
        return item;
    }

    static SpawnPoint Spawn(string id, Transform parent, Vector3 position, float yaw) {
        var g = new GameObject("Aparicion " + id);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var s = g.AddComponent<SpawnPoint>();
        s.id = id;
        return s;
    }

    static Transform Point(string name, Transform parent, Vector3 position) {
        var t = Group(name, parent);
        t.localPosition = position;
        return t;
    }

    static void TextSign(string text, Transform parent, Vector3 position, float yaw, Color color, float size = .12f) {
        var g = new GameObject("Letrero " + text);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var mesh = g.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.font = font;
        g.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        mesh.text = text;
        mesh.color = color;
        mesh.characterSize = size;
        mesh.fontSize = 64;
        mesh.anchor = TextAnchor.MiddleCenter;
    }


    // ---------- Postproceso (URP) ----------
    static VolumeProfile Profile(string name, Color filter, float contrast, float saturation, float bloom, float vignette, float grain, float aberration) {
        string path = $"{Root}/Postproceso/{name}.asset";
        Directory.CreateDirectory($"{Root}/Postproceso");
        AssetDatabase.DeleteAsset(path);
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, path);
        var b = p.Add<Bloom>(true);
        b.intensity.Override(bloom); b.threshold.Override(.85f); b.scatter.Override(.7f);
        var v = p.Add<Vignette>(true);
        v.intensity.Override(vignette); v.smoothness.Override(.45f);
        var c = p.Add<ColorAdjustments>(true);
        c.colorFilter.Override(filter); c.contrast.Override(contrast); c.saturation.Override(saturation);
        if (grain > 0) { var g = p.Add<FilmGrain>(true); g.intensity.Override(grain); g.type.Override(FilmGrainLookup.Medium3); }
        if (aberration > 0) p.Add<ChromaticAberration>(true).intensity.Override(aberration);
        foreach (var comp in p.components) AssetDatabase.AddObjectToAsset(comp, p);
        EditorUtility.SetDirty(p);
        return p;
    }

    static Volume PostFx(Transform parent, VolumeProfile profile, float weight = 1, int priority = 0) {
        var g = new GameObject("Postproceso " + profile.name);
        g.transform.SetParent(parent, false);
        var v = g.AddComponent<Volume>();
        v.isGlobal = true;
        v.sharedProfile = profile;
        v.weight = weight;
        v.priority = priority;
        return v;
    }

    // Tono de cada escena: calle azul de noche, alcantarilla verdosa, laboratorio frio y limpio.
    static VolumeProfile SceneLook(string scene) => scene switch {
        "P0_Calle" => Profile("Calle", new Color(.85f, .9f, 1.05f), 12, -5, .9f, .32f, .2f, 0),
        "C1_Laboratorio" => Profile("Laboratorio", new Color(.8f, .87f, .95f), 18, -10, .3f, .3f, .08f, 0),
        "C5_LuzDelDia" => Profile("Persecucion", new Color(1.05f, .85f, .82f), 22, 5, .8f, .4f, .3f, .15f),
        _ => Profile("Alcantarilla " + scene, new Color(.88f, 1, .9f), 15, -15, .7f, .38f, .25f, 0),
    };

    static Volume AlarmLook(Transform parent) => PostFx(parent, Profile("Alarma", new Color(1.3f, .92f, .88f), 18, 15, 1f, .4f, .38f, .22f), 0, 5);

    // ---------- Jugador y sistemas de escena ----------
    static FirstPersonController Player(Transform parent, bool flashlightOn, Color background) {
        var root = new GameObject("Jugador");
        root.transform.SetParent(parent, false);
        var body = root.AddComponent<CharacterController>();
        body.height = 1.75f;
        body.radius = .32f;
        body.center = Vector3.up * .875f;
        body.stepOffset = .35f;
        body.slopeLimit = 50;
        body.skinWidth = .03f;
        var head = new GameObject("Cabeza");
        head.tag = "MainCamera";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = Vector3.up * 1.63f;
        var camera = head.AddComponent<Camera>();
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 140;
        camera.fieldOfView = 68;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        head.AddComponent<AudioListener>();
        var lamp = new GameObject("Linterna").AddComponent<Light>();
        lamp.transform.SetParent(head.transform, false);
        lamp.transform.localPosition = new Vector3(.18f, -.12f, .1f);
        lamp.type = LightType.Spot;
        lamp.range = 18;
        lamp.spotAngle = 58;
        lamp.innerSpotAngle = 26;
        lamp.intensity = 6;
        lamp.color = new Color(1, .95f, .85f);
        lamp.shadows = LightShadows.None;
        lamp.enabled = flashlightOn;
        var controller = root.AddComponent<FirstPersonController>();
        controller.head = head.transform;
        controller.flashlight = lamp;
        return controller;
    }

    static ObjectiveManager Systems(Transform parent) {
        var g = new GameObject("Historia y pantalla");
        g.transform.SetParent(parent, false);
        g.AddComponent<Hud>();
        g.AddComponent<ThreatMusic>();
        PostFx(parent, SceneLook(parent.root.name));
        return g.AddComponent<ObjectiveManager>();
    }

    static ObjectiveStep Step(string objective, string completeOn, params string[] lines) =>
        new() { objective = objective, completeOn = completeOn, lines = lines };

    static void Atmosphere(Color ambient, Color fog, float density) {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = density;
        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = .4f;
    }

    static ParticleSystem Particles(string name, Transform parent, Vector3 box, float rate, float speed, float size, float life, string material) {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.maxParticles = Mathf.CeilToInt(rate * life * 1.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        shape.rotation = new Vector3(90, 0, 0);
        var renderer = g.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = M(material);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = .04f;
        renderer.lengthScale = 1;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static void NewScene(string name, Action<Transform> build) {
        boxMeshes.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = Group(name);
        build(root);
        EditorSceneManager.SaveScene(scene, $"{Scenes}/{name}.unity");
    }

    // ---------- Menu ----------
    static void BuildMenu() {
        NewScene("Menu", root => {
            Atmosphere(new Color(.02f, .025f, .03f), new Color(.01f, .012f, .015f), .08f);
            var cam = new GameObject("Camara del menu").AddComponent<Camera>();
            cam.transform.SetParent(root, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .012f, .015f);
            cam.transform.localPosition = new Vector3(0, 1.6f, -6);
            cam.tag = "MainCamera";
            var rain = Particles("Lluvia", root, new Vector3(14, 8, 1), 700, 14, .02f, 1, "Lluvia");
            rain.transform.localPosition = new Vector3(0, 7, 0);
            new GameObject("Menu principal").AddComponent<MainMenu>().transform.SetParent(root, false);
        });
    }

    // ---------- Escena 1: la calle ----------
    static void BuildStreet() {
        NewScene("P0_Calle", root => {
            Atmosphere(new Color(.07f, .08f, .11f), new Color(.03f, .035f, .05f), .035f);
            var city = Group("Ciudad", root);
            Box("Calzada", city, new Vector3(0, -.1f, 0), new Vector3(90, .2f, 8), "Asfalto");
            Box("Acera norte", city, new Vector3(0, .05f, 5.5f), new Vector3(90, .3f, 3), "Acera");
            Box("Acera sur", city, new Vector3(0, .05f, -5.5f), new Vector3(90, .3f, 3), "Acera");
            Box("Suelo del callejon", city, new Vector3(10, .05f, 16), new Vector3(4, .3f, 18), "Acera");
            Box("Muro final de la calle oeste", city, new Vector3(-45, 4, 0), new Vector3(1, 8, 26), "Edificio");
            Box("Muro final de la calle este", city, new Vector3(45, 4, 0), new Vector3(1, 8, 26), "Edificio");

            // Manzanas: al norte se abre el callejon entre x=8 y x=12.
            Building(city, "Edificio norte oeste", new Vector3(-18.5f, 0, 14), new Vector3(53, 14, 14));
            Building(city, "Edificio norte este", new Vector3(28.5f, 0, 14), new Vector3(33, 11, 14));
            Building(city, "Edificio sur", new Vector3(0, 0, -14), new Vector3(90, 12, 14));
            Box("Fondo del callejon", city, new Vector3(10, 4, 25.5f), new Vector3(4, 8, 1), "Ladrillo");

            for (int i = -3; i <= 3; i++) {
                StreetLamp(city, new Vector3(i * 13 + 4, 0, 6.6f), 180);
                StreetLamp(city, new Vector3(i * 13 - 2, 0, -6.6f), 0);
            }
            Box("Coche aparcado", city, new Vector3(-16, .7f, -2.6f), new Vector3(4.2f, 1.4f, 1.8f), "Coche");
            Box("Coche aparcado", city, new Vector3(22, .7f, 2.6f), new Vector3(4.2f, 1.4f, 1.8f), "Coche");
            Box("Contenedor", city, new Vector3(11.3f, .8f, 12), new Vector3(1.2f, 1.3f, 2.2f), "Oxido");
            for (int i = 0; i < 6; i++) Box("Charco", city, new Vector3(-30 + i * 11, .01f, i % 2 == 0 ? 1.2f : -1.8f), new Vector3(2.4f, .01f, 1.6f), "Agua", false);

            // Entrada de mantenimiento al fondo del callejon: el mismo letrero rojo de la fotografia.
            var service = Group("Entrada de mantenimiento", root);
            service.localPosition = new Vector3(10, .2f, 21);
            Box("Marco de la tapa", service, Vector3.zero, new Vector3(1.6f, .06f, 1.6f), "Metal", false);
            var hole = Box("Hueco de la escalera", service, new Vector3(0, -.02f, 0), new Vector3(1.1f, .02f, 1.1f), "Mugre", false);
            var cover = Box("Tapa de mantenimiento", service, new Vector3(0, .06f, 0), new Vector3(1.2f, .08f, 1.2f), "Oxido");
            Usable(cover, "Abrir la tapa", "tapa");
            var mover = cover.AddComponent<EventMover>();
            mover.eventId = "tapa";
            mover.moveBy = new Vector3(1.3f, 0, .2f);
            mover.rotateBy = new Vector3(0, 35, 0);
            var ladder = Box("Escalera hacia abajo", service, new Vector3(0, .4f, 0), new Vector3(1, .8f, 1), "Metal", false);
            ladder.AddComponent<BoxCollider>().isTrigger = true;
            ladder.GetComponent<Renderer>().enabled = false;
            Usable(ladder, "Bajar por la escalera", "bajar");
            for (int i = 0; i < 4; i++) Cylinder("Valla", service, new Vector3(-.9f + (i % 2) * 1.8f, .5f, i < 2 ? -.9f : .9f), .03f, 1, Vector3.zero, "Barandilla");
            Cylinder("Barra de la valla", service, new Vector3(0, 1, .9f), .025f, 1.8f, new Vector3(0, 0, 90), "Barandilla");
            Box("Letrero", service, new Vector3(0, 2.4f, 3.9f), new Vector3(2.4f, .7f, .08f), "LetreroRojo", false);
            TextSign("SERVICIO", service, new Vector3(0, 2.4f, 3.84f), 0, Color.white, .09f);
            PointLight("Lampara de obra", service, new Vector3(1.2f, 2.6f, 3.2f), new Color(1, .35f, .2f), 9, 2.2f, true);

            Player(root, false, new Color(.03f, .035f, .05f));
            Spawn("inicio", root, new Vector3(-30, .2f, -5.5f), 90);
            var rain = Particles("Lluvia", root, new Vector3(26, 26, 1), 1400, 16, .02f, 1.1f, "Lluvia");
            rain.gameObject.AddComponent<FollowCamera>().offset = new Vector3(0, 9, 4);

            var story = Systems(root);
            var first = Step("Encuentra la entrada de mantenimiento de la fotografia", "callejon",
                "Un sobre sin remitente. Dentro, una direccion y una fotografia borrosa de una entrada de mantenimiento.",
                "La nota decia: \"Bajo la ciudad estan escondiendo algo. Si quieres saber la verdad, ve esta noche. No confies en nadie.\"",
                "En la foto se ve un letrero rojo al fondo de un callejon.");
            var second = Step("Abre la tapa de mantenimiento", "tapa", "Es aqui. El mismo letrero de la fotografia.");
            second.marker = cover.transform;
            var third = Step("Baja por la escalera", "bajar", "Debajo hay una escalera oxidada que se pierde en la oscuridad.");
            third.marker = hole.transform;
            third.loadScene = "P1_Alcantarilla";
            third.loadSpawn = "escalera";
            third.loadTitle = "Prologo: La mision";
            story.steps = new List<ObjectiveStep> { first, second, third };
            Trigger("callejon", root, new Vector3(10, 1.5f, 10), new Vector3(4, 3, 3));

            var route = new GameObject("Ruta de prueba").AddComponent<BotRoute>();
            route.transform.SetParent(root, false);
            route.points = new[] {
                Point("Ruta 1", route.transform, new Vector3(-6, 0, -5.5f)),
                Point("Ruta 2", route.transform, new Vector3(10, 0, 0)),
                Point("Ruta 3", route.transform, new Vector3(10, 0, 9)),
                Point("Ruta 4", route.transform, new Vector3(10, 0, 18)),
            };
        });
    }

    static void Building(Transform parent, string name, Vector3 foot, Vector3 size) {
        Box(name, parent, foot + Vector3.up * size.y / 2, size, "Edificio");
        // Ventanas oscuras en la fachada que da a la calle, algunas encendidas.
        float face = foot.z > 0 ? foot.z - size.z / 2 - .03f : foot.z + size.z / 2 + .03f;
        var rng = new System.Random(name.Length * 7919);
        for (float x = foot.x - size.x / 2 + 2; x < foot.x + size.x / 2 - 1; x += 3.2f)
            for (float y = 4; y < size.y - 1; y += 3.4f) {
                bool lit = rng.NextDouble() < .12;
                Box("Ventana", parent, new Vector3(x, y, face), new Vector3(1.4f, 1.8f, .04f), lit ? "VentanaEncendida" : "Ventana", false);
            }
    }

    static void StreetLamp(Transform parent, Vector3 foot, float yaw) {
        var lamp = Group("Farola", parent);
        lamp.localPosition = foot;
        lamp.localRotation = Quaternion.Euler(0, yaw, 0);
        Cylinder("Poste", lamp, new Vector3(0, 2.6f, 0), .07f, 5.2f, Vector3.zero, "Metal", true);
        Box("Brazo", lamp, new Vector3(0, 5.1f, .6f), new Vector3(.1f, .1f, 1.3f), "Metal", false);
        Box("Bombilla", lamp, new Vector3(0, 5, 1.2f), new Vector3(.45f, .12f, .3f), "LuzCalle", false);
        PointLight("Luz de farola", lamp, new Vector3(0, 4.7f, 1.2f), new Color(1, .62f, .3f), 11, 2.2f);
    }

    // ---------- Kit modular de alcantarilla ----------
    // Lados abiertos: 1 norte (+Z), 2 este (+X), 4 sur (-Z), 8 oeste (-X).
    static void MakeKitPrefabs() {
        var kit = new (string name, int mask, bool chamber)[] {
            ("Tunel_Recto", 1 | 4, false), ("Tunel_Curva", 1 | 2, false), ("Tunel_T", 1 | 2 | 4, false),
            ("Tunel_Cruce", 15, false), ("Tunel_Final", 1, false), ("Camara_Pieza", 15, true),
        };
        foreach (var (name, mask, chamber) in kit) {
            var g = Group(name);
            SewerCell(g, mask, chamber, chamber ? 15 : 0);
            PrefabUtility.SaveAsPrefabAsset(g.gameObject, $"{Kit}/{name}.prefab");
            Object.DestroyImmediate(g.gameObject);
        }
        SavePiece("Pasarela_Barandilla", t => Walkway(t, 6));
        SavePiece("Escalera_Mano", t => Ladder(t, 4.2f));
        SavePiece("Compuerta", Gate);
    }

    static void SavePiece(string name, Action<Transform> build) {
        var g = Group(name);
        build(g);
        PrefabUtility.SaveAsPrefabAsset(g.gameObject, $"{Kit}/{name}.prefab");
        Object.DestroyImmediate(g.gameObject);
    }

    // Una celda de 6 x 6 m. "open" dice que lados conectan con otra celda; "chamberSides" que lados
    // dan a otra celda de la misma camara (sin pilares ni dinteles).
    static void SewerCell(Transform cell, int open, bool chamber, int chamberSides) {
        float height = chamber ? ChamberHeight : TunnelHeight;
        Box("Suelo", cell, new Vector3(0, -.2f, 0), new Vector3(Cell, .4f, Cell), "Suelo");
        Box("Agua", cell, new Vector3(0, .12f, 0), new Vector3(Cell, .02f, Cell), "Agua", false);
        Box("Techo", cell, new Vector3(0, height + .2f, 0), new Vector3(Cell, .4f, Cell), "Concreto");
        for (int side = 0; side < 4; side++) {
            int bit = 1 << side;
            var dir = Direction(side);
            var across = new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x));
            var wallCenter = dir * (Cell / 2 - Wall / 2);
            if ((open & bit) == 0) {
                Box("Muro", cell, wallCenter + Vector3.up * height / 2, across * Cell + Abs(dir) * Wall + Vector3.up * height, "Muro");
                Box("Linea de mugre", cell, wallCenter - dir * (Wall / 2 + .01f) + Vector3.up * .35f, across * Cell + Abs(dir) * .02f + Vector3.up * .5f, "Mugre", false);
            } else if (chamber && (chamberSides & bit) == 0) {
                // Paso de la camara a un tunel normal: dintel sobre la abertura.
                Box("Dintel", cell, wallCenter + Vector3.up * (TunnelHeight + (ChamberHeight - TunnelHeight) / 2), across * Cell + Abs(dir) * Wall + Vector3.up * (ChamberHeight - TunnelHeight), "Muro");
            }
        }
        if (!chamber)
            for (int corner = 0; corner < 4; corner++) {
                var c = new Vector3(corner % 2 == 0 ? -1 : 1, 0, corner < 2 ? -1 : 1) * (Cell / 2 - Wall / 2);
                Box("Pilar", cell, c + Vector3.up * height / 2, new Vector3(Wall, height, Wall), "Concreto");
            }
        // Tuberia en el primer muro cerrado.
        for (int side = 0; side < 4; side++) {
            if ((open & (1 << side)) != 0) continue;
            var dir = Direction(side);
            var along = new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x));
            Cylinder("Tuberia", cell, dir * (Cell / 2 - Wall - .2f) + Vector3.up * (height - .6f), .16f, Cell, along.x > 0 ? new Vector3(0, 0, 90) : new Vector3(90, 0, 0), "Oxido");
            break;
        }
    }

    static Vector3 Direction(int side) => side switch { 0 => Vector3.forward, 1 => Vector3.right, 2 => Vector3.back, _ => Vector3.left };
    static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static void Walkway(Transform parent, float length) {
        Box("Pasarela", parent, new Vector3(0, .45f, 0), new Vector3(length, .1f, 1.4f), "Metal");
        Box("Escalon", parent, new Vector3(-length / 2 - .35f, .15f, 0), new Vector3(.7f, .3f, 1.4f), "Metal");
        for (float x = -length / 2; x <= length / 2 + .01f; x += 1.5f)
            Cylinder("Poste barandilla", parent, new Vector3(x, 1, -.65f), .03f, 1.1f, Vector3.zero, "Barandilla", true);
        Cylinder("Barandilla", parent, new Vector3(0, 1.5f, -.65f), .035f, length, new Vector3(0, 0, 90), "Barandilla");
        Cylinder("Barandilla baja", parent, new Vector3(0, 1.05f, -.65f), .03f, length, new Vector3(0, 0, 90), "Barandilla");
    }

    static void Ladder(Transform parent, float height) {
        for (int side = -1; side <= 1; side += 2) Cylinder("Larguero", parent, new Vector3(side * .25f, height / 2, 0), .03f, height, Vector3.zero, "Oxido");
        for (float y = .3f; y < height; y += .35f) Cylinder("Peldano", parent, new Vector3(0, y, 0), .02f, .5f, new Vector3(0, 0, 90), "Oxido");
    }

    static void Gate(Transform parent) {
        Box("Marco superior", parent, new Vector3(0, 2.9f, 0), new Vector3(4.8f, .25f, .25f), "Oxido");
        for (float x = -2.2f; x <= 2.21f; x += .4f) Cylinder("Barrote", parent, new Vector3(x, 1.45f, 0), .04f, 2.9f, Vector3.zero, "Oxido", true);
        Box("Cadena", parent, new Vector3(0, 1.2f, -.1f), new Vector3(.5f, .5f, .05f), "Metal", false);
    }

    // ---------- Escena 2: la alcantarilla ----------
    static void BuildSewer() {
        NewScene("P1_Alcantarilla", root => {
            Atmosphere(new Color(.06f, .075f, .065f), new Color(.012f, .02f, .016f), .045f);
            int w = SewerMap[0].Length, h = SewerMap.Length;
            bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && SewerMap[y][x] != '#';
            char At(int x, int y) => Open(x, y) ? SewerMap[y][x] : '#';
            Vector3 Pos(int x, int y) => new(x * Cell, 0, -y * Cell);

            var tunnels = Group("Tuneles", root);
            Vector2Int start = default, lab = default;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    if (!Open(x, y)) continue;
                    if (At(x, y) == 'S') start = new Vector2Int(x, y);
                    if (At(x, y) == 'L') lab = new Vector2Int(x, y);
                    int open = (Open(x, y - 1) ? 1 : 0) | (Open(x + 1, y) ? 2 : 0) | (Open(x, y + 1) ? 4 : 0) | (Open(x - 1, y) ? 8 : 0);
                    bool chamber = At(x, y) == 'C';
                    int chamberSides = chamber ? (At(x, y - 1) == 'C' ? 1 : 0) | (At(x + 1, y) == 'C' ? 2 : 0) | (At(x, y + 1) == 'C' ? 4 : 0) | (At(x - 1, y) == 'C' ? 8 : 0) : 0;
                    var cell = Group($"Celda {x},{y}", tunnels);
                    cell.localPosition = Pos(x, y);
                    SewerCell(cell, open, chamber, chamberSides);
                }

            var path = ShortestPath(start, lab, Open);
            if (path == null) throw new Exception("La alcantarilla no tiene camino de la escalera a la puerta");

            // Llegada: escalera desde el pozo de la calle, con la luz de la farola entrando por arriba.
            var arrival = Group("Llegada por la escalera", root);
            arrival.localPosition = Pos(start.x, start.y);
            var ladder = Group("Escalera", arrival);
            ladder.localPosition = new Vector3(0, 0, Cell / 2 - Wall - .1f);
            Ladder(ladder, TunnelHeight + .4f);
            var shaft = new GameObject("Luz del pozo").AddComponent<Light>();
            shaft.transform.SetParent(arrival, false);
            shaft.transform.localPosition = new Vector3(0, TunnelHeight + .3f, 1.5f);
            shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shaft.type = LightType.Spot;
            shaft.spotAngle = 40;
            shaft.range = 8;
            shaft.intensity = 4;
            shaft.color = new Color(.65f, .7f, .9f);
            shaft.shadows = LightShadows.None;
            var first = path[1] - path[0];
            Spawn("escalera", root, Pos(start.x, start.y) + new Vector3(0, .05f, 1.2f), Mathf.Atan2(first.x, -first.y) * Mathf.Rad2Deg);

            // Camara grande: pasarela con barandilla y luz de emergencia verde como punto de referencia.
            var chamberCells = new List<Vector2Int>();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (At(x, y) == 'C') chamberCells.Add(new Vector2Int(x, y));
            var chamberCenter = chamberCells.Aggregate(Vector3.zero, (a, c) => a + Pos(c.x, c.y)) / chamberCells.Count;
            var walkway = Group("Pasarela de la camara", root);
            walkway.localPosition = chamberCenter + new Vector3(0, 0, Cell * 1.5f - Wall - .8f);
            Walkway(walkway, Cell * 3 - 2 * Wall - 1.6f);
            PointLight("Luz de emergencia de la camara", root, chamberCenter + Vector3.up * (ChamberHeight - .6f), new Color(.45f, 1, .55f), 16, 2.5f, true);
            Box("Lampara de emergencia", root, chamberCenter + Vector3.up * (ChamberHeight - .35f), new Vector3(.6f, .15f, .3f), "LuzVerde", false);

            // Luces de emergencia viejas a lo largo de la ruta y alguna fuera de ella.
            for (int i = 2; i < path.Count - 1; i += 3) {
                var p = path[i];
                PointLight("Luz de emergencia", root, Pos(p.x, p.y) + Vector3.up * (TunnelHeight - .5f), new Color(.5f, .95f, .6f), 12, 2.4f, true);
                Box("Lampara de emergencia", root, Pos(p.x, p.y) + Vector3.up * (TunnelHeight - .25f), new Vector3(.4f, .12f, .2f), "LuzVerde", false);
            }
            PointLight("Luz lejana", root, Pos(13, 1) + Vector3.up * 3, new Color(.5f, .95f, .6f), 8, 1.2f, true);
            PointLight("Luz lejana", root, Pos(1, 8) + Vector3.up * 3, new Color(.5f, .95f, .6f), 8, 1.2f, true);

            // Compuertas cerradas en dos callejones sin salida.
            foreach (var deadEnd in new[] { new Vector2Int(13, 1), new Vector2Int(1, 8) }) {
                var gate = Group("Compuerta cerrada", root);
                gate.localPosition = Pos(deadEnd.x, deadEnd.y);
                Gate(gate);
            }

            // Marcas de arrastre recientes a lo largo de la ruta, a mitad de camino.
            var marks = Group("Marcas de arrastre", root);
            for (int i = 4; i < 12 && i + 1 < path.Count; i++) {
                var a = Pos(path[i].x, path[i].y);
                var b = Pos(path[i + 1].x, path[i + 1].y);
                var along = (b - a).normalized;
                for (int k = -1; k <= 1; k += 2) {
                    var side = Vector3.Cross(Vector3.up, along) * k * .35f;
                    var mark = Box("Marca", marks, (a + b) / 2 + side + Vector3.up * .14f, new Vector3(.12f, .01f, Cell * .9f), "Mugre", false);
                    mark.transform.localRotation = Quaternion.LookRotation(along);
                }
            }

            // Puerta de seguridad del laboratorio, entreabierta, con luz roja.
            var labDoor = Group("Puerta del laboratorio", root);
            labDoor.localPosition = Pos(lab.x, lab.y) + new Vector3(0, 0, -(Cell / 2 - Wall));
            Box("Marco", labDoor, new Vector3(0, 2.7f, 0), new Vector3(3, .4f, .3f), "Metal");
            var leaf = Box("Hoja de la puerta", labDoor, new Vector3(-.4f, 1.25f, .05f), new Vector3(1.6f, 2.5f, .12f), "Puerta");
            leaf.transform.localRotation = Quaternion.Euler(0, -18, 0);
            Box("Rendija oscura", labDoor, new Vector3(.75f, 1.25f, -.02f), new Vector3(.5f, 2.5f, .02f), "Mugre", false);
            Usable(leaf, "Entrar al laboratorio", "puerta-laboratorio");
            Box("Luz de alarma", labDoor, new Vector3(0, 3.1f, .15f), new Vector3(.3f, .15f, .15f), "LuzRoja", false);
            PointLight("Luz roja de la puerta", labDoor, new Vector3(0, 2.9f, .8f), new Color(1, .12f, .08f), 8, 2.4f, true);
            TextSign("LAB-03  ACCESO RESTRINGIDO", labDoor, new Vector3(0, 3.25f, .17f), 180, new Color(.9f, .9f, .85f), .03f);

            var drips = Particles("Goteo", root, new Vector3(8, 8, 1), 30, 0, .015f, 1.2f, "Lluvia");
            drips.gameObject.AddComponent<FollowCamera>().offset = new Vector3(0, 3.3f, 3);
            var dripMain = drips.main;
            dripMain.gravityModifier = 1;

            Player(root, true, new Color(.012f, .02f, .016f));

            // Ruta del GPS: los puntos donde el camino gira, hasta la puerta.
            var gps = Group("Ruta del GPS", root);
            var corners = new List<(int index, Transform point)>();
            for (int i = 1; i < path.Count; i++) {
                bool last = i == path.Count - 1;
                bool turn = !last && path[i + 1] - path[i] != path[i] - path[i - 1];
                if (turn || last) corners.Add((i, Point("GPS " + corners.Count, gps, Pos(path[i].x, path[i].y))));
            }
            Transform[] RouteFrom(int pathIndex) => corners.Where(c => c.index >= pathIndex).Select(c => c.point).ToArray();

            var story = Systems(root);
            int marksAt = 6, chamberAt = path.FindIndex(p => At(p.x, p.y) == 'C');
            var follow = Step("Sigue el GPS hasta la puerta de seguridad", "marcas",
                "El GPS marca una ruta bajo las calles. Termina en una puerta de seguridad.",
                "El agua le llega a los tobillos.");
            follow.gpsRoute = RouteFrom(0);
            follow.gpsSignal = .9f;
            follow.checkpoint = true;
            follow.checkpointSpawn = "escalera";
            var dragged = Step("Sigue el GPS hasta la puerta de seguridad", "camara",
                "Marcas recientes en el suelo... como si algo pesado hubiera sido arrastrado por aqui.");
            dragged.gpsRoute = RouteFrom(marksAt);
            dragged.gpsSignal = .55f;
            var weak = Step("Sigue el GPS hasta la puerta de seguridad", "puerta-vista", "La senal del GPS se debilita.");
            weak.gpsRoute = RouteFrom(chamberAt);
            weak.gpsSignal = .25f;
            var door = Step("Entra por la puerta del laboratorio", "puerta-laboratorio", "Al fin. La puerta del laboratorio. Esta entreabierta.");
            door.marker = leaf.transform;
            door.completeChapterTitle = "Fin del prologo";
            story.steps = new List<ObjectiveStep> { follow, dragged, weak, door };

            var markCell = path[marksAt];
            Trigger("marcas", root, Pos(markCell.x, markCell.y) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            var chamberCell = path[chamberAt];
            Trigger("camara", root, Pos(chamberCell.x, chamberCell.y) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            var nearLab = path[path.Count - 2];
            Trigger("puerta-vista", root, Pos(nearLab.x, nearLab.y) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
        });
    }

    static List<Vector2Int> ShortestPath(Vector2Int from, Vector2Int to, Func<int, int, bool> open) {
        var previous = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        while (queue.Count > 0) {
            var c = queue.Dequeue();
            if (c == to) break;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }) {
                var n = c + d;
                if (open(n.x, n.y) && !previous.ContainsKey(n)) { previous[n] = c; queue.Enqueue(n); }
            }
        }
        if (!previous.ContainsKey(to)) return null;
        var path = new List<Vector2Int> { to };
        while (path[^1] != from) path.Add(previous[path[^1]]);
        path.Reverse();
        return path;
    }
}
