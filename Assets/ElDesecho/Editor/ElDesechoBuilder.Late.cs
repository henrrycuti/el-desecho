using System.Collections.Generic;
using System.Linq;
using ElDesecho;
using UnityEditor;
using UnityEngine;

// Capitulos 3 a 5 y epilogo.
public static partial class ElDesechoBuilder {
    // # roca, . tunel, C camara grande, M sala de mantenimiento (seca), V camara de vigilancia (gafas),
    // B cuerpo junto a la compuerta, R puerta reforzada (salida), S llegada desde el conducto.
    static readonly string[] InvestigationMap = {
        "###################",
        "#S....#CCC.....MM##",
        "####.##CCC.###.MM.#",
        "#....##CCC.#V#....#",
        "#.####...#.#.####.#",
        "#......#...#.....##",
        "###.#.#####.###.###",
        "#B..#.......#.....#",
        "###.#.#####.#.###.#",
        "#...#.....#...#R..#",
        "###################",
    };

    static void MakeLateMaterials() {
        Lit("Polvo", new Color(.32f, .29f, .24f), .15f);
        Lit("CementoSeco", new Color(.36f, .34f, .3f), .2f);
        Lit("Uniforme", new Color(.16f, .2f, .12f), .3f);
        Emissive("Amanecer", new Color(1, .82f, .6f), 3);
        Lit("Papel", new Color(.85f, .82f, .74f), .1f);
        Lit("Cinta", new Color(.9f, .75f, .1f), .4f);
    }

    // Construye celdas de alcantarilla a partir de un mapa. "dry" son los caracteres de suelo seco (sin agua).
    static void SewerFromMap(Transform root, string[] map, string dry, System.Random rng, System.Action<Transform, char, int> extra = null) {
        int w = map[0].Length, h = map.Length;
        bool Open(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && map[y][x] != '#';
        char At(int x, int y) => Open(x, y) ? map[y][x] : '#';
        var tunnels = Group("Alcantarilla", root);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) {
                if (!Open(x, y)) continue;
                char k = At(x, y);
                int open = (Open(x, y - 1) ? 1 : 0) | (Open(x + 1, y) ? 2 : 0) | (Open(x, y + 1) ? 4 : 0) | (Open(x - 1, y) ? 8 : 0);
                var cell = Group($"Celda {x},{y}", tunnels);
                cell.localPosition = new Vector3(x * Cell, 0, -y * Cell);
                bool chamber = k == 'C';
                int chamberSides = chamber ? (At(x, y - 1) == 'C' ? 1 : 0) | (At(x + 1, y) == 'C' ? 2 : 0) | (At(x, y + 1) == 'C' ? 4 : 0) | (At(x - 1, y) == 'C' ? 8 : 0) : 0;
                SewerCell(cell, open, chamber, chamberSides);
                if (dry.IndexOf(k) >= 0) {
                    var water = cell.Find("Agua");
                    if (water != null) Object.DestroyImmediate(water.gameObject);
                    Box("Suelo seco", cell, new Vector3(0, .01f, 0), new Vector3(Cell - .1f, .02f, Cell - .1f), "CementoSeco", false);
                } else OrganicDressing(cell, open, chamber, rng);
                extra?.Invoke(cell, k, open);
            }
    }

    // Tuberia para esconderse contra el primer lado cerrado de una celda.
    static void PipeAt(Transform root, string[] map, int x, int y) {
        bool Closed(int cx, int cy) => cy < 0 || cy >= map.Length || cx < 0 || cx >= map[0].Length || map[cy][cx] == '#';
        int side = Closed(x, y - 1) ? 1 : Closed(x + 1, y) ? 2 : Closed(x, y + 1) ? 4 : Closed(x - 1, y) ? 8 : 0;
        if (side == 0) throw new System.Exception($"La celda {x},{y} no tiene pared para la tuberia");
        HidingPipe(root, new Vector3(x * Cell, 0, -y * Cell), side);
    }

