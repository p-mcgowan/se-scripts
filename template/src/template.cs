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
            this.options = options;
        }
    }

    public delegate void Del(DrawingSurface ds, string token, Dictionary<string, string> options);
    public delegate string Callback();

    public Program program;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public Dictionary<string, Del> methods;
    public Dictionary<string, List<Node>> renderNodes;

    public char[] splitSemi = new[] { ';' };

    public Template(Program program = null) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<name>[^:; ]+)(:(?<params>[^ ]+)?;?)? ?(?<text>.*)?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, Del>();
        this.program = program;
        this.renderNodes = new Dictionary<string, List<Node>>();
        this.RegisterRenderAction("text", this.RenderText);
    }

    public void RegisterRenderAction(string key, Del callback) {
        this.methods[key] = callback;
    }

    public void RegisterRenderAction(string key, Callback callback) {
        this.methods[key] = this.Text(callback);
    }

    public void PreRender(string outputName, string templateStrings) {
        this.PreRender(outputName, templateStrings.Split('\n'));
    }

    public void PreRender(string outputName, string[] templateStrings) {
        List<Node> nodeList = new List<Node>();

        foreach (string line in templateStrings) {
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    nodeList.Add(new Node("text", this.token.value));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    nodeList.Add(new Node(m.Groups["name"].Value, m.Groups["text"].Value, this.StringToDict(m.Groups["params"].Value)));
                } else {
                    nodeList.Add(new Node(this.token.value));
                }
            }
        }

        this.renderNodes.Add(outputName, nodeList);
    }

    public void Render(DrawingSurface ds, string name = null) {
        string dsName = name ?? ds.name;
        List<Node> nodeList = null;
        if (!this.renderNodes.TryGetValue(dsName, out nodeList)) {
            ds.Text("No template found").Draw();
            return;
        }

        Del callback = null;
        foreach (Node node in nodeList) {
            if (this.methods.TryGetValue(node.action, out callback)) {
                callback(ds, node.text, node.options);
            } else {
                ds.Text($"{{{node.action}}}");
            }
            callback = null;
            ds.Newline();
        }

        ds.Draw();
    }

    public Del Text(Callback getStr) {
        return (DrawingSurface ds, string text, Dictionary<string, string> options) => this.RenderText(ds, getStr(), options);
    }

    public void RenderText(DrawingSurface ds, string text, Dictionary<string, string> options) {
        ds.Text(text, options);
    }

    public Dictionary<string, string> StringToDict(string options) {
        if (options == "") {
            return null;
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
