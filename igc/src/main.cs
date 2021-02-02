IGCEmitter emitter;

public void HandleTest(MyIGCMessage msg) {
    Echo(msg.ToString());
    emitter.Emit("TEST", "I am responding!", msg.Source);
}

public Program() {
    emitter = new IGCEmitter(this);
    emitter.Hello();

    emitter.On("TEST", HandleTest);
}

public void Main(string argument, UpdateType updateType) {
    MyCommandLine cli = new MyCommandLine();
    cli.TryParse(argument);

    if ((updateType & UpdateType.IGC) != 0) {
        emitter.Process(argument);
    } else if (argument != null && argument != "") {
        string command = cli.Items.ElementAtOrDefault(0);
        string channel = cli.Items.ElementAtOrDefault(1);
        string msg = cli.Items.ElementAtOrDefault(2);
        string target = cli.Items.ElementAtOrDefault(3);

        switch (command) {
            case "who":
                foreach (var kv in emitter.receievers) {
                    Echo($"{kv.Key}, {kv.Value}");
                }
            break;
            case "broadcast":
                Echo($"[{channel}]: broadcasting '{msg}'");
                emitter.Emit(channel, msg);
            break;
            case "send":
                Echo($"[{channel}]: unicasting '{msg}' to {target}");
                emitter.Emit(channel, msg, emitter.Who(target));
            break;
        }
    }
}
