public class Template {
    public class Token {
        public bool isText = true;
        public string value = null;
    }

    public class Node {
        public string action;
        public string text;
        public Dictionary<string, string> options;

        public Node(string action, string text = null, Dictionary<string, string> options = null) {
            this.action = action;
            this.text = text;
            this.options = options ?? new Dictionary<string, string>();
        }
    }

    public delegate void DsCallback(DrawingSurface ds, string token, Dictionary<string, string> options);
    public delegate string TextCallback();

    public Program program;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public Dictionary<string, DsCallback> methods;
    public Dictionary<string, List<Node>> renderNodes;
    public Dictionary<string, bool> templateVars;

    public char[] splitSemi = new[] { ';' };
    public char[] splitDot = new[] { '.' };
    public string[] splitLine = new[] { "\r\n", "\r", "\n" };

    public Template(Program program = null) {
        this.program = program;
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<newline>\?)?(?<name>[^:]+)(:(?<params>[^:]*))?(:(?<text>.+))?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, DsCallback>();
        this.renderNodes = new Dictionary<string, List<Node>>();
        this.templateVars = new Dictionary<string, bool>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        this.Register("text", this.RenderText);
        this.Register("textCircle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.TextCircle(options));
        this.Register("circle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Circle(options));
        this.Register("bar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Bar(options));
        this.Register("midBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.MidBar(options));
        this.Register("multiBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.MultiBar(options));
    }

    public void Clear() {
        this.methods.Clear();
        this.renderNodes.Clear();
        this.templateVars.Clear();
    }

    public void Register(string key, DsCallback callback) {
        this.methods[key] = callback;
    }

    public void Register(string key, TextCallback callback) {
        this.methods[key] = (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Text(callback(), options);
    }

    public void RenderText(DrawingSurface ds, string text, Dictionary<string, string> options) {
        ds.Text(text, options);
    }

    public Dictionary<string, bool> PreRender(string outputName, string templateStrings) {
        return this.PreRender(outputName, templateStrings.Split(splitLine, StringSplitOptions.None));
    }

    public Dictionary<string, bool> PreRender(string outputName, string[] templateStrings) {
        this.templateVars.Clear();
        List<Node> nodeList = new List<Node>();

        bool autoNewline;
        for (int i = 0; i < templateStrings.Length; ++i) {
            string line = templateStrings[i].TrimEnd();
            autoNewline = true;
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    nodeList.Add(new Node("text", this.token.value));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    var opts = this.StringToDict(m.Groups["params"].Value);
                    if (m.Groups["newline"].Value != "") {
                        opts["noNewline"] = "true";
                        autoNewline = false;
                    }
                    string text = (m.Groups["text"].Value == "" ? null : m.Groups["text"].Value);
                    if (text != null) {
                        opts["text"] = text;
                    }
                    this.AddTemplateTokens(m.Groups["name"].Value);
                    nodeList.Add(new Node(m.Groups["name"].Value, text, opts));
                } else {
                    this.AddTemplateTokens(this.token.value);
                    nodeList.Add(new Node(this.token.value));
                }
            }

            if (autoNewline) {
                nodeList.Add(new Node("newline"));
            }
        }

        this.renderNodes[outputName] = nodeList;

        return this.templateVars;
    }

    public void AddTemplateTokens(string name) {
        string prefix = "";
        foreach (string part in name.Split(splitDot, StringSplitOptions.RemoveEmptyEntries)) {
            this.templateVars[$"{prefix}{part}"] = true;
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
        foreach (Node node in nodeList) {
            if (node.action == "newline") {
                ds.Newline();
                continue;
            }

            if (this.methods.TryGetValue(node.action, out callback)) {
                callback(ds, node.text, node.options);
            } else {
                ds.Text($"{{{node.action}}}");
            }
        }

        ds.Draw();
    }

    public Dictionary<string, string> StringToDict(string options = "") {
        if (options == "") {
            return new Dictionary<string, string>();
        }

        return options.Split(splitSemi, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('='))
            .ToDictionary(pair => pair[0], pair => pair[1]);
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
