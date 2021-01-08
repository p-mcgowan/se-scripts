public class IGCEmitter {
    public Program p;
    public long id;
    public Dictionary<string, IMyBroadcastListener> listeners;
    public Dictionary<string, List<Action<MyIGCMessage>>> handlers;
    public IMyUnicastListener unicastListener;
    public Dictionary<string, List<Action<MyIGCMessage>>> unicastHandlers;
    public Dictionary<string, long> receievers;

    public IGCEmitter(Program p) {
        this.p = p;
        this.id = this.p.IGC.Me;
        this.handlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.listeners = new Dictionary<string, IMyBroadcastListener>();
        this.unicastHandlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.receievers = new Dictionary<string, long>();
    }

    public IGCEmitter Emit<TData>(string channel, TData data, long target = -1) {
        if (target != -1) {
            this.p.IGC.SendUnicastMessage(target, channel, data);
        } else {
            this.p.IGC.SendBroadcastMessage(channel, data);
        }

        return this;
    }

    public IGCEmitter OnUnicast(string channel, Action<MyIGCMessage> handler) {
        this.unicastListener = this.p.IGC.UnicastListener;
        this.unicastListener.SetMessageCallback(channel);
        this.AddHandler(channel, handler, this.unicastHandlers);

        return this;
    }

    public IGCEmitter On(string channel, Action<MyIGCMessage> handler) {
        this.AddListener(channel);
        this.AddHandler(channel, handler, this.handlers);

        this.p.Echo($"[{this.id}] listening on {channel}");

        return this;
    }

    public void Off(string channel) {
        this.listeners.Remove(channel);
        this.handlers.Remove(channel);
    }

    public void Process(string callbackString = "") {
        List<Action<MyIGCMessage>> callbacks;

        foreach (var kv in this.listeners) {
            string channel = kv.Key;
            IMyBroadcastListener listener = kv.Value;

            if (listener.HasPendingMessage && this.handlers.TryGetValue(channel, out callbacks)) {
                while (listener.HasPendingMessage) {
                    MyIGCMessage msg = listener.AcceptMessage();
                    foreach (Action<MyIGCMessage> handle in callbacks) {
                        handle(msg);
                    }
                }
            }
        }

        if (this.unicastListener != null) {
            if (this.unicastListener.HasPendingMessage) {
                while (this.unicastListener.HasPendingMessage) {
                    MyIGCMessage msg = this.unicastListener.AcceptMessage();
                    if (this.unicastHandlers.TryGetValue(msg.Tag, out callbacks)) {
                        foreach (Action<MyIGCMessage> handle in callbacks) {
                            handle(msg);
                        }
                    }
                }
            }
        }
    }

    public void Log(string message) {
        this.p.Me.GetSurface(0).WriteText(message + "\n", true);
        this.p.Echo(message);
    }

    public void Hello(string response = null) {
        response = response ?? this.p.Me.CubeGrid.CustomName;

        this
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                this.Emit("HELLO", response, msg.Source);
                Log($"[{msg.Tag}] <= {who}");
            })
            .OnUnicast("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                Log($"[{msg.Tag}] <= {who}");
            });

        this.Emit("HELLO", response);
    }

    public void AddListener(string channel) {
        IMyBroadcastListener listener = this.p.IGC.RegisterBroadcastListener(channel);
        listener.SetMessageCallback(channel);
        this.listeners[channel] = listener;
    }

    public void AddHandler(string channel, Action<MyIGCMessage> handler,  Dictionary<string, List<Action<MyIGCMessage>>> handlers) {
        List<Action<MyIGCMessage>> list;
        if (!handlers.TryGetValue(channel, out list)) {
            handlers[channel] = new List<Action<MyIGCMessage>>();
        }
        handlers[channel].Add(handler);
    }

    public long Who(string name) {
        return this.receievers[name];
    }

    public string Who(long id) {
        return this.receievers.FirstOrDefault(x => x.Value == id).Key;
    }
}