    // ===================== CAPITULO 3: LO QUE QUEDA DE LA INVESTIGACION =====================
    static void BuildInvestigation() {
        NewScene("C3_Investigacion", root => {
            Atmosphere(new Color(.04f, .055f, .045f), new Color(.01f, .016f, .012f), .045f);
            var map = InvestigationMap;
            Vector3 Pos(int x, int y) => new(x * Cell, 0, -y * Cell);
            Grid(root, map, Cell, "");
            var rng = new System.Random(11);
            SewerFromMap(root, map, "MV", rng);

            // Salas de mantenimiento: secas, con mesas de herramientas y casilleros.
            foreach (var c in OpenCells(map).Where(c => map[c.y][c.x] == 'M')) {
                var room = Point("Mantenimiento", root, Pos(c.x, c.y));
                Box("Banco de herramientas", room, new Vector3(-1.9f, .5f, 1.9f), new Vector3(1.2f, 1, .8f), "Mesa");
                PointLight("Luz de mantenimiento", room, Vector3.up * 3, new Color(1, .85f, .6f), 8, 1.4f, true);
            }
            Locker(root, Pos(16, 1) + new Vector3(0, 0, 1.9f), 180);
            Locker(root, Pos(17, 2) + new Vector3(1.9f, 0, 0), -90);

            // Camara de vigilancia: monitores rotos y las gafas de exploracion.
            var watch = Point("Camara de vigilancia", root, Pos(12, 3));
            for (int i = 0; i < 3; i++) Box("Monitor", watch, new Vector3(-1.2f + i * 1.2f, 1.5f, 2.3f), new Vector3(1, .7f, .1f), i == 1 ? "Pantalla" : "Ventana", false);
            Box("Consola", watch, new Vector3(0, .5f, 1.9f), new Vector3(3.4f, 1, .6f), "Mesa");
            var goggles = Box("Gafas de exploracion", watch, new Vector3(.6f, 1.05f, 1.8f), new Vector3(.3f, .08f, .14f), "Pantalla", false);
            var gogglesCollider = goggles.AddComponent<BoxCollider>();
            gogglesCollider.isTrigger = true;
            gogglesCollider.size = new Vector3(6, 20, 10);
            var take = Usable(goggles, "Coger las gafas de exploracion", "gafas");
            take.give = "gafas";
            take.hideOnUse = true;
            PointLight("Pantalla encendida", watch, new Vector3(0, 1.6f, 1.6f), new Color(.3f, .6f, 1), 5, 1.4f, true);
            Trigger("vigilancia", root, Pos(12, 4) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            // El cuerpo atrapado junto a la compuerta, con la llave, el antidoto y el documento.
            var gate = Point("Compuerta", root, Pos(1, 7) + new Vector3(0, 0, -(Cell / 2 - Wall - .2f)));
            Gate(gate);
            var body = Point("Cuerpo atrapado", root, Pos(1, 7) + new Vector3(0, 0, -1.6f));
            Corpse(body, 90);
            Box("Frasco verde", body, new Vector3(.5f, .12f, .4f), new Vector3(.1f, .22f, .1f), "Liquido", false);
            Box("Documento", body, new Vector3(-.4f, .02f, .5f), new Vector3(.3f, .01f, .4f), "Papel", false);
            var examine = new GameObject("Examinar el cuerpo");
            examine.transform.SetParent(body, false);
            examine.transform.localPosition = Vector3.up * .5f;
            var examineBox = examine.AddComponent<BoxCollider>();
            examineBox.isTrigger = true;
            examineBox.size = new Vector3(1.8f, 1.2f, 1.8f);
            var search = Usable(examine, "Examinar el cuerpo", "cuerpo");
            search.give = "llave,antidoto";
            var bodyMark = examine.AddComponent<XRayTarget>();
            bodyMark.kind = XRayKind.Item;
            bodyMark.label = "Cuerpo";
            bodyMark.offset = Vector3.zero;

            // Puerta reforzada: solo se abre con la llave.
            var door = Point("Puerta reforzada", root, Pos(15, 9) + new Vector3(0, 0, -(Cell / 2 - Wall)));
            Box("Marco", door, new Vector3(0, 1.5f, 0), new Vector3(3.2f, 3, .4f), "Metal");
            var leaf = Box("Hoja reforzada", door, new Vector3(0, 1.3f, .25f), new Vector3(2.2f, 2.6f, .2f), "Puerta");
            Usable(leaf, "Abrir con la llave", "puerta-reforzada");
            TextSign("ACCESO SECTOR B", door, new Vector3(0, 2.9f, .38f), 180, new Color(.9f, .85f, .7f), .035f);
            var doorMark = leaf.AddComponent<XRayTarget>();
            doorMark.kind = XRayKind.Item;
            doorMark.label = "Puerta";
            doorMark.offset = Vector3.zero;

            // Escondites, ruido y luces.
            foreach (var p in new[] { (4, 3), (3, 5), (10, 4), (7, 7), (16, 7), (12, 9) }) PipeAt(root, map, p.Item1, p.Item2);
            var walkway = Point("Pasarela", root, Pos(8, 1) + new Vector3(0, .85f, 2.3f));
            Walkway(walkway, 9);
            Hide(walkway, new Vector3(0, -.45f, 0), new Vector3(8.2f, .8f, 1.2f), true, "bajo la pasarela");
            foreach (var p in new[] { (5, 5), (9, 7), (14, 5), (8, 9) }) Noisy(root, Pos(p.Item1, p.Item2) + new Vector3(1, 0, 1), "latas");
            foreach (var p in new[] { (4, 1), (8, 3), (2, 5), (11, 7), (6, 9), (17, 3) }) {
                PointLight("Luz de emergencia", root, Pos(p.Item1, p.Item2) + Vector3.up * (TunnelHeight - .5f), new Color(.5f, .95f, .6f), 10, 1.6f, true);
                Box("Lampara de emergencia", root, Pos(p.Item1, p.Item2) + Vector3.up * (TunnelHeight - .25f), new Vector3(.4f, .12f, .2f), "LuzVerde", false);
            }
            var drips = Particles("Goteo", root, new Vector3(8, 8, 1), 40, 0, .015f, 1.2f, "Lluvia");
            drips.gameObject.AddComponent<FollowCamera>().offset = new Vector3(0, 3.3f, 3);
            var dripMain = drips.main;
            dripMain.gravityModifier = 1;

            var player = Player(root, true, new Color(.01f, .016f, .012f));
            player.floorNoise = 1.3f;
            Spawn("llegada", root, Pos(1, 1) + Vector3.up * .05f, 90);
            Spawn("vigilancia", root, Pos(12, 4) + Vector3.up * .05f, 0);
            Spawn("cuerpo", root, Pos(2, 7) + Vector3.up * .05f, 90);

            // Tres masas pequenas, cada una en su zona.
            var zones = new (Vector2Int at, System.Func<Vector2Int, bool> area)[] {
                (new Vector2Int(8, 2), c => c.x >= 6 && c.x <= 11 && c.y <= 5),
                (new Vector2Int(6, 7), c => c.y >= 5 && c.x <= 11),
                (new Vector2Int(16, 5), c => c.x >= 12),
            };
            foreach (var z in zones) {
                var small = Creature(root, Pos(z.at.x, z.at.y), false, .55f);
                small.patrolArea = OpenCells(map).Where(z.area).ToArray();
            }

            var story = Systems(root);
            var arrive = Step("Busca una salida en la nueva seccion", "vigilancia",
                "Al otro lado cayo en una seccion mas amplia de las alcantarillas, conectada con tuberias y salas de mantenimiento.",
                "Podia orientarse mejor... pero algo se movia entre los conductos. Mas de una cosa. Reaccionaban a los ruidos.");
            arrive.checkpoint = true;
            arrive.checkpointSpawn = "llegada";
            var cam = Step("Coge las gafas de exploracion", "gafas",
                "Una camara de vigilancia abandonada. Monitores rotos... y en la consola, unas gafas de exploracion.");
            cam.marker = goggles.transform;
            var use = Step("Usa las gafas (G) para encontrar lo que mostraba la camara", "cuerpo",
                "Las gafas ven a traves de algunas paredes durante unos segundos. La bateria se agota rapido.",
                "Si la vacias del todo, tarda mucho en recargarse. Cada uso debe servir para decidir.",
                "En el ultimo monitor se veia un cuerpo atrapado junto a una compuerta.");
            use.checkpoint = true;
            use.checkpointSpawn = "vigilancia";
            var read = Step("Lee el documento", "",
                "Junto al cadaver: la llave de la puerta reforzada, un frasco de color verde y un documento.",
                "\"Prueba de antidoto: neutralizacion temporal del compuesto.\"",
                "\"El liquido no puede matar a las criaturas, pero si detenerlas durante un tiempo.\"",
                "\"El efecto es breve y el frasco tarda en estar listo para volver a usarse.\"",
                "Guardo el antidoto. Si lo reservaba para el momento adecuado, podria ayudarle a escapar.");
            read.dialogue = true;
            var open = Step("Abre la puerta reforzada con la llave", "puerta-reforzada",
                "Antidoto: pulsa Q (o ANTIDOTO) para detener a una criatura cercana unos segundos.");
            open.marker = leaf.transform;
            open.checkpoint = true;
            open.checkpointSpawn = "cuerpo";
            open.completeChapterTitle = "Fin del capitulo 3";
            story.steps = new List<ObjectiveStep> { arrive, cam, use, read, open };
        });
    }

    // # roca, S llegada, ~ sector inundado, p sector seco con polvo, v sector de vapor, H puerta pesada.
    static readonly string[] ExitMap = {
        "#####################",
        "#S~~~~#~~~~~~~#~~~~~#",
        "#~##~~#~###~#~#~###~#",
        "#~~~~~~~~~~~#~~~~~#~#",
        "###################p#",
        "#pppppppp#ppppp#pppp#",
        "#p####p###p###p#p##p#",
        "#pppp#pppppppppppp#p#",
        "#p###################",
        "#vvvv#vvvvvvv#vvvvvv#",
        "#v##vvv###v###v#v#v##",
        "#vvv#vv#vvvvvvv#vvvH#",
        "#####################",
    };

    static readonly Vector2Int[] Steps4 = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    static ShadowFigure Silhouette(Transform root, Vector3 position) {
        var holder = Point("Silueta al fondo", root, position);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreatureSetup.PrefabPath);
        var figure = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
        figure.transform.localScale = Vector3.one * .6f;
        figure.SetActive(false);
        var lamp = PointLight("Luz que parpadea", holder, new Vector3(0, TunnelHeight - .4f, 0), new Color(.5f, .95f, .6f), 9, .3f);
        var shadow = holder.gameObject.AddComponent<ShadowFigure>();
        shadow.figure = figure;
        shadow.lamp = lamp;
        return shadow;
    }

