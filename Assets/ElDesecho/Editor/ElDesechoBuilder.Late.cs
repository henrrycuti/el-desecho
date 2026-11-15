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
}
