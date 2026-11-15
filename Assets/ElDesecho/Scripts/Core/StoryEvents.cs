using System;

namespace ElDesecho {
// Canal comun de eventos de historia: los disparadores e interactuables lo emiten y los objetivos lo escuchan.
public static class StoryEvents {
    public static event Action<string> Raised;
    public static void Raise(string id) {
        if (!string.IsNullOrEmpty(id)) Raised?.Invoke(id);
    }
}
}