    // ===================== CAPITULO 4: LA PUERTA DE SALIDA =====================
    static void BuildExitDoor() {
        NewScene("C4_PuertaSalida", root => {
            Atmosphere(new Color(.035f, .045f, .04f), new Color(.012f, .014f, .012f), .05f);
            var map = ExitMap;
            int w = map[0].Length, h = map.Length;
            bool Open(int x, int y) => y >= 0 && y < h && x >= 0 && x < w && map[y][x] != '#';
            Vector3 Pos(int x, int y) => new(x * Cell, 0, -y * Cell);
            Grid(root, map, Cell, "");
            var rng = new System.Random(23);
            SewerFromMap(root, map, "pvH", rng, (cell, k, open) => {
                if (k == '~') FloodCell(cell);
                if (k == 'p') {
                    Box("Polvo", cell, new Vector3(0, .02f, 0), new Vector3(Cell - .4f, .01f, Cell - .4f), "Polvo", false);
                    Box("Escombro", cell, new Vector3(1.6f, .2f, -1.7f), new Vector3(.7f, .4f, .5f), "Concreto");
                }
                if (k == 'v') Cylinder("Tuberia de vapor", cell, new Vector3(-1.9f, 2.6f, 0), .25f, Cell, new Vector3(90, 0, 0), "Metal");
            });
            var start = new Vector2Int(1, 1);
            var door = new Vector2Int(19, 11);
            var path = ShortestPath(start, door, Open);
            var onPath = new HashSet<Vector2Int>(path);

            // Pasillos sin salida: escombros y una marca que solo se ve con las gafas.
            var deadEnds = OpenCells(map).Where(c => !onPath.Contains(c) && Steps4.Count(d => Open(c.x + d.x, c.y + d.y)) == 1).ToList();
            foreach (var d in deadEnds) {
                var rubble = Point("Pasillo bloqueado", root, Pos(d.x, d.y));
                for (int i = 0; i < 4; i++) Box("Escombro", rubble, new Vector3((i % 2 - .5f) * 1.4f, .5f + (i / 2) * .8f, 0), new Vector3(1.3f, 1, 1.4f), "Concreto").transform.localRotation = Quaternion.Euler(i * 17, i * 40, 0);
                var mark = rubble.gameObject.AddComponent<XRayTarget>();
                mark.kind = XRayKind.Blocked;
                mark.label = "Bloqueado";
                mark.offset = Vector3.up * 1.5f;
            }
            foreach (var d in deadEnds.Take(3)) Silhouette(root, Pos(d.x, d.y) + new Vector3(0, 0, 0));

            // Vapor: chorros periodicos en la mitad de las celdas de vapor de la ruta.
            int steamIndex = 0;
            foreach (var c in path.Where(c => map[c.y][c.x] == 'v')) {
                if (steamIndex++ % 2 == 1) continue;
                var jet = new GameObject("Chorro de vapor");
                jet.transform.SetParent(root, false);
                jet.transform.localPosition = Pos(c.x, c.y) + Vector3.up * 1.2f;
                var box = jet.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(3, 2.4f, 3);
                var steam = Particles("Vapor", jet.transform, new Vector3(1.5f, 1.5f, 1), 0, 3, .6f, 1, "Lluvia");
                steam.transform.localPosition = Vector3.up * 2.2f;
                var sm = steam.main;
                sm.startColor = new Color(.85f, .9f, .9f, .25f);
                steam.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
                var steamJet = jet.AddComponent<SteamJet>();
                steamJet.steam = steam;
                steamJet.offset = steamIndex * 1.3f;
                Box("Boquilla", jet.transform, new Vector3(0, 1.3f, 0), new Vector3(.4f, .2f, .4f), "Oxido", false);
                PointLight("Brillo del vapor", jet.transform, new Vector3(0, 1.6f, 0), new Color(1, .55f, .3f), 9, 1.8f, true);
            }

            // Polvo flotando en el sector seco.
            foreach (var c in path.Where(c => map[c.y][c.x] == 'p').Where((c, i) => i % 4 == 0)) {
                var dust = Particles("Polvo en el aire", root, new Vector3(5, 5, 1), 6, .1f, .05f, 6, "Lluvia");
                dust.transform.localPosition = Pos(c.x, c.y) + Vector3.up * 2;
                var dm = dust.main;
                dm.startColor = new Color(.7f, .65f, .55f, .2f);
                dust.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            }

            // Puerta pesada con mecanismo antiguo.
            var heavy = Point("Puerta pesada", root, Pos(door.x, door.y) + new Vector3(Cell / 2 - Wall, 0, 0));
            Box("Hoja de la puerta pesada", heavy, new Vector3(-.15f, 1.5f, 0), new Vector3(.3f, 3, 3.2f), "Puerta");
            Cylinder("Rueda del mecanismo", heavy, new Vector3(-.4f, 1.5f, 0), .55f, .12f, new Vector3(0, 0, 90), "Oxido");
            for (int i = 0; i < 4; i++) Box("Radio", heavy, new Vector3(-.42f, 1.5f, 0), new Vector3(.06f, 1, .06f), "Oxido", false).transform.localRotation = Quaternion.Euler(i * 45, 0, 0);
            PointLight("Corriente de aire frio", heavy, new Vector3(-1.2f, 2.6f, 0), new Color(.7f, .8f, 1), 6, 1.2f);
            var heavyMark = heavy.gameObject.AddComponent<XRayTarget>();
            heavyMark.kind = XRayKind.Item;
            heavyMark.label = "Puerta";
            heavyMark.offset = Vector3.up * 1.5f;

            foreach (var c in path.Where((c, i) => i % 9 == 4 && map[c.y][c.x] != 'H')) {
                PointLight("Luz de emergencia", root, Pos(c.x, c.y) + Vector3.up * (TunnelHeight - .5f), new Color(.5f, .95f, .6f), 10, 1.6f, true);
                Box("Lampara de emergencia", root, Pos(c.x, c.y) + Vector3.up * (TunnelHeight - .25f), new Vector3(.4f, .12f, .2f), "LuzVerde", false);
            }
            foreach (var c in path.Where((c, i) => i % 11 == 6)) PipeAt(root, map, c.x, c.y);
            foreach (var c in path.Where((c, i) => i % 13 == 8)) Noisy(root, Pos(c.x, c.y) + new Vector3(1.2f, 0, 1.2f), "latas");
            new GameObject("Golpes en las paredes").AddComponent<WallKnocks>().transform.SetParent(root, false);

            var player = Player(root, true, new Color(.012f, .014f, .012f));
            player.floorNoise = 1.2f;
            Spawn("llegada", root, Pos(1, 1) + Vector3.up * .05f, 90);
            Spawn("seco", root, Pos(19, 5) + Vector3.up * .05f, 270);
            Spawn("vapor", root, Pos(1, 9) + Vector3.up * .05f, 90);

            var a = Creature(root, Pos(10, 3), false, .55f);
            a.patrolArea = OpenCells(map).Where(c => map[c.y][c.x] == '~').ToArray();
            var b = Creature(root, Pos(12, 7), false, .55f);
            b.patrolArea = OpenCells(map).Where(c => map[c.y][c.x] == 'p').ToArray();
            var big = Creature(root, Pos(10, 11), false);
            big.patrolArea = OpenCells(map).Where(c => map[c.y][c.x] == 'v').ToArray();

            Trigger("sector-seco", root, Pos(19, 5) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            Trigger("sector-vapor", root, Pos(1, 9) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            Trigger("puerta-pesada", root, Pos(door.x, door.y) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            var story = Systems(root);
            var flood = Step("Atraviesa el sector inundado", "sector-seco",
                "El camino hacia el exterior lo llevo a traves de varios sectores de mantenimiento.",
                "El primero estaba inundado. Cada paso en el agua sonaba demasiado.");
            flood.checkpoint = true;
            flood.checkpointSpawn = "llegada";
            var dry = Step("Cruza el sector seco", "sector-vapor",
                "Aqui el suelo estaba seco y cubierto de polvo. Las luces de emergencia se encendian y se apagaban, revelando movimiento al fondo de los pasillos.",
                "Usa las gafas para ver que pasillos estan bloqueados.");
            dry.checkpoint = true;
            dry.checkpointSpawn = "seco";
            var steamStep = Step("Atraviesa la sala de vapor sin quemarte", "puerta-pesada",
                "En estas salas, las tuberias soltaban chorros de vapor caliente. Habia que esperar el momento justo.");
            steamStep.checkpoint = true;
            steamStep.checkpointSpawn = "vapor";
            steamStep.marker = heavy;
            steamStep.completeChapterTitle = "Fin del capitulo 4";
            story.steps = new List<ObjectiveStep> { flood, dry, steamStep };
        });
    }

    // Persecucion final en zigzag: H puerta de inicio, j zanja (saltar), c cables bajos y k tuberia que cae (agacharse),
    // f masa que cierra el paso (antidoto), Z compuerta con la luz del amanecer.
    static readonly string[] DawnMap = {
        "#############",
        "#H..j...c...#",
        "###########.#",
        "#...k..f..j.#",
        "#.###########",
        "#..j...cc...#",
        "###########.#",
        "#Z...j..k...#",
        "#############",
    };

    // ===================== CAPITULO 5: LA LUZ DEL DIA =====================
    static void BuildDawn() {
        NewScene("C5_LuzDelDia", root => {
            Atmosphere(new Color(.06f, .03f, .03f), new Color(.02f, .006f, .006f), .04f);
            var map = DawnMap;
            Vector3 Pos(int x, int y) => new(x * Cell, 0, -y * Cell);
            Grid(root, map, Cell, "");
            var rng = new System.Random(5);
            SewerFromMap(root, map, "", rng, (cell, k, open) => {
                if (k == 'j') {
                    Object.DestroyImmediate(cell.Find("Suelo").gameObject);
                    var water = cell.Find("Agua");
                    if (water != null) Object.DestroyImmediate(water.gameObject);
                    for (int s = -1; s <= 1; s += 2) Box("Borde de la zanja", cell, new Vector3(s * 1.75f, -.2f, 0), new Vector3(2.5f, .4f, Cell), "Suelo");
                    var pit = new GameObject("Fondo de la zanja");
                    pit.transform.SetParent(cell, false);
                    pit.transform.localPosition = Vector3.down * 3;
                    var box = pit.AddComponent<BoxCollider>();
                    box.size = new Vector3(1, 2, Cell);
                    box.isTrigger = true;
                    pit.AddComponent<DeathZone>();
                    // Puente invisible que solo pisa la criatura (capa 10, que el jugador ignora).
                    var bridge = new GameObject("Puente de la criatura");
                    bridge.transform.SetParent(cell, false);
                    bridge.transform.localPosition = Vector3.down * .2f;
                    bridge.AddComponent<BoxCollider>().size = new Vector3(1, .4f, Cell);
                    bridge.layer = 10;
                }
                // Cables bajos: capa 11, que la criatura atraviesa.
                if (k == 'c') for (int i = 0; i < 2; i++) Cylinder("Cable bajo", cell, new Vector3(i * .5f - .25f, 1.25f + i * .1f, 0), .06f, Cell, new Vector3(90, 0, 0), "Zocalo", true).layer = 11;
            });
            int fall = 0;
            foreach (var c in OpenCells(map).Where(c => map[c.y][c.x] == 'k')) {
                var pipe = Cylinder("Tuberia que cae", root, Pos(c.x, c.y) + Vector3.up * 3.2f, .18f, Cell, new Vector3(90, 0, 0), "Oxido", true);
                pipe.layer = 11;
                var mover = pipe.AddComponent<EventMover>();
                mover.eventId = "cae-" + fall;
                mover.moveBy = Vector3.down * 1.85f;
                mover.duration = .35f;
                int dir = c.y == 3 || c.y == 7 ? 1 : -1;
                Trigger("cae-" + fall, root, Pos(c.x + dir * 2, c.y) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
                fall++;
            }
            foreach (var c in OpenCells(map).Where((c, i) => i % 3 == 1)) PointLight("Luz de alarma", root, Pos(c.x, c.y) + Vector3.up * (TunnelHeight - .5f), new Color(1, .15f, .1f), 11, 2.2f, true);

            // Puerta pesada del inicio y su mecanismo.
            var gate = Box("Compuerta pesada", root, Pos(1, 1) + new Vector3(2.75f, 1.8f, 0), new Vector3(.3f, 3.6f, 4.8f), "Puerta");
            var mover0 = gate.AddComponent<EventMover>();
            mover0.eventId = "puerta";
            mover0.moveBy = Vector3.up * 3.3f;
            mover0.duration = 2.5f;
            var wheel = Cylinder("Mecanismo antiguo", root, Pos(1, 1) + new Vector3(2.45f, 1.5f, -.8f), .5f, .12f, new Vector3(0, 0, 90), "Oxido");
            wheel.AddComponent<BoxCollider>().isTrigger = true;
            Usable(wheel, "Girar el mecanismo con la llave", "puerta");

            // Compuerta abierta con la luz del amanecer.
            var exit = Point("Salida al amanecer", root, Pos(1, 7));
            // Se abre el muro oeste de la ultima celda: detras se ve la luz del dia (antes quedaba tapada por el ladrillo).
            var exitCell = root.Find("Alcantarilla/Celda 1,7");
            if (exitCell != null)
                foreach (var t in exitCell.Cast<Transform>().ToList())
                    if (t.localPosition.x < -2f && t.localPosition.y > -.1f) Object.DestroyImmediate(t.gameObject);
            Box("Luz del amanecer", exit, new Vector3(-3.4f, 1.8f, 0), new Vector3(.1f, 3.8f, 6), "Amanecer");
            Box("Marco de la compuerta", exit, new Vector3(-2.9f, 3.5f, 0), new Vector3(.4f, .3f, 5.4f), "Oxido");
            PointLight("Resplandor del amanecer", exit, new Vector3(-1.8f, 1.8f, 0), new Color(1, .8f, .55f), 16, 5);
            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.transform.SetParent(exit, false);
            sun.transform.localPosition = new Vector3(-2, 2.5f, 0);
            sun.transform.localRotation = Quaternion.Euler(10, 90, 0);
            sun.type = LightType.Spot;
            sun.spotAngle = 100;
            sun.range = 30;
            sun.intensity = 8;
            sun.color = new Color(1, .82f, .6f);
            sun.shadows = LightShadows.None;
            Trigger("salida", root, Pos(1, 7) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            var player = Player(root, true, new Color(.02f, .006f, .006f));
            player.floorNoise = 1.2f;
            Spawn("puerta", root, Pos(1, 1) + Vector3.up * .05f, 90);
            Spawn("tramo2", root, Pos(11, 2) + Vector3.up * .05f, 180);
            Spawn("tramo3", root, Pos(1, 4) + Vector3.up * .05f, 180);
            Spawn("tramo4", root, Pos(11, 6) + Vector3.up * .05f, 180);

            var hunter = Creature(root, Pos(1, 1), true);
            hunter.chaseSpeed = 3.9f;
            hunter.giveUpSeconds = 999;
            var blocker = Creature(root, Pos(5, 3), true, .55f);
            blocker.giveUpSeconds = 999;
            foreach (var t in hunter.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 9;
            foreach (var t in blocker.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 9;
            var chase = new GameObject("Persecucion").AddComponent<Chapter5Chase>();
            chase.transform.SetParent(root, false);
            chase.hunter = hunter;
            chase.blocker = blocker;
            chase.segmentStarts = new[] { Point("Inicio tramo 1", root, Pos(1, 1)), Point("Inicio tramo 2", root, Pos(11, 1)), Point("Inicio tramo 3", root, Pos(1, 3)), Point("Inicio tramo 4", root, Pos(11, 5)) };
            chase.blockerAt = Point("Masa que cierra el paso", root, Pos(5, 3));
            chase.exitPoint = exit;
            chase.retreatTo = Point("La masa retrocede", root, Pos(6, 7));
            chase.door = mover0;
            Trigger("bloqueo", root, Pos(9, 3) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            Trigger("tramo2", root, Pos(11, 2) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            Trigger("tramo3", root, Pos(1, 4) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));
            Trigger("tramo4", root, Pos(11, 6) + Vector3.up * 1.5f, new Vector3(Cell, 3, Cell));

            var story = Systems(root);
            var open = Step("Gira el mecanismo de la puerta", "puerta",
                "La llave encajaba en la cerradura. Tras la puerta, una corriente de aire frio. Mas alla estaba la salida.");
            open.marker = wheel.transform;
            open.checkpoint = true;
            open.checkpointSpawn = "puerta";
            var run1 = Step("¡Corre!", "tramo2", "Comenzo la ultima persecucion. Salta las zanjas (Espacio) y agachate bajo los cables (C).");
            run1.raiseOnStart = "tramo-1";
            run1.checkpoint = true;
            run1.checkpointSpawn = "puerta";
            var run2 = Step("¡Corre!", "tramo3", "Si una criatura te cierra el paso, usa el antidoto (Q).");
            run2.raiseOnStart = "tramo-2";
            run2.checkpoint = true;
            run2.checkpointSpawn = "tramo2";
            var run3 = Step("¡Corre!", "tramo4", "Adrenalina (R): recupera velocidad unos segundos. Solo tienes dos dosis.");
            run3.raiseOnStart = "tramo-3";
            run3.checkpoint = true;
            run3.checkpointSpawn = "tramo3";
            var run4 = Step("¡Corre hacia la luz!", "final", "Al final del tunel aparecio una compuerta abierta.");
            run4.raiseOnStart = "tramo-4";
            run4.checkpoint = true;
            run4.checkpointSpawn = "tramo4";
            run4.marker = exit;
            run4.completeChapterTitle = "Fin del capitulo 5";
            story.steps = new List<ObjectiveStep> { open, run1, run2, run3, run4 };
        });
    }

    // ===================== EPILOGO (Timeline + Cinemachine) =====================
    static void BuildEpilogue() {
        const string folder = "Assets/ElDesecho/Epilogue";
        System.IO.Directory.CreateDirectory(folder);
        var photos = new List<Material>();
        foreach (var name in new[] { "04-laboratorio", "08-laboratorio-rojo", "10-camino-largo" }) {
            string from = "Verification/" + name + ".png", to = folder + "/" + name + ".png";
            if (!System.IO.File.Exists(from)) continue;
            System.IO.File.Copy(from, to, true);
            AssetDatabase.ImportAsset(to);
            var m = Save("Foto " + name, new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(to));
            EditorUtility.SetDirty(m);
            photos.Add(m);
        }
        NewScene("E_Epilogo", root => {
            Atmosphere(new Color(.15f, .15f, .16f), new Color(.02f, .02f, .025f), .02f);
            var camObject = new GameObject("Camara del epilogo");
            camObject.tag = "MainCamera";
            camObject.transform.SetParent(root, false);
            var cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = .05f;
            camObject.AddComponent<AudioListener>();
            var brain = camObject.AddComponent<Unity.Cinemachine.CinemachineBrain>();

            Unity.Cinemachine.CinemachineCamera Shot(string name, Vector3 at, Vector3 look, Vector3 drift) {
                var g = new GameObject("Plano: " + name);
                g.transform.SetParent(root, false);
                g.transform.position = at;
                g.transform.rotation = Quaternion.LookRotation(look - at);
                g.AddComponent<CameraDrift>().velocity = drift;
                var vcam = g.AddComponent<Unity.Cinemachine.CinemachineCamera>();
                vcam.Lens.FieldOfView = 50;
                return vcam;
            }

            // 1. Las noticias.
            var news = Point("Noticias", root, Vector3.zero);
            Box("Mesa", news, new Vector3(0, .4f, 0), new Vector3(2.4f, .8f, 1.4f), "Mesa");
            Box("Periodico", news, new Vector3(0, .81f, 0), new Vector3(1.2f, .01f, .8f), "Papel", false);
            var headline = new GameObject("Titular");
            headline.transform.SetParent(news, false);
            headline.transform.localPosition = new Vector3(0, .83f, .25f);
            headline.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var text = headline.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            headline.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.text = "LABORATORIO SECRETO\nBAJO LA CIUDAD";
            text.color = Color.black;
            text.characterSize = .012f;
            text.fontSize = 64;
            text.anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < photos.Count; i++) {
                var photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                photo.name = "Fotografia";
                photo.transform.SetParent(news, false);
                photo.transform.localPosition = new Vector3(-.35f + i * .35f, .825f, -.15f);
                photo.transform.localRotation = Quaternion.Euler(90, 0, 0);
                photo.transform.localScale = new Vector3(.32f, .18f, 1);
                photo.GetComponent<Renderer>().sharedMaterial = photos[i];
                Object.DestroyImmediate(photo.GetComponent<Collider>());
            }
            PointLight("Lampara", news, new Vector3(.4f, 2, .3f), new Color(1, .9f, .7f), 6, 3);

            // 2. El laboratorio clausurado.
            var closed = Point("Laboratorio clausurado", root, new Vector3(100, 0, 0));
            Box("Pared", closed, new Vector3(0, 1.8f, 1), new Vector3(8, 3.6f, .3f), "PanelBlanco");
            Box("Puerta", closed, new Vector3(0, 1.25f, .8f), new Vector3(1.6f, 2.5f, .15f), "Puerta");
            for (int i = 0; i < 2; i++) Box("Cinta", closed, new Vector3(0, 1.3f, .7f), new Vector3(2.6f, .12f, .02f), "Cinta", false).transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 25 : -25);
            var sealedSign = new GameObject("Clausurado");
            sealedSign.transform.SetParent(closed, false);
            sealedSign.transform.localPosition = new Vector3(0, 2.8f, .6f);
            sealedSign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var sealedText = sealedSign.AddComponent<TextMesh>();
            sealedText.font = font;
            sealedSign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            sealedText.text = "CLAUSURADO";
            sealedText.color = new Color(.8f, .1f, .1f);
            sealedText.characterSize = .05f;
            sealedText.fontSize = 64;
            sealedText.anchor = TextAnchor.MiddleCenter;
            PointLight("Luz azul", closed, new Vector3(-2, 2, -1), new Color(.2f, .3f, 1), 8, 3, true);
            PointLight("Luz roja", closed, new Vector3(2, 2, -1), new Color(1, .1f, .1f), 8, 3, true);

            // 3. La institucion: el periodista internado.
            var ward = Point("Institucion", root, new Vector3(200, 0, 0));
            Box("Suelo", ward, new Vector3(0, -.1f, 0), new Vector3(5, .2f, 5), "Azulejo");
            Box("Pared", ward, new Vector3(0, 1.5f, 2.5f), new Vector3(5, 3, .2f), "PanelBlanco");
            Box("Pared", ward, new Vector3(-2.5f, 1.5f, 0), new Vector3(.2f, 3, 5), "PanelBlanco");
            Box("Cama", ward, new Vector3(-1.2f, .3f, 1.2f), new Vector3(1, .6f, 2), "Bata");
            Cylinder("Periodista", ward, new Vector3(-1.2f, 1.05f, .6f), .25f, 1, Vector3.zero, "Bata", true);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(ward, false);
            head.transform.localPosition = new Vector3(-1.2f, 1.75f, .6f);
            head.transform.localScale = Vector3.one * .26f;
            Finish(head, "Piel", false);
            PointLight("Luz de la celda", ward, new Vector3(0, 2.8f, 0), new Color(.8f, .85f, .9f), 6, 1.5f, true);

            // 4 y 5. Los soldados con el antidoto y la masa que crece.
            var tunnel = Point("Tuneles", root, new Vector3(300, 0, 0));
            for (int i = 0; i < 3; i++) {
                var c = Point("Tunel", tunnel, new Vector3(0, 0, i * Cell));
                SewerCell(c, 1 | 4, false, 0);
            }
            for (int i = 0; i < 4; i++) {
                var soldier = Point("Soldado", tunnel, new Vector3(-1.4f + i * .9f, 0, -1.2f - (i % 2) * .8f));
                Cylinder("Uniforme", soldier, new Vector3(0, .9f, 0), .25f, 1.4f, Vector3.zero, "Uniforme", true);
                var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                helmet.transform.SetParent(soldier, false);
                helmet.transform.localPosition = Vector3.up * 1.75f;
                helmet.transform.localScale = Vector3.one * .3f;
                Finish(helmet, "Uniforme", false);
                var lamp = new GameObject("Linterna").AddComponent<Light>();
                lamp.transform.SetParent(soldier, false);
                lamp.transform.localPosition = new Vector3(.2f, 1.4f, .2f);
                lamp.type = LightType.Spot;
                lamp.spotAngle = 40;
                lamp.range = 14;
                lamp.intensity = 4;
                lamp.shadows = LightShadows.None;
            }
            var spray = Particles("Antidoto", tunnel, new Vector3(1, 1, 1), 40, 6, .12f, 1.2f, "Lluvia");
            spray.transform.localPosition = new Vector3(0, 1.3f, -.6f);
            spray.transform.localRotation = Quaternion.identity;
            var sprayMain = spray.main;
            sprayMain.startColor = new Color(.3f, 1, .4f, .5f);
            spray.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreatureSetup.PrefabPath);
            PointLight("Luz del antidoto", tunnel, new Vector3(0, 2.6f, 7), new Color(.4f, 1, .5f), 10, 3.5f, true);
            PointLight("Luz de los soldados", tunnel, new Vector3(.5f, 3, 3), new Color(.9f, .95f, 1), 12, 3);
            PointLight("Luz sobre la boca", tunnel, new Vector3(0, 4.5f, 6.5f), new Color(1, .9f, .8f), 6, 2.5f);
            // Luces de cine para los planos cercanos de la masa (antes la criatura llenaba el plano y salia negra).
            foreach (var (pos, aim, color) in new[] {
                (new Vector3(1.2f, 2.2f, 3.4f), new Vector3(0, 1.6f, 8.5f), new Color(.75f, 1, .8f)),
                (new Vector3(-1, 3.2f, 5.6f), new Vector3(0, 3.4f, 8.5f), new Color(1, .9f, .8f)),
                (new Vector3(0, 2.5f, 11.5f), new Vector3(0, 1.8f, 8), new Color(.4f, 1, .5f)) }) {
                var key = new GameObject("Luz de cine").AddComponent<Light>();
                key.transform.SetParent(tunnel, false);
                key.transform.localPosition = pos;
                key.transform.localRotation = Quaternion.LookRotation(aim - pos);
                key.type = LightType.Spot;
                key.spotAngle = 60;
                key.range = 14;
                key.intensity = 7;
                key.color = color;
                key.shadows = LightShadows.None;
            }
            Transform grower = null;
            SkinnedMeshRenderer mouth = null;
            for (int i = 0; i < 3; i++) {
                var mass = (GameObject)PrefabUtility.InstantiatePrefab(prefab, tunnel);
                mass.transform.localPosition = new Vector3(-1.6f + i * 1.6f, 0, 7 + (i % 2) * 1.5f);
                mass.transform.localRotation = Quaternion.Euler(0, 180, 0);
                mass.transform.localScale = Vector3.one * .6f;
                if (i == 1) { grower = mass.transform; mouth = mass.GetComponentInChildren<SkinnedMeshRenderer>(); }
            }

            var shots = new[] {
                (Shot("Noticias", new Vector3(.2f, 1.9f, -1.1f), new Vector3(0, .8f, 0), new Vector3(0, 0, .03f)), 0f, 9f),
                (Shot("Clausurado", new Vector3(100, 1.6f, -4), new Vector3(100, 1.5f, 1), new Vector3(0, 0, .25f)), 9f, 7f),
                (Shot("Institucion", new Vector3(201.6f, 1.7f, -1.8f), new Vector3(198.8f, 1.3f, .6f), new Vector3(0, 0, .12f)), 16f, 9f),
                (Shot("Soldados", new Vector3(301.8f, 1.8f, -4), new Vector3(300, 1, 7), new Vector3(0, 0, .3f)), 25f, 9f),
                (Shot("La masa crece", new Vector3(301.6f, 2, -1.5f), new Vector3(300, 1.8f, 8.5f), new Vector3(-.05f, 0, .15f)), 34f, 7f),
                (Shot("La boca", new Vector3(300.8f, 2.6f, 1), new Vector3(300, 3.2f, 8.5f), new Vector3(0, 0, .12f)), 41f, 7f),
            };

            var directorObject = new GameObject("Linea de tiempo del epilogo");
            directorObject.transform.SetParent(root, false);
            var director = directorObject.AddComponent<UnityEngine.Playables.PlayableDirector>();
            director.playOnAwake = false;
            string timelinePath = folder + "/Epilogo.playable";
            AssetDatabase.DeleteAsset(timelinePath);
            var timeline = ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, timelinePath);
            var track = timeline.CreateTrack<Unity.Cinemachine.CinemachineTrack>(null, "Camaras");
            foreach (var (vcam, start, length) in shots) {
                var clip = track.CreateClip<Unity.Cinemachine.CinemachineShot>();
                clip.start = start;
                clip.duration = length;
                clip.displayName = vcam.name;
                var shot = (Unity.Cinemachine.CinemachineShot)clip.asset;
                string id = GUID.Generate().ToString();
                shot.VirtualCamera = new ExposedReference<Unity.Cinemachine.CinemachineVirtualCameraBase> { exposedName = id };
                director.SetReferenceValue(id, vcam);
                EditorUtility.SetDirty(shot);
            }
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            director.playableAsset = timeline;
            director.SetGenericBinding(track, brain);

            var epilogue = directorObject.AddComponent<EpilogueDirector>();
            epilogue.director = director;
            epilogue.grower = grower;
            epilogue.mouth = mouth;
            epilogue.roar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ElDesecho/Creature/Audio/Alert.wav");
            epilogue.lines = new[] {
                new EpilogueDirector.Line { at = .5f, text = "El periodista entrego las fotografias, la grabacion y los documentos a las autoridades." },
                new EpilogueDirector.Line { at = 4.6f, text = "Su investigacion llego a las noticias." },
                new EpilogueDirector.Line { at = 9.2f, text = "El laboratorio fue clausurado. Se desplegaron soldados y equipos cientificos para contener a las criaturas y estudiar el antidoto." },
                new EpilogueDirector.Line { at = 16.2f, text = "Pero el periodista nunca volvio a ser el mismo. Sonaba con los pasillos rojos, con la alarma, con la masa acercandose por las paredes." },
                new EpilogueDirector.Line { at = 21f, text = "Lo internaron. En las noches gritaba que las criaturas seguian alli y que alguien debia cerrar las alcantarillas." },
                new EpilogueDirector.Line { at = 25.2f, text = "Un grupo de soldados entra en los tuneles con el antidoto." },
                new EpilogueDirector.Line { at = 29.5f, text = "Al principio, el plan funciona: las criaturas quedan inmoviles." },
                new EpilogueDirector.Line { at = 34.2f, text = "Entonces una de las masas se sacude. Su forma cambia y empieza a crecer." },
                new EpilogueDirector.Line { at = 38.5f, text = "El antidoto ya no tiene efecto." },
            };
        });
    }
}
