using System;
using UnityEngine;

namespace ElDesecho {
// Ruido del mundo. El Deshecho es ciego: solo sabe donde esta el jugador por lo que oye.
public static class Noise {
    public static event Action<Vector3, float> Heard;
    // Ultimo ruido del jugador (0 = silencio, 1 = muy fuerte), para el indicador en pantalla.
    public static float PlayerLevel { get; private set; }

    public static void Emit(Vector3 position, float radius, bool fromPlayer = true) {
        if (radius <= 0) return;
        if (fromPlayer) PlayerLevel = Mathf.Max(PlayerLevel, Mathf.Clamp01(radius / 16f));
        Heard?.Invoke(position, radius);
    }

    public static void Fade(float dt) => PlayerLevel = Mathf.MoveTowards(PlayerLevel, 0, dt * .8f);
}
}
