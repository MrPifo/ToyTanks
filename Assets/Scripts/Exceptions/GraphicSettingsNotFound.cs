using System;

[Serializable]
public class GraphicSettingsNotFound : Exception {
    public GraphicSettingsNotFound() { }

    public GraphicSettingsNotFound(string message)
        : base(message) { }

    public GraphicSettingsNotFound(string message, Exception inner)
        : base(message, inner) { }
}