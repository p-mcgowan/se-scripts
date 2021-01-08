IGCEmitter emitter;

public Program() {
    emitter = new IGCEmitter(this);
    emitter.Hello();
}

public void Main(string argument, UpdateType updateType) {
    Echo(updateType.ToString());
    Echo(argument);

    if ((updateType & UpdateType.IGC) != 0) {
        Echo($"rec'd msg");
        emitter.Process(argument);
    } else if (argument != null && argument != "") {
        string[] args = argument.Split(new [] { ' ' });

        Echo($"sending '{args[1]}' on channel '{args[0]}'");
        emitter.Emit(args[0], args[1]);
    }
}
