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

    public Config() {
        this.ini = new MyIni();
        this.settings = new Dictionary<string, string>();
        this.keys = new List<MyIniKey>();
    }

    public void Clear() {
        this.ini.Clear();
        this.settings.Clear();
        this.keys.Clear();
        this.customData = null;
    }

    public bool Parse(Program p) {
        this.Clear();

        MyIniParseResult result;
        if (!this.ini.TryParse(p.Me.CustomData, out result)) {
            p.Echo($"failed to parse custom data\n{result}");
            return false;
        }
        this.customData = p.Me.CustomData;

        string value;
        this.ini.GetKeys(this.keys);

        foreach (MyIniKey key in this.keys) {
            if (this.ini.Get(key.Section, key.Name).TryGetString(out value)) {
                this.Set(key.ToString(), value);
            }
        }

        return true;
    }

    public void Set(string name, string value) {
        this.settings[name] = value;
    }

    public string Get(string name, string alt = null) {
        return DictGet<string, string>(this.settings, name, null) ?? alt;
    }

    public bool Enabled(string name) {
        return DictGet<string, string>(this.settings, name, "false") == "true";
    }
}
