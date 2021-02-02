StringBuilder logs = new StringBuilder(512);
StringBuilder size = new StringBuilder("Q");

public int GetLineCount() {
    IMyTextSurface surface = Me.GetSurface(0);
    Vector2 charSizeInPx = surface.MeasureStringInPixels(size, surface.Font, surface.FontSize);
    float padding = (surface.TextPadding / 100) * surface.SurfaceSize.Y;
    float height = surface.SurfaceSize.Y - (2 * padding);

    return (int)(Math.Round(height / charSizeInPx.Y));
}

public void Debug(string message, bool newline = true, IMyTextSurface output = null) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);
    string res = logs.ToString();
    Echo(text);
    if (output != null) {
        output.WriteText(res);
    } else {
        Me.GetSurface(0).WriteText(res);
    }
}

public void Log(string message, bool newline = true, IMyTextSurface output = null) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);

    string res = logs.ToString();
    int lineLength = GetLineCount();
    int count = 0;
    for (int i = logs.Length - 1; i >= 0; --i) {
        if (res[i] == '\n') {
            count++;
        }
        if (count >= lineLength) {
            res = res.Substring(i, logs.Length - i);
            break;
        }
    }

    Echo(text);
    if (output != null) {
        output.WriteText(res);
    } else {
        Me.GetSurface(0).WriteText(res);
    }
}
