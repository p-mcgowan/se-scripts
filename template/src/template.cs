public class Token {
    public bool isText = true;
    public string value = null;
}

public delegate void Del(DrawingSurface ds, string token);

public class Template {
    public Program program;
    public Dictionary<string, Del> methods;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Match match;
    public Token token;

    public Template(Program program) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, Del>();
        this.program = program;
    }

    public void RegisterRenderAction(string key, Del callback) {
        this.methods[key] = callback;
    }

    public void RenderText(DrawingSurface ds, string text) {
        ds.Text(text);
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
                } else if (this.methods.ContainsKey(this.token.value)) {
                    this.methods[this.token.value](ds, this.token.value);
                } else {
                    ds.Text($"{{{this.token.value}}}");
                }
            }
            ds.Newline();
        }
        ds.Draw();
    }
}
