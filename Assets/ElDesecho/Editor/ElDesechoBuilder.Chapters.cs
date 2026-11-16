using System;
using System.Collections.Generic;
using System.Linq;
using ElDesecho;
using UnityEditor;
using UnityEngine;

// Capitulos 1 (laboratorio) y 2 (tres caminos). Mismo sistema que el prologo, otra estetica:
// el laboratorio es limpio, blanco y luminoso hasta que suena la alarma; la alcantarilla es humeda y organica.
public static partial class ElDesechoBuilder {
    const float LabCell = 4, LabHeight = 3.2f, LabWall = .3f;

    // # pared, . pasillo, H sala principal, E entrada desde la alcantarilla, F almacen (linterna),
    // G oficina de seguridad (GPS), X puerta de mantenimiento hacia las alcantarillas.
    static readonly string[] LabMap = {
        "#################",
        "#F..#.....#....G#",
        "#.#.#.###.#.##.##",
        "#.#...#.....#...#",
        "#.#####.###.#.#.#",
        "#.....#.#.....#.#",
        "#.###.HHH.###.#.#",
        "#...#.HHH...#...#",
        "###.#.HHH.#.###.#",
        "#X..#..E..#.....#",
        "#################",
    };

    // # roca, . tunel, C cruce, S llegada, D puerta cerrada (camino corto), ~ tunel inundado,
    // K derrumbe (fin del inundado), A suministros (adrenalina), r corredor estrecho, d conducto pequeno, Z salida.
    static readonly string[] PathsMap = {
        "######################",
        "#K~~~~~~~~############",
        "#########~##......####",
        "#########~##.##.#.####",
        "#########~##.##.#.####",
        "#########~##.##.#.####",
        "#######CCC##.####.####",
        "#D.....CCC...####.####",
        "#######CCC####....####",
        "########S#####.#######",
        "##############ArrdddZ#",
        "######################",
    };

    static void MakeChapterMaterials() {
        Lit("Azulejo", new Color(.82f, .84f, .86f), .75f);
        Lit("AzulejoGris", new Color(.62f, .65f, .68f), .75f);
        Lit("PanelBlanco", new Color(.78f, .8f, .82f), .55f, .2f);
        Lit("FranjaAzul", new Color(.12f, .32f, .6f), .6f);
        Lit("Zocalo", new Color(.18f, .2f, .22f), .4f);
        Lit("TechoLab", new Color(.7f, .72f, .74f), .3f);
        Emissive("Fluorescente", new Color(.85f, .93f, 1), 2.2f);
        Emissive("PanelAlarma", new Color(1, .06f, .04f), 1.6f);
        TransparentLit("Cristal", new Color(.7f, .85f, .9f, .25f), .95f);
        Emissive("Liquido", new Color(.25f, .9f, .35f), 1.2f);
        Lit("Sangre", new Color(.16f, .02f, .015f), .8f);
        Lit("Bata", new Color(.9f, .9f, .88f), .3f);
        Lit("Piel", new Color(.55f, .42f, .36f), .4f);
        Emissive("Pantalla", new Color(.2f, .55f, 1), 1.4f);
        Lit("Casillero", new Color(.32f, .4f, .38f), .5f, .6f);
        Lit("Mesa", new Color(.45f, .47f, .5f), .6f, .5f);
        Lit("Moho", new Color(.06f, .12f, .05f), .4f);
        Lit("Baba", new Color(.18f, .26f, .12f), .95f);
        Lit("AguaProfunda", new Color(.03f, .07f, .05f), .98f);
        Emissive("Senal", new Color(1, .1f, .05f), 2);
        Emissive("Jeringa", new Color(.9f, .2f, .3f), 1.5f);
    }

    static Vector3 LabPos(int x, int y) => new(x * LabCell, 0, -y * LabCell);
    static bool creaturePrepared;

