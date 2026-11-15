using System.Collections.Generic;
using UnityEngine;

namespace ElDesecho {
// Mapa por celdas de un escenario (el mismo dibujo con el que se construyo). La criatura lo usa para moverse
// y la prueba automatica para caminar. '#' es pared; los caracteres de "playerOnly" son pasos donde
// la criatura no cabe (conductos, escondites).
public sealed class CellGrid : MonoBehaviour {
    public string[] rows = new string[0];
    public float cellSize = 6;
    public string playerOnly = "d";

    public static CellGrid Instance { get; private set; }
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public int Width => rows.Length > 0 ? rows[0].Length : 0;
    public int Height => rows.Length;

    public char At(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.y < Height && c.x < Width ? rows[c.y][c.x] : '#';

    public bool Walkable(Vector2Int c, bool creature) {
        char k = At(c);
        return k != '#' && (!creature || playerOnly.IndexOf(k) < 0);
    }

    public Vector3 Center(Vector2Int c) => transform.position + new Vector3(c.x * cellSize, 0, -c.y * cellSize);

    public Vector2Int ToCell(Vector3 world) {
        var local = world - transform.position;
        return new Vector2Int(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(-local.z / cellSize));
    }

    public Vector2Int Find(char k) {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (rows[y][x] == k) return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }

    public List<Vector2Int> OpenCells(bool creature) {
        var list = new List<Vector2Int>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (Walkable(new Vector2Int(x, y), creature)) list.Add(new Vector2Int(x, y));
        return list;
    }

    static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    // Camino mas corto de celda a celda (sin incluir la de partida). Vacio si no hay camino.
    public List<Vector2Int> Path(Vector2Int from, Vector2Int to, bool creature) {
        var result = new List<Vector2Int>();
        if (!Walkable(to, creature)) return result;
        var previous = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        while (queue.Count > 0) {
            var c = queue.Dequeue();
            if (c == to) break;
            foreach (var d in Steps) {
                var n = c + d;
                if (Walkable(n, creature) && !previous.ContainsKey(n)) {
                    previous[n] = c;
                    queue.Enqueue(n);
                }
            }
        }
        if (!previous.ContainsKey(to)) return result;
        for (var c = to; c != from; c = previous[c]) result.Add(c);
        result.Reverse();
        return result;
    }
}
}
