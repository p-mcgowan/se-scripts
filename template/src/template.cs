public class Token {
    public bool isText = true;
    public string value = null;
}

public delegate void Del(ref DrawingSurface ds, string token, string options = "");

public class Template {
    public Program program;
    public Dictionary<string, Del> methods;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public char[] splitSpace;

    public Template(Program program = null) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<name>[^: ]+)(:(?<params>[^; ]+);? )(?<text>.*)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, Del>();
        this.program = program;
        this.RegisterRenderAction("text", this.RenderText);
        this.splitSpace = new[] { ' ' };
    }

    public void RegisterRenderAction(string key, Del callback) {
        this.methods[key] = callback;
    }

    public void RenderText(ref DrawingSurface ds, string text, string options = "") {
        Color? colour = null;
        if (options != "") {
            Dictionary<string, string> keyValuePairs = options.Split(';')
                .Select(value => value.Split('='))
                .ToDictionary(pair => pair[0], pair => pair[1]);

            if (keyValuePairs["colour"] != null) {
                var cols = keyValuePairs["colour"].Split(',').Select(value => Int32.Parse(value)).ToArray();
                colour = new Color(
                    cols.ElementAtOrDefault(0),
                    cols.ElementAtOrDefault(1),
                    cols.ElementAtOrDefault(2),
                    cols.Length > 3 ? cols[3] : 255
                );
            }
        }
        this.program.Echo($"c:{colour}");
        ds.Text(text, colour: colour);
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
                this.program.Echo($"err parsing token {e}");
                return false;
            }

            return true;
        } else {
            return false;
        }
    }

    public void Render(ref DrawingSurface ds, string[] templateStrings) {
        foreach (string line in templateStrings) {
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    ds.Text(this.token.value);
                    continue;
                }

                string name = this.token.value;
                string opts = "";
                string text = "";
                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    opts = m.Groups["params"].Value;
                    text = m.Groups["text"].Value;
                    name = m.Groups["name"].Value;
                }
                this.program.Echo($"name:{name},text:{text},opts:{opts}");

                // string[] parts = this.token.value.Split(this.splitSpace, 2);
                // string[] opts = this.token.value.Split(':');
                // this.program.Echo(String.Format("p1: {0}, p2?: {1}", parts[0], parts.Length > 1 ? parts[1] : "naw"));

                if (this.methods.ContainsKey(name)) {
                    this.methods[name](ref ds, text, opts);
                } else {
                    ds.Text($"{{{this.token.value}}}");
                }
            }
            ds.Newline();
        }
        ds.Draw();
    }
}