    // scale < 1: una de las masas pequenas en que se dividio el compuesto (mismo modelo, mas rapida).
    static CreatureBrain Creature(Transform parent, Vector3 position, bool dormant, float scale = 1) {
        // El prefab de la criatura se rehace una vez por compilacion (por si cambio el modelo o sus animaciones).
        if (!creaturePrepared) { CreatureSetup.Prepare(); creaturePrepared = true; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreatureSetup.PrefabPath);
        var creature = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        creature.transform.localPosition = position;
        creature.transform.localScale = Vector3.one * scale;
        var xray = creature.AddComponent<XRayTarget>();
        xray.kind = XRayKind.Creature;
        xray.offset = Vector3.up * 1.4f * scale;
        var body = creature.AddComponent<CharacterController>();
        body.height = 2.4f;
        body.radius = .45f;
        body.center = Vector3.up * 1.2f;
        body.stepOffset = .35f;
        var brain = creature.AddComponent<CreatureBrain>();
        brain.startDormant = dormant;
        brain.catchDistance = 1.5f; // brazos largos: atrapa desde un poco mas lejos
        const string audio = "Assets/ElDesecho/Creature/Audio/";
        brain.breathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "Breath.wav");
        brain.stepClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "Step.wav");
        brain.alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "Alert.wav");
        brain.attackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "Attack.wav");
        if (scale < 1) {
            brain.patrolSpeed = 1.6f;
            brain.investigateSpeed = 2.6f;
            brain.chaseSpeed = 3.9f;
            brain.catchDistance = 1;
        }
        return brain;
    }

    static void Grid(Transform root, string[] rows, float cell, string playerOnly) {
        var grid = root.gameObject.AddComponent<CellGrid>();
        grid.rows = rows;
        grid.cellSize = cell;
        grid.playerOnly = playerOnly;
    }

    static IEnumerable<Vector2Int> OpenCells(string[] rows) {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '#') yield return new Vector2Int(x, y);
    }

    static void Hide(Transform parent, Vector3 position, Vector3 size, bool crouch, string label) {
        var g = new GameObject("Escondite " + label);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        var box = g.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var spot = g.AddComponent<HidingSpot>();
        spot.needsCrouch = crouch;
        spot.label = label;
    }

    static void Noisy(Transform parent, Vector3 position, string kind) {
        var g = new GameObject("Ruidoso " + kind);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        var box = g.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.1f, 1, 1.1f);
        var prop = g.AddComponent<NoisyProp>();
        if (kind == "cristales") {
            for (int i = 0; i < 6; i++) {
                var shard = Box("Cristal roto", g.transform, new Vector3((i % 3 - 1) * .25f, .02f, (i / 3 - .5f) * .3f), new Vector3(.12f, .01f, .07f), "Cristal", false);
                shard.transform.localRotation = Quaternion.Euler(0, i * 47, 0);
            }
        } else {
            prop.visual = Cylinder("Lata", g.transform, new Vector3(0, .07f, 0), .05f, .14f, new Vector3(90, 0, 30), "Oxido").transform;
            Cylinder("Lata", g.transform, new Vector3(.25f, .07f, .15f), .05f, .14f, Vector3.zero, "Metal");
        }
    }

    // ===================== CAPITULO 1: EL LABORATORIO SILENCIOSO =====================
    static void BuildLab() {
        NewScene("C1_Laboratorio", root => {
            Atmosphere(new Color(.32f, .35f, .38f), new Color(.6f, .64f, .68f), .012f);
            int w = LabMap[0].Length, h = LabMap.Length;
            bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && LabMap[y][x] != '#';
            char At(int x, int y) => Open(x, y) ? LabMap[y][x] : '#';
            Grid(root, LabMap, LabCell, "");

            var building = Group("Laboratorio", root);
            var lights = new List<Light>();
            var panels = new List<Renderer>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    if (!Open(x, y)) continue;
                    int open = (Open(x, y - 1) ? 1 : 0) | (Open(x + 1, y) ? 2 : 0) | (Open(x, y + 1) ? 4 : 0) | (Open(x - 1, y) ? 8 : 0);
                    var cell = Group($"Sala {x},{y}", building);
                    cell.localPosition = LabPos(x, y);
                    LabCellGeo(cell, open, (x + y) % 2 == 0, panels);
                    // Mobiliario modelado en Blender, lejos de pruebas, cuerpos y escondites.
                    var busy = new[] { (3, 3), (11, 3), (8, 8), (13, 5), (1, 5), (15, 7), (1, 3), (1, 1), (15, 1), (5, 3), (15, 5), (3, 7) };
                    if (At(x, y) == '.' && (x * 5 + y * 3) % 4 == 1 && !busy.Contains((x, y))) LabProps(cell, open, x + y, LabCell, LabWall);
                    if ((x * 3 + y * 5) % 4 == 0 || At(x, y) is 'E' or 'F' or 'G' or 'X' or 'H')
                    {
                        // Algunos fluorescentes parpadean y otros estan rotos (apagados, con el panel oscuro).
                        var tube = PointLight("Fluorescente", cell, Vector3.up * (LabHeight - .3f), new Color(.85f, .92f, 1), 7, 2.4f, lights.Count % 4 == 1);
                        if (lights.Count % 6 == 3) { tube.enabled = false; panels[^1].sharedMaterial = panels[^2].sharedMaterial = M("Ventana"); }
                        lights.Add(tube);
                    }
                }

            // Entrada: la puerta de seguridad entreabierta por la que llega desde la alcantarilla.
            var entry = LabPos(7, 9);
            var door = Group("Puerta de la alcantarilla", root);
            door.localPosition = entry + new Vector3(0, 0, -(LabCell / 2 - LabWall));
            var hinge = Group("Bisagra", door);
            hinge.localPosition = new Vector3(-1.2f, 0, 0);
            var leaf = Box("Hoja", hinge, new Vector3(.8f, 1.25f, .05f), new Vector3(1.6f, 2.5f, .12f), "Puerta");
            hinge.localRotation = Quaternion.Euler(0, -35, 0);
            var back = Usable(leaf, "Volver atras", "volver");
            back.onlyWhenObjective = false;
            back.singleUse = false;
            back.sayOnUse = "No hay vuelta atras. Sin GPS, en esos tuneles se perderia.";
            Box("Marco", door, new Vector3(0, 2.6f, 0), new Vector3(2.6f, .3f, .3f), "Metal");
            TextSign("LABORATORIO 03 · ZONA RESTRINGIDA", building, entry + new Vector3(0, 2.45f, 1.6f), 0, new Color(.1f, .25f, .5f), .03f);

            // Sala principal: tanque de contencion roto, mesas, equipos volcados.
            var hall = Group("Sala principal", root);
            hall.localPosition = LabPos(7, 7);
            var tank = Group("Tanque de contencion roto", hall);
            tank.localPosition = new Vector3(2.6f, 0, -2.6f);
            Prop("Tanque_Contencion", hall, new Vector3(-2.6f, 0, -2.6f), 30);
            Cylinder("Base", tank, new Vector3(0, .15f, 0), .9f, .3f, Vector3.zero, "Metal", true);
            Cylinder("Cristal del tanque", tank, new Vector3(0, 1.4f, 0), .85f, 2.2f, Vector3.zero, "Cristal");
            Cylinder("Residuo", tank, new Vector3(0, .4f, 0), .8f, .2f, Vector3.zero, "Liquido");
            Box("Charco verde", tank, new Vector3(-.9f, .01f, .7f), new Vector3(2, .01f, 1.4f), "Liquido", false);
            Noisy(hall, new Vector3(1.4f, 0, -1.4f), "cristales");
            Bench(hall, new Vector3(-2, 0, 2), 0, true);
            Bench(hall, new Vector3(2, 0, 2), 0, true);
            Box("Silla volcada", hall, new Vector3(-1.6f, .25f, -2.8f), new Vector3(.5f, .5f, .5f), "Zocalo").transform.localRotation = Quaternion.Euler(80, 20, 0);
            var vent = Group("Rejilla de ventilacion", root);
            vent.localPosition = LabPos(6, 6);
            Box("Rejilla", vent, new Vector3(0, 2.5f, LabCell / 2 - LabWall - .06f), new Vector3(1.4f, .8f, .1f), "Metal", false);

            // Pruebas para fotografiar.
            var evidence = Group("Pruebas", root);
            Photo(evidence, LabPos(3, 3) + new Vector3(1, 0, 1), "Fotografiar el cuerpo", lab => Corpse(lab, 0));
            Photo(evidence, LabPos(11, 3) + new Vector3(-1.2f, 0, 1.2f), "Fotografiar el equipo destrozado", lab => {
                Box("Equipo volcado", lab, new Vector3(0, .35f, 0), new Vector3(1.2f, .7f, .8f), "Mesa").transform.localRotation = Quaternion.Euler(0, 20, 75);
                Box("Pantalla rota", lab, new Vector3(.6f, .1f, .5f), new Vector3(.5f, .05f, .35f), "Pantalla", false);
            });
            Photo(evidence, LabPos(8, 8) + new Vector3(1.3f, 0, -1.3f), "Fotografiar el tanque roto", lab => Box("Fuga verde", lab, new Vector3(0, .02f, 0), new Vector3(1.2f, .01f, .9f), "Liquido", false));
            Photo(evidence, LabPos(13, 5) + new Vector3(1.2f, 0, 1.2f), "Fotografiar las marcas en la pared", lab => {
                Box("Marcas", lab, new Vector3(.2f, 1.2f, .5f), new Vector3(.05f, 1.8f, .25f), "Sangre", false);
                Box("Marcas", lab, new Vector3(.2f, 1.1f, 0), new Vector3(.05f, 1.6f, .18f), "Sangre", false);
            });

            // Otros restos: cuerpos, manchas y cristales.
            Corpse(Point("Cuerpo", root, LabPos(1, 5) + new Vector3(.8f, 0, -1)), 90);
            Corpse(Point("Cuerpo", root, LabPos(15, 7) + new Vector3(-1, 0, .9f)), 200);
            var stains = new[] { (5, 3), (9, 3), (1, 7), (5, 5), (11, 5), (15, 3), (3, 9), (13, 9), (11, 7) };
            for (int i = 0; i < stains.Length; i++)
                Decal(i % 3 == 2 ? "Baba" : "Sangre", root, LabPos(stains[i].Item1, stains[i].Item2) + new Vector3((i % 3 - 1) * 1.1f, .25f, (i % 2 - .5f) * 1.6f), Vector2.one * (1.2f + i % 2 * .6f), i * 47);
            // Rastro de baba desde el tanque roto y aranazos en las paredes.
            for (int i = 0; i < 6; i++) Decal("Baba", root, LabPos(7, 7) + new Vector3(1.6f - i * .9f, .25f, -1.2f - i * .5f), new Vector2(.9f, .9f), i * 61);
            Decal("Aranazo", root, LabPos(13, 5) + new Vector3(1.5f, 1.4f, .3f), new Vector2(1, 1.3f), 0, Vector3.right);
            Decal("Aranazo", root, LabPos(5, 9) + new Vector3(-1.4f, 1.5f, 0), new Vector2(1.1f, 1.4f), 8, Vector3.left);
            Decal("Aranazo", root, LabPos(1, 1) + new Vector3(0, 1.5f, 1.4f), new Vector2(1.1f, 1.4f), -6, Vector3.forward);
            Decal("Sangre", root, LabPos(7, 8) + new Vector3(-1.3f, .25f, -.6f), Vector2.one * 1.5f, 30);
            Noisy(root, LabPos(5, 3), "cristales");
            Noisy(root, LabPos(15, 5), "cristales");
            Noisy(root, LabPos(3, 7), "latas");

            // Escondites: casilleros en los pasillos y huecos bajo las mesas.
            var hide = Group("Escondites", root);
            Locker(hide, LabPos(1, 3) + new Vector3(-1.25f, 0, 0), 90);
            Locker(hide, LabPos(9, 3) + new Vector3(0, 0, -1.25f), 0);
            Locker(hide, LabPos(13, 7) + new Vector3(0, 0, -1.25f), 0);
            Locker(hide, LabPos(3, 9) + new Vector3(0, 0, -1.25f), 0);
            Locker(hide, LabPos(15, 9) + new Vector3(0, 0, -1.25f), 0);
            Bench(hide, LabPos(5, 1) + new Vector3(0, 0, 1.1f), 0, true);

            // Almacen: linterna de emergencia. Seguridad: GPS de repuesto. Salida de mantenimiento.
            var store = Group("Almacen", root);
            store.localPosition = LabPos(1, 1);
            Box("Armario de emergencia", store, new Vector3(-1.4f, 1, 1.2f), new Vector3(.8f, 2, .5f), "PanelAlarma");
            var lamp = Cylinder("Linterna de emergencia", store, new Vector3(-1.4f, 1.1f, .85f), .07f, .3f, new Vector3(90, 0, 0), "Barandilla");
            var lampCollider = lamp.AddComponent<BoxCollider>();
            lampCollider.isTrigger = true;
            lampCollider.size = new Vector3(4, 4, 4);
            var lampUse = Usable(lamp, "Coger la linterna de emergencia", "linterna");
            lampUse.give = "linterna";
            lampUse.hideOnUse = true;
            TextSign("ALMACEN · EMERGENCIA", store, new Vector3(0, 2.6f, LabCell / 2 - LabWall - .02f), 180, new Color(.6f, .05f, .05f), .03f);
            var office = Group("Oficina de seguridad", root);
            office.localPosition = LabPos(15, 1);
            Box("Escritorio", office, new Vector3(1, .45f, 1.2f), new Vector3(1.6f, .9f, .8f), "Mesa");
            Box("Monitor", office, new Vector3(.6f, 1.15f, 1.35f), new Vector3(.6f, .4f, .05f), "Pantalla", false);
            var gps = Box("GPS de repuesto", office, new Vector3(1.3f, .95f, 1.1f), new Vector3(.12f, .04f, .2f), "Pantalla", false);
            var gpsCollider = gps.AddComponent<BoxCollider>();
            gpsCollider.isTrigger = true;
            gpsCollider.size = new Vector3(8, 30, 8);
            var gpsUse = Usable(gps, "Coger el GPS (danado)", "gps");
            gpsUse.give = "gps";
            gpsUse.hideOnUse = true;
            TextSign("SEGURIDAD", office, new Vector3(0, 2.6f, LabCell / 2 - LabWall - .02f), 180, new Color(.1f, .25f, .5f), .03f);
            var exit = Group("Puerta de mantenimiento", root);
            exit.localPosition = LabPos(1, 9) + new Vector3(-(LabCell / 2 - LabWall), 0, 0);
            var exitDoor = Box("Puerta", exit, new Vector3(.05f, 1.2f, 0), new Vector3(.12f, 2.4f, 1.4f), "Oxido");
            Usable(exitDoor, "Abrir la puerta de mantenimiento", "salida");
            TextSign("MANTENIMIENTO · ALCANTARILLADO", exit, new Vector3(.15f, 2.65f, 0), 90, new Color(.9f, .8f, .2f), .028f);
            Box("Luz de salida", exit, new Vector3(.15f, 2.9f, 0), new Vector3(.05f, .12f, .5f), "LuzVerde", false);

            // El cientifico herido aparece al fondo de la sala tras el grito.
            var scientist = Group("Cientifico herido", root);
            scientist.localPosition = LabPos(7, 5) + new Vector3(1.1f, 0, 1.2f);
            scientist.localRotation = Quaternion.Euler(0, 180, 0);
            var coat = Cylinder("Cuerpo", scientist, new Vector3(0, .85f, 0), .25f, 1.3f, Vector3.zero, "Bata", true);
            Box("Sangre en la bata", scientist, new Vector3(0, 1, .24f), new Vector3(.3f, .5f, .02f), "Sangre", false);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza";
            head.transform.SetParent(scientist, false);
            head.transform.localPosition = Vector3.up * 1.68f;
            head.transform.localScale = Vector3.one * .26f;
            Finish(head, "Piel", false);
            Usable(coat, "Ayudar al herido", "cientifico");

            var dropped = Cylinder("Linterna caida", root, Vector3.zero, .06f, .26f, new Vector3(90, 0, 0), "Metal", true);
            dropped.AddComponent<Rigidbody>().mass = .4f;
            dropped.SetActive(false);

            var alarm = new GameObject("Alarma").AddComponent<AlarmLights>();
            alarm.transform.SetParent(root, false);
            alarm.lights = lights.ToArray();
            alarm.panels = panels.ToArray();
            alarm.alarmPanel = M("PanelAlarma");
            alarm.alarmVolume = AlarmLook(root);
            // Sombras solo en tres fluorescentes clave (rendimiento en telefono).
            foreach (var key in lights.Where(l => l.enabled).Where((l, i) => i % 9 == 0).Take(3)) key.shadows = LightShadows.Soft;

            var player = Player(root, true, new Color(.6f, .64f, .68f));
            player.floorNoise = 1;
            Spawn("entrada", root, entry + new Vector3(0, .05f, -.6f), 0);
            Spawn("huida", root, LabPos(7, 8) + Vector3.up * .05f, 180);
            Spawn("almacen", root, LabPos(1, 1) + new Vector3(.8f, .05f, -.8f), 180);
            Spawn("seguridad", root, LabPos(15, 1) + new Vector3(-.5f, .05f, -.8f), 180);

            var creature = Creature(root, LabPos(6, 6), true);
            creature.patrolArea = OpenCells(LabMap).ToArray();

            var sequence = new GameObject("Secuencia del ataque").AddComponent<Chapter1Sequence>();
            sequence.transform.SetParent(root, false);
            sequence.creature = creature;
            sequence.scientist = scientist;
            sequence.vent = vent;
            sequence.entranceDoor = hinge;
            sequence.droppedFlashlight = dropped.transform;
            sequence.alarm = alarm;
            sequence.firstStepAfterAttack = 5;

            var story = Systems(root);
            var explore = Step("Explora el laboratorio", "pasillo",
                "La puerta estaba entreabierta. Del tunel oscuro y humedo a un laboratorio frio, blanco... y destrozado.",
                "Pasillos con luces que parpadean, equipos volcados, cristales rotos y manchas oscuras en el suelo.");
            explore.checkpoint = true;
            explore.checkpointSpawn = "entrada";
            var photos = Step("Fotografia las pruebas", "foto", "Saco la camara. Tenia que documentar todo lo que habia pasado aqui.");
            photos.count = 4;
            var scream = Step("Busca de donde vino el grito", "cientifico",
                "Cada prueba confirmaba que alli habia ocurrido algo terrible.",
                "Entonces se oyo un grito desde el fondo del laboratorio.");
            scream.marker = coat.transform;
            scream.raiseOnStart = "cientifico-aparece";
            var dialogue = Step("Escucha al cientifico", "",
                "Un hombre cubierto de sangre se apoyaba contra la pared. -Ayudame... no. No hay tiempo.",
                "-Trabajabamos en un compuesto para desintegrar microorganismos. Iba a eliminar los residuos peligrosos del planeta.",
                "-En una prueba reacciono. Se volvio una masa viscosa... viva. Lo que toca, desaparece.",
                "-Despues se dividio en varias masas mas pequenas. Intentamos contenerlas, pero el laboratorio cayo en el caos.",
                "-Toma. El archivo con los resultados y los registros del experimento.",
                "-Llevalo a las autoridades. Si entienden lo que hicimos, quiza puedan detenerlo.");
            dialogue.dialogue = true;
            dialogue.raiseOnStart = "archivo";
            var attack = Step("", "ataque-fin");
            attack.raiseOnStart = "alarma";
            var flee = Step("Huye y busca una linterna de emergencia", "linterna",
                "Sin GPS y sin luz, perdio la orientacion. El laboratorio se habia convertido en un laberinto rojo.",
                "La criatura no ve: te encuentra por lo que oye. Muevete agachado y en silencio.",
                "Si se acerca, escondete en un casillero o bajo una mesa.");
            flee.marker = lamp.transform;
            flee.checkpoint = true;
            flee.checkpointSpawn = "huida";
            var findGps = Step("Busca un GPS que funcione", "gps",
                "Con la linterna de emergencia veia algo mejor. Ahora necesitaba orientarse.",
                "En la oficina de seguridad deberia haber un localizador.");
            findGps.marker = gps.transform;
            findGps.checkpoint = true;
            findGps.checkpointSpawn = "almacen";
            var leave = Step("Encuentra la puerta de mantenimiento hacia las alcantarillas", "salida",
                "El GPS estaba danado: solo daba senal a ratos.",
                "Y las senales del archivo sugerian algo peor: no habia una sola masa suelta en el laboratorio.");
            leave.marker = exitDoor.transform;
            leave.checkpoint = true;
            leave.checkpointSpawn = "seguridad";
            leave.completeChapterTitle = "Fin del capitulo 1";
            story.steps = new List<ObjectiveStep> { explore, photos, scream, dialogue, attack, flee, findGps, leave };
            Trigger("pasillo", root, LabPos(7, 8) + Vector3.up * 1.5f, new Vector3(LabCell, 3, LabCell));
        });
    }

    static void LabCellGeo(Transform cell, int open, bool white, List<Renderer> panels) {
        Box("Suelo de azulejos", cell, new Vector3(0, -.1f, 0), new Vector3(LabCell, .2f, LabCell), white ? "Azulejo" : "AzulejoGris");
        Box("Techo", cell, new Vector3(0, LabHeight + .1f, 0), new Vector3(LabCell, .2f, LabCell), "TechoLab");
        // Pantalla de fluorescente modelada (Blender) con dos tubos; los tubos cambian a rojo con la alarma.
        AlignLong(Prop("Fluorescente_Carcasa", cell, new Vector3(0, LabHeight, 0), 0, false), Vector3.forward);
        for (int t = -1; t <= 1; t += 2)
            panels.Add(Box("Tubo fluorescente", cell, new Vector3(t * .08f, LabHeight - .13f, 0), new Vector3(.045f, .045f, 2.1f), "Fluorescente", false).GetComponent<Renderer>());
        for (int side = 0; side < 4; side++) {
            if ((open & (1 << side)) != 0) continue;
            var dir = Direction(side);
            var across = new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x));
            var center = dir * (LabCell / 2 - LabWall / 2);
            Box("Panel blanco", cell, center + Vector3.up * LabHeight / 2, across * LabCell + Abs(dir) * LabWall + Vector3.up * LabHeight, "PanelBlanco");
            var face = center - dir * (LabWall / 2 + .01f);
            Box("Franja azul", cell, face + Vector3.up * 1.05f, across * LabCell + Abs(dir) * .02f + Vector3.up * .12f, "FranjaAzul", false);
            Box("Zocalo", cell, face + Vector3.up * .08f, across * LabCell + Abs(dir) * .03f + Vector3.up * .16f, "Zocalo", false);
        }
    }

    static void Photo(Transform parent, Vector3 position, string prompt, Action<Transform> scene) {
        var spot = Group("Prueba", parent);
        spot.localPosition = position;
        scene(spot);
        var target = new GameObject("Encuadre");
        target.transform.SetParent(spot, false);
        target.transform.localPosition = Vector3.up * .6f;
        var box = target.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.6f, 1.4f, 1.6f);
        var item = Usable(target, prompt, "foto");
        item.photo = true;
        var glow = new GameObject("Brillo").AddComponent<Light>();
        glow.transform.SetParent(spot, false);
        glow.transform.localPosition = Vector3.up * 1.8f;
        glow.type = LightType.Point;
        glow.range = 2.5f;
        glow.intensity = 1.2f;
        glow.color = new Color(1, .95f, .8f);
        glow.shadows = LightShadows.None;
    }

    static void Corpse(Transform parent, float yaw) {
        var body = Group("Cuerpo de cientifico", parent);
        body.localRotation = Quaternion.Euler(0, yaw, 0);
        Cylinder("Bata", body, new Vector3(0, .2f, 0), .22f, 1.4f, new Vector3(90, 0, 0), "Bata");
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Cabeza";
        head.transform.SetParent(body, false);
        head.transform.localPosition = new Vector3(0, .17f, .85f);
        head.transform.localScale = Vector3.one * .24f;
        Finish(head, "Piel", false);
        Box("Mancha", body, new Vector3(.2f, .01f, .1f), new Vector3(1.1f, .01f, .9f), "Sangre", false);
    }

    static void Bench(Transform parent, Vector3 position, float yaw, bool hideUnder) {
        var bench = Group("Mesa de laboratorio", parent);
        bench.localPosition = position;
        bench.localRotation = Quaternion.Euler(0, yaw, 0);
        Box("Tablero", bench, new Vector3(0, 1.25f, 0), new Vector3(2.2f, .08f, .9f), "Mesa");
        for (int i = 0; i < 4; i++) Box("Pata", bench, new Vector3(i % 2 == 0 ? -1 : 1, .62f, i < 2 ? -.38f : .38f), new Vector3(.06f, 1.25f, .06f), "Metal");
        Box("Matraz", bench, new Vector3(-.5f, 1.4f, .1f), new Vector3(.12f, .22f, .12f), "Cristal", false);
        if (hideUnder) Hide(bench, new Vector3(0, .55f, 0), new Vector3(1.9f, 1, .8f), true, "bajo la mesa");
    }

    static void Locker(Transform parent, Vector3 position, float yaw) {
        var locker = Group("Casillero", parent);
        locker.localPosition = position;
        locker.localRotation = Quaternion.Euler(0, yaw, 0);
        Box("Fondo", locker, new Vector3(0, 1.1f, -.42f), new Vector3(1, 2.2f, .06f), "Casillero");
        Box("Lado", locker, new Vector3(-.5f, 1.1f, 0), new Vector3(.06f, 2.2f, .9f), "Casillero");
        Box("Lado", locker, new Vector3(.5f, 1.1f, 0), new Vector3(.06f, 2.2f, .9f), "Casillero");
        Box("Techo", locker, new Vector3(0, 2.2f, 0), new Vector3(1, .06f, .9f), "Casillero");
        Box("Puerta abierta", locker, new Vector3(.75f, 1.1f, .55f), new Vector3(.04f, 2.1f, .5f), "Casillero", false).transform.localRotation = Quaternion.Euler(0, 60, 0);
        Hide(locker, new Vector3(0, 1, -.1f), new Vector3(.8f, 2, .6f), false, "en el casillero");
    }

    // ===================== CAPITULO 2: TRES CAMINOS =====================
    static void BuildThreePaths() {
        NewScene("C2_TresCaminos", root => {
            Atmosphere(new Color(.035f, .05f, .035f), new Color(.008f, .015f, .01f), .055f);
            int w = PathsMap[0].Length, h = PathsMap.Length;
            bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && PathsMap[y][x] != '#';
            char At(int x, int y) => Open(x, y) ? PathsMap[y][x] : '#';
            Vector3 Pos(int x, int y) => new(x * Cell, 0, -y * Cell);
            Grid(root, PathsMap, Cell, "d");
            var rng = new System.Random(7);

            var tunnels = Group("Alcantarilla", root);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    if (!Open(x, y)) continue;
                    char k = At(x, y);
                    int open = (Open(x, y - 1) ? 1 : 0) | (Open(x + 1, y) ? 2 : 0) | (Open(x, y + 1) ? 4 : 0) | (Open(x - 1, y) ? 8 : 0);
                    var cell = Group($"Celda {x},{y}", tunnels);
                    cell.localPosition = Pos(x, y);
                    if (k == 'd') { DuctCell(cell, At(x - 1, y) != 'd', At(x + 1, y) != 'd'); continue; }
                    bool chamber = k == 'C';
                    int chamberSides = chamber ? (At(x, y - 1) == 'C' ? 1 : 0) | (At(x + 1, y) == 'C' ? 2 : 0) | (At(x, y + 1) == 'C' ? 4 : 0) | (At(x - 1, y) == 'C' ? 8 : 0) : 0;
                    SewerCell(cell, open, chamber, chamberSides);
                    OrganicDressing(cell, open, chamber, rng);
                    if (k == '~' || k == 'K') FloodCell(cell);
                    if (k == 'r') NarrowCell(cell);
                }

            // Cruce: plano viejo en la pared, pasarela con hueco debajo para esconderse, luz desde una rejilla.
            var hub = Group("Cruce", root);
            hub.localPosition = Pos(8, 7);
            float southFace = -(Cell * 1.5f - Wall - .05f);
            var map = Box("Plano viejo", hub, new Vector3(-4.5f, 1.6f, southFace), new Vector3(1.6f, 1.1f, .05f), "Acera");
            Usable(map, "Mirar el viejo plano", "plano");
            TextSign("PLANO DE LA RED", hub, new Vector3(-4.5f, 2.3f, southFace + .03f), 0, new Color(.85f, .8f, .7f), .03f);
            var walkway = Group("Pasarela", hub);
            walkway.localPosition = new Vector3(-3.5f, .85f, Cell * 1.5f - Wall - .8f);
            Walkway(walkway, 9);
            Hide(walkway, new Vector3(0, -.45f, 0), new Vector3(8.2f, .8f, 1.2f), true, "bajo la pasarela");
            var grate = new GameObject("Luz por la rejilla").AddComponent<Light>();
            grate.transform.SetParent(hub, false);
            grate.transform.localPosition = new Vector3(0, ChamberHeight, 0);
            grate.transform.localRotation = Quaternion.Euler(90, 0, 0);
            grate.type = LightType.Spot;
            grate.spotAngle = 50;
            grate.range = 10;
            grate.intensity = 3;
            grate.color = new Color(.6f, .7f, .85f);
            grate.shadows = LightShadows.None;
            GameObject Cross(string name, Vector3 at, float yaw) {
                var g = Group(name, root);
                g.localPosition = at;
                g.localRotation = Quaternion.Euler(0, yaw, 0);
                Box("Aspa", g, Vector3.zero, new Vector3(1.4f, .18f, .05f), "Senal", false).transform.localRotation = Quaternion.Euler(0, 0, 45);
                Box("Aspa", g, Vector3.zero, new Vector3(1.4f, .18f, .05f), "Senal", false).transform.localRotation = Quaternion.Euler(0, 0, -45);
                PointLight("Brillo", g, Vector3.back * .4f, new Color(1, .15f, .1f), 4, 1.2f);
                g.gameObject.SetActive(false);
                return g.gameObject;
            }
            var shortMark = Cross("Camino falso: corto", Pos(6, 7) + new Vector3(0, 3.1f, 0), 90);
            var floodMark = Cross("Camino falso: inundado", Pos(9, 5) + new Vector3(0, 3.1f, 0), 0);

            // Camino corto: termina en una puerta cerrada.
            var dead = Group("Puerta cerrada", root);
            dead.localPosition = Pos(1, 7) + new Vector3(-(Cell / 2 - Wall), 0, 0);
            Box("Puerta oxidada", dead, new Vector3(.05f, 1.3f, 0), new Vector3(.15f, 2.6f, 1.8f), "Oxido");
            TextSign("SALIDA", dead, new Vector3(.2f, 2.9f, 0), 90, new Color(.8f, .75f, .6f), .04f);
            Trigger("corto-final", root, Pos(2, 7) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            // Camino inundado: agua profunda que hace ruido, trampas bajo el agua, objetos que suenan y un derrumbe.
            foreach (var t in new[] { new Vector2Int(9, 3), new Vector2Int(6, 1), new Vector2Int(3, 1) }) TrapAt(root, Pos(t.x, t.y) + new Vector3(.6f, 0, 0));
            Noisy(root, Pos(9, 4) + new Vector3(1.2f, 0, 0), "latas");
            Noisy(root, Pos(5, 1), "latas");
            var rubble = Group("Derrumbe", root);
            rubble.localPosition = Pos(1, 1);
            for (int i = 0; i < 9; i++) Box("Escombro", rubble, new Vector3(-1.6f + (i % 3) * .9f, .5f + (i / 3) * .9f, (i % 2 - .5f) * 2), new Vector3(1.4f, 1.1f, 1.6f), "Concreto").transform.localRotation = Quaternion.Euler(i * 13, i * 31, i * 7);
            Trigger("inundado-final", root, Pos(2, 1) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            // Camino largo: tuberias por todas partes, escondites y objetos ruidosos. Aqui patrulla la criatura.
            Trigger("camino-largo", root, Pos(12, 5) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            foreach (var p in new[] { (12, 4, 8), (14, 2, 1), (17, 5, 2), (16, 8, 4), (15, 5, 8) }) HidingPipe(root, Pos(p.Item1, p.Item2), p.Item3);
            Noisy(root, Pos(13, 2), "latas");
            Noisy(root, Pos(17, 3), "latas");
            Noisy(root, Pos(15, 8), "latas");
            foreach (var c in new[] { new Vector2Int(12, 2), new Vector2Int(17, 2), new Vector2Int(17, 8), new Vector2Int(9, 1) }) {
                PointLight("Luz de emergencia", root, Pos(c.x, c.y) + Vector3.up * (TunnelHeight - .5f), new Color(.5f, .95f, .6f), 10, 1.8f, true);
                Lamp("Lampara_Jaula", root, Pos(c.x, c.y) + Vector3.up * TunnelHeight, "LuzVerde", 74);
            }
            Trigger("suministros", root, Pos(14, 9) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            // Suministros abandonados con la inyeccion de adrenalina.
            var supply = Group("Suministros", root);
            supply.localPosition = Pos(14, 10);
            for (int i = 0; i < 4; i++) Box("Caja", supply, new Vector3(-2 + (i % 2) * .9f, .35f + (i / 2) * .7f, -1.8f), new Vector3(.8f, .7f, .8f), "Oxido");
            Box("Mesa", supply, new Vector3(-1.6f, .45f, 1.5f), new Vector3(1.2f, .9f, .7f), "Mesa");
            var syringe = Cylinder("Inyeccion de adrenalina", supply, new Vector3(-1.6f, .95f, 1.5f), .03f, .2f, new Vector3(0, 0, 90), "Jeringa");
            var syringeCollider = syringe.AddComponent<BoxCollider>();
            syringeCollider.isTrigger = true;
            syringeCollider.size = new Vector3(40, 8, 40);
            var adrenaline = Usable(syringe, "Coger la inyeccion de adrenalina", "adrenalina");
            adrenaline.give = "adrenalina";
            adrenaline.hideOnUse = true;
            PointLight("Luz de suministros", supply, new Vector3(-1, 2.8f, 1), new Color(1, .8f, .55f), 7, 1.6f, true);
            Trigger("conducto-entrada", root, Pos(16, 10) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            var duct = Point("Abertura del conducto", root, Pos(17, 10) + new Vector3(-Cell / 2 + .3f, .6f, 0));

            // Salida: una seccion mas amplia de la alcantarilla.
            Trigger("conducto", root, Pos(20, 10) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            PointLight("Luz al otro lado", root, Pos(20, 10) + Vector3.up * 3, new Color(.6f, .75f, .9f), 12, 2);

            var drips = Particles("Goteo", root, new Vector3(8, 8, 1), 40, 0, .015f, 1.2f, "Lluvia");
            drips.gameObject.AddComponent<FollowCamera>().offset = new Vector3(0, 3.3f, 3);
            var dripMain = drips.main;
            dripMain.gravityModifier = 1;

            var player = Player(root, true, new Color(.008f, .015f, .01f));
            player.floorNoise = 1.3f;
            Spawn("cruce", root, Pos(8, 9) + new Vector3(0, .05f, 1), 0);
            Spawn("suministros", root, Pos(14, 9) + new Vector3(0, .05f, 0), 180);

            var creature = Creature(root, Pos(17, 5), false);
            creature.patrolArea = OpenCells(PathsMap).Where(c => c.x >= 12 && c.y <= 9 && PathsMap[c.y][c.x] == '.').ToArray();
            var events = new GameObject("Eventos de los tres caminos").AddComponent<Chapter2Events>();
            events.transform.SetParent(root, false);
            events.creature = creature;
            events.ambushFrom = Point("Aparicion en el camino corto", root, Pos(6, 7));
            events.chaseFrom = Point("Aparicion en la persecucion", root, Pos(17, 8));
            events.shortMark = shortMark;
            events.floodMark = floodMark;

            var story = Systems(root);
            var arrive = Step("Mira el viejo plano del cruce", "plano",
                "Detras de el, algo golpeo la puerta de mantenimiento.",
                "Las alcantarillas formaban un laberinto. Los tuneles se dividian y ya no tenia un mapa confiable.");
            arrive.marker = map.transform;
            arrive.checkpoint = true;
            arrive.checkpointSpawn = "cruce";
            var plan = Step("Elige un camino", "suministros",
                "Uno parecia corto y llevaba directo a una salida marcada en el plano.",
                "Otro atravesaba una zona inundada. Bajo el agua podia haber trampas.",
                "El tercero era mas largo y estaba casi borrado del plano.");
            plan.dialogue = true;
            plan.checkpoint = true;
            plan.checkpointSpawn = "cruce";
            var supplies = Step("Busca algo util entre los suministros abandonados", "adrenalina", "Al final del tunel, cajas abandonadas.");
            supplies.marker = syringe.transform;
            supplies.checkpoint = true;
            supplies.checkpointSpawn = "suministros";
            var run = Step("¡Corre! Busca una abertura en la pared", "conducto", "La criatura aparecio de repente.");
            run.marker = duct;
            run.completeChapterTitle = "Fin del capitulo 2";
            story.steps = new List<ObjectiveStep> { arrive, plan, supplies, run };
        });
    }

    // Musgo, baba y tuberias extra: la alcantarilla debe sentirse humeda y organica.
    static void OrganicDressing(Transform cell, int open, bool chamber, System.Random rng) {
        float height = chamber ? ChamberHeight : TunnelHeight;
        for (int side = 0; side < 4; side++) {
            if ((open & (1 << side)) != 0) continue;
            var dir = Direction(side);
            var along = new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x));
            var face = dir * (Cell / 2 - Wall - .02f);
            float r1 = (float)rng.NextDouble(), r2 = (float)rng.NextDouble(), r3 = (float)rng.NextDouble();
            Box("Musgo", cell, face + Vector3.up * (.6f + r1 * 1.4f) + along * (r2 - .5f) * 3, along * (1 + r3 * 1.5f) + Abs(dir) * .03f + Vector3.up * (.5f + r1), "Moho", false);
            Box("Baba", cell, face + Vector3.up * (height - .9f) + along * (r3 - .5f) * 2, along * .3f + Abs(dir) * .04f + Vector3.up * 1.2f, "Baba", false);
            Cylinder("Tuberia baja", cell, face - dir * .25f + Vector3.up * .5f, .12f, Cell, along.x > 0 ? new Vector3(0, 0, 90) : new Vector3(90, 0, 0), "Metal");
            Cylinder("Tuberia vertical", cell, face - dir * .2f + along * 1.8f + Vector3.up * height / 2, .1f, height, Vector3.zero, "Oxido");
        }
        Cylinder("Tuberia del techo", cell, Vector3.up * (height - .35f) + new Vector3(.8f, 0, 0), .2f, Cell, new Vector3(90, 0, 0), "Oxido");
    }

    static void FloodCell(Transform cell) {
        Box("Agua profunda", cell, new Vector3(0, .45f, 0), new Vector3(Cell, .02f, Cell), "AguaProfunda", false);
        var zone = new GameObject("Zona de agua profunda");
        zone.transform.SetParent(cell, false);
        zone.transform.localPosition = Vector3.up;
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(Cell, 2, Cell);
        var z = zone.AddComponent<NoiseZone>();
        z.noiseMultiplier = 2.2f;
        z.speedMultiplier = .7f;
    }

    // Corredor estrecho de la persecucion (de este a oeste): paredes que lo estrechan, escalones rotos y una tuberia baja.
    static void NarrowCell(Transform cell) {
        for (int k = -1; k <= 1; k += 2) Box("Muro estrecho", cell, new Vector3(0, TunnelHeight / 2, k * 1.9f), new Vector3(Cell, TunnelHeight, 1.6f), "Muro");
        Box("Escalon roto", cell, new Vector3(-.8f, .12f, 0), new Vector3(1.2f, .24f, 2.2f), "Concreto");
        Box("Escalon roto", cell, new Vector3(.6f, .06f, .3f), new Vector3(.8f, .12f, 1.4f), "Concreto").transform.localRotation = Quaternion.Euler(0, 0, 6);
        Cylinder("Tuberia baja", cell, new Vector3(1.5f, 1.95f, 0), .14f, 2.2f, new Vector3(90, 0, 0), "Oxido");
    }

    // Conducto pequeno (de este a oeste): solo se cabe agachado; la criatura no puede seguir.
    static void DuctCell(Transform cell, bool westEnd, bool eastEnd) {
        const float duct = 1.25f;
        Box("Suelo del conducto", cell, new Vector3(0, -.2f, 0), new Vector3(Cell, .4f, Cell), "Suelo");
        Box("Techo del conducto", cell, new Vector3(0, duct + .2f, 0), new Vector3(Cell, .4f, Cell), "Oxido");
        for (int k = -1; k <= 1; k += 2) Box("Pared del conducto", cell, new Vector3(0, duct / 2, k * 1.9f), new Vector3(Cell, duct + .1f, 2.2f), "Oxido");
        foreach (var (end, x) in new[] { (westEnd, -Cell / 2 + .2f), (eastEnd, Cell / 2 - .2f) })
            if (end) Box("Dintel", cell, new Vector3(x, duct + (TunnelHeight - duct) / 2 + .2f, 0), new Vector3(.4f, TunnelHeight - duct, Cell), "Muro");
        Box("Baba en el conducto", cell, new Vector3(0, .02f, 0), new Vector3(Cell * .8f, .01f, .9f), "Baba", false);
    }

    static void TrapAt(Transform parent, Vector3 position) {
        var trap = Group("Trampa para animales", parent);
        trap.localPosition = position + Vector3.up * .05f;
        var left = Group("Mandibula", trap);
        left.localPosition = new Vector3(-.25f, .05f, 0);
        Box("Dientes", left, new Vector3(-.15f, 0, 0), new Vector3(.3f, .04f, .5f), "Metal", false);
        var right = Group("Mandibula", trap);
        right.localPosition = new Vector3(.25f, .05f, 0);
        Box("Dientes", right, new Vector3(.15f, 0, 0), new Vector3(.3f, .04f, .5f), "Metal", false);
        var box = trap.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.2f, 1, 1.2f);
        box.center = Vector3.up * .5f;
        var t = trap.gameObject.AddComponent<Trap>();
        t.jawLeft = left;
        t.jawRight = right;
    }

    // Tuberia ancha contra una pared (lado 1 norte, 2 este, 4 sur, 8 oeste): el jugador se mete agachado.
    static void HidingPipe(Transform parent, Vector3 cellCenter, int wallSide) {
        int side = wallSide switch { 1 => 0, 2 => 1, 4 => 2, _ => 3 };
        var dir = Direction(side);
        var pipe = Group("Tuberia para esconderse", parent);
        pipe.localPosition = cellCenter + dir * (Cell / 2 - Wall - .8f);
        pipe.localRotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up));
        for (int i = 0; i < 8; i++) {
            float a = i * 45 * Mathf.Deg2Rad;
            if (Mathf.Sin(a) < -.6f) continue;
            var slab = Box("Pared de la tuberia", pipe, new Vector3(Mathf.Cos(a) * .7f, .7f + Mathf.Sin(a) * .7f, 0), new Vector3(.55f, .08f, 2.4f), "Oxido");
            slab.transform.localRotation = Quaternion.Euler(0, 0, i * 45 + 90);
        }
        Hide(pipe, new Vector3(0, .6f, 0), new Vector3(1, 1.1f, 2.2f), true, "en la tuberia");
    }
}
