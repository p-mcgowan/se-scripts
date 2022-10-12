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
