public Program() {
    config.Parse(this);
}

public void Main() {

}
Config config = new Config();

public static TValue DictGet<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
    TValue value;
    return dict.TryGetValue(key, out value) ? value : defaultValue;
}

public class Config {
    public MyIni ini;
    public string customData;
    public Dictionary<string, string> settings;
    public List<MyIniKey> keys;
    public List<string> sections;

    public Config() {
        this.ini = new MyIni();
        this.settings = new Dictionary<string, string>();
        this.keys = new List<MyIniKey>();
        this.sections = new List<string>();
    }

    public void Clear() {
        this.ini.Clear();
        this.settings.Clear();
        this.keys.Clear();
        this.customData = null;
    }

    public bool Parse(Program p) {
        bool parsed = this.Parse(p.Me.CustomData);
        if (!parsed) {
            p.Echo($"failed to parse customData");
        }

        return parsed;
    }

    public bool Parse(string iniTemplate) {
        this.Clear();

        MyIniParseResult result;
        if (!this.ini.TryParse(iniTemplate, out result)) {
            return false;
        }
        this.customData = iniTemplate;

        this.ini.GetSections(this.sections);

        string keyValue;
        this.ini.GetKeys(this.keys);
        foreach (MyIniKey key in this.keys) {
            if (this.ini.Get(key.Section, key.Name).TryGetString(out keyValue)) {
                this.Set(key.ToString(), keyValue);
            }
        }

        return true;
    }

    public void Set(string name, string keyValue) {
        this.settings[name] = keyValue;
    }

    public string Get(string name, string alt = null) {
        return DictGet<string, string>(this.settings, name, null) ?? alt;
    }

    public bool Enabled(string name) {
        return DictGet<string, string>(this.settings, name, "false") == "true";
    }
}
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
