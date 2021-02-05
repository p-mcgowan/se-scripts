public class Template {
    public class Token {
        public bool isText = true;
        public string value = null;
    }

    public class Node {
        public string action;
        public string text;
        public DrawingSurface.Options options;
        public MySprite? sprite;

        public Node(string action, string text = null, DrawingSurface.Options opts = null) {
            this.action = action;
            this.text = text;
            this.options = opts ?? new DrawingSurface.Options();
            if (action == "text") {
                this.options.text = this.options.text ?? text;
                this.sprite = DrawingSurface.TextSprite(this.options);
            }
        }
    }

    public delegate void DsCallback(DrawingSurface ds, string token, DrawingSurface.Options options);
    public delegate string TextCallback();

    public Program program;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public Dictionary<string, DsCallback> methods;
    public Dictionary<string, List<Node>> renderNodes;
    public Dictionary<string, Dictionary<string, bool>> templateVars;
    public Dictionary<string, string> prerenderedTemplates;
    public List<int> removeNodes;

    public char[] splitSemi = new[] { ';' };
    public char[] splitDot = new[] { '.' };
    public string[] splitLine = new[] { "\r\n", "\r", "\n" };

    public Template(Program program = null) {
        this.program = program;
        this.tokenizer = Util.Regex(@"((?<!\\)\{([^\}]|\\\})+(?<!\\)\}|(\\\{|[^\{])+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<newline>\?)?(?<name>[^:]+)(:(?<params>[^:]*))?(:(?<text>.+))?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, DsCallback>();
        this.renderNodes = new Dictionary<string, List<Node>>();
        this.templateVars = new Dictionary<string, Dictionary<string, bool>>();
        this.prerenderedTemplates = new Dictionary<string, string>();
        this.removeNodes = new List<int>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        this.Register("textCircle", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.TextCircle(options));
        this.Register("circle", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Circle(options));
        this.Register("bar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Bar(options));
        this.Register("midBar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.MidBar(options));
        this.Register("multiBar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.MultiBar(options));
        this.Register("right", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.SetCursor(ds.width, null));
        this.Register("center", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.SetCursor(ds.width / 2f, null));
    }

    public void Clear() {
        this.templateVars.Clear();
        this.prerenderedTemplates.Clear();
        this.renderNodes.Clear();
        this.methods.Clear();
    }

    public void Register(string key, DsCallback callback) {
        this.methods[key] = callback;
    }

    public void Register(string key, TextCallback callback) {
        // TODO: precalc
        this.methods[key] = (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Text(callback(), options);
    }

    public void ConfigureScreen(DrawingSurface ds, DrawingSurface.Options options) {
        ds.surface.Font = options.custom.Get("font", ds.surface.Font);
        ds.surface.FontSize = options.size == 0f ? ds.surface.FontSize : options.size;
        ds.surface.TextPadding = options.textPadding ?? ds.surface.TextPadding;
        ds.surface.ScriptForegroundColor = options.colour ?? ds.surface.ScriptForegroundColor;
        ds.surface.ScriptBackgroundColor = options.bgColour ?? ds.surface.ScriptBackgroundColor;
    }

    public bool IsPrerendered(string outputName, string templateString) {
        string value = this.prerenderedTemplates.Get(outputName, null);
        if (value == "" || value == null) {
            return false;
        }
        if (String.CompareOrdinal(value, templateString) != 0) {
            return false;
        }
        return true;
    }

    public Dictionary<string, bool> PreRender(string outputName, string templateStrings) {
        this.prerenderedTemplates[outputName] = templateStrings;
        return this.PreRender(outputName, templateStrings.Split(splitLine, StringSplitOptions.None));
    }

    private Dictionary<string, bool> PreRender(string outputName, string[] templateStrings) {
        if (this.templateVars.ContainsKey(outputName)) {
            this.templateVars[outputName].Clear();
        } else {
            this.templateVars[outputName] = new Dictionary<string, bool>();
        }
        List<Node> nodeList = new List<Node>();

        bool autoNewline;
        string text;
        for (int i = 0; i < templateStrings.Length; ++i) {
            string line = templateStrings[i].TrimEnd();
            autoNewline = true;
            this.match = null;
            text = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    text = System.Text.RegularExpressions.Regex.Replace(this.token.value, @"\\([\{\}])", "$1");
                    nodeList.Add(new Node("text", text));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    var opts = this.StringToOptions(m.Groups["params"].Value);
                    if (m.Groups["newline"].Value != "") {
                        opts.custom["noNewline"] = "true";
                        autoNewline = false;
                    }
                    text = (m.Groups["text"].Value == "" ? null : m.Groups["text"].Value);
                    if (text != null) {
                        text = System.Text.RegularExpressions.Regex.Replace(text, @"\\([\{\}])", "$1");
                        opts.text = text;
                    }
                    string action = m.Groups["name"].Value;
                    this.AddTemplateTokens(this.templateVars[outputName], action);
                    nodeList.Add(new Node(action, text, opts));
                    if (action == "config") {
                        autoNewline = false;
                    }
                } else {
                    this.AddTemplateTokens(this.templateVars[outputName], this.token.value);
                    nodeList.Add(new Node(this.token.value));
                }
            }

            if (autoNewline) {
                nodeList.Add(new Node("newline"));
            }
        }

        this.renderNodes[outputName] = nodeList;

        return this.templateVars[outputName];
    }

    public void AddTemplateTokens(Dictionary<string, bool> tplVars, string name) {
        string prefix = "";
        foreach (string part in name.Split(splitDot, StringSplitOptions.RemoveEmptyEntries)) {
            tplVars[$"{prefix}{part}"] = true;
            prefix = $"{prefix}{part}.";
        }
    }

    public void Render(DrawingSurface ds, string name = null) {
        string dsName = name ?? ds.name;
        List<Node> nodeList = null;
        if (!this.renderNodes.TryGetValue(dsName, out nodeList)) {
            ds.Text("No template found").Draw();
            return;
        }

        DsCallback callback = null;
        int i = 0;
        this.removeNodes.Clear();
        foreach (Node node in nodeList) {
            if (node.action == "newline") {
                ds.Newline();
                continue;
            }

            if (node.action == "text") {
                ds.AddTextSprite((MySprite)node.sprite);
                continue;
            }

            if (node.action == "config") {
                this.ConfigureScreen(ds, node.options);
                removeNodes.Add(i);
                continue;
            }

            if (this.methods.TryGetValue(node.action, out callback)) {
                callback(ds, node.text, node.options);
            } else {
                ds.Text($"{{{node.action}}}");
            }
            i++;
        }
        ds.Draw();

        foreach (int removeNode in removeNodes) {
            nodeList.RemoveAt(removeNode);
        }
    }

    public DrawingSurface.Options StringToOptions(string options = "") {
        DrawingSurface.Options opts = new DrawingSurface.Options();
        if (options == "") {
            return opts;
        }

        Dictionary<string, string> parsed = options.Split(splitSemi, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Split('='))
            .ToDictionary(pair => pair.Length > 1 ? pair[0] : "unknown", pair => pair.Length > 1 ? pair[1] : pair[0]);

        string value;
        if (parsed.Pop("width", out value)) { opts.width = Util.ParseFloat(value); }
        if (parsed.Pop("height", out value)) { opts.height = Util.ParseFloat(value); }
        if (parsed.Pop("outline", out value)) { opts.outline = Util.ParseBool(value); }
        if (parsed.Pop("bgColour", out value)) { opts.bgColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("colour", out value)) { opts.colour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("fillColour", out value)) { opts.fillColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("textColour", out value)) { opts.textColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("height", out value)) { opts.height = Util.ParseFloat(value); }
        if (parsed.Pop("high", out value)) { opts.high = Util.ParseFloat(value); }
        if (parsed.Pop("low", out value)) { opts.low = Util.ParseFloat(value); }
        if (parsed.Pop("net", out value)) { opts.net = Util.ParseFloat(value); }
        if (parsed.Pop("pad", out value)) { opts.pad = Util.ParseFloat(value); }
        if (parsed.Pop("pct", out value)) { opts.pct = Util.ParseFloat(value); }
        if (parsed.Pop("scale", out value)) { opts.scale = Util.ParseFloat(value); }
        if (parsed.Pop("size", out value)) { opts.size = Util.ParseFloat(value); }
        if (parsed.Pop("width", out value)) { opts.width = Util.ParseFloat(value); }
        if (parsed.Pop("text", out value)) { opts.text = value; }
        if (parsed.Pop("textPading", out value)) { opts.textPadding = Util.ParseFloat(value); }
        if (parsed.Pop("align", out value)) { opts.align = DrawingSurface.stringToAlignment.Get(value); }
        if (parsed.Pop("colours", out value)) {
            opts.colours = value.Split(DrawingSurface.underscoreSep)
                .Select(col => DrawingSurface.StringToColour(col) ?? Color.White).ToList();
        }
        if (parsed.Pop("values", out value)) {
            opts.values = value.Split(DrawingSurface.underscoreSep)
                .Select(pct => Util.ParseFloat(pct, 0f)).ToList();
        }

        opts.custom = parsed;

        return opts;
    }

    public void Echo(string text) {
        if (this.program != null) {
            this.program.Echo(text);
        }
    }

    public bool GetToken(string line) {
        if (this.match == null) {
            this.match = this.tokenizer.Match(line);
        } else {
            this.match = this.match.NextMatch();
        }

        if (this.match.Success) {
            try {
                string _token = this.match.Groups[1].Value;
                if (_token[0] == '{') {
                    this.token.value = _token.Substring(1, _token.Length - 2);
                    this.token.isText = false;
                } else {
                    this.token.value = _token;
                    this.token.isText = true;
                }
            } catch (Exception e) {
                this.Echo($"err parsing token {e}");
                return false;
            }

            return true;
        } else {
            return false;
        }
    }
}
