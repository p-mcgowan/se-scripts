public class IGCEmitter {
    public Program p;
    public long id;
    public Dictionary<string, IMyBroadcastListener> listeners;
    public Dictionary<string, List<Action<MyIGCMessage>>> handlers;
    public IMyUnicastListener unicastListener;
    public Dictionary<string, List<Action<MyIGCMessage>>> unicastHandlers;
    public Dictionary<string, long> receievers;
    public bool verbose;
    public StringBuilder logs;

    public IGCEmitter(Program p, bool verbose = false, StringBuilder logs = null) {
        this.p = p;
        this.id = this.p.IGC.Me;
        this.handlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.listeners = new Dictionary<string, IMyBroadcastListener>();
        this.unicastHandlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.receievers = new Dictionary<string, long>();
        this.verbose = verbose;
        this.logs = logs ?? new StringBuilder("");
        this.unicastListener = this.p.IGC.UnicastListener;
    }

    public IGCEmitter Emit<TData>(string channel, TData data, long target = -1) {
        if (target != -1) {
            this.p.IGC.SendUnicastMessage(target, channel, data);
        } else {
            this.p.IGC.SendBroadcastMessage(channel, data);
        }

        return this;
    }

    public IGCEmitter On(string channel, Action<MyIGCMessage> handler, bool unicast = false) {
        if (unicast) {
            this.unicastListener.SetMessageCallback(channel);
            this.AddHandler(channel, handler, this.unicastHandlers);
        } else {
            this.AddListener(channel);
            this.AddHandler(channel, handler, this.handlers);
        }

        if (this.verbose) {
            this.p.Echo($"[{this.p.Me.CubeGrid.CustomName}] listening on {channel}");
        }

        return this;
    }

    public IGCEmitter Once(string channel, Action<MyIGCMessage> handler, bool unicast = false) {
        Action<MyIGCMessage> wrapper = null;

        wrapper = (MyIGCMessage msg) => {
            handler(msg);
            this.Off(channel, wrapper, unicast);
        };
        this.On(channel, wrapper, unicast);

        return this;
    }

    public bool Off(string channel, Action<MyIGCMessage> handler = null, bool unicast = false) {
        List<Action<MyIGCMessage>> actions = null;
        var msgHandles = unicast ? this.unicastHandlers : this.handlers;

        if (!msgHandles.TryGetValue(channel, out actions) || actions.Count == 0) {
            return false;
        }

        if (handler == null) {
            actions.Clear();
        } else {
            actions.RemoveAll(h => h == handler);
        }

        if (actions.Count == 0) {
            if (unicast) {
                this.unicastListener.DisableMessageCallback();
            } else {
                this.listeners[channel].DisableMessageCallback();
            }
        }

        return true;
    }

    public bool Process(string callbackString = "") {
        bool hadMessage = false;
        List<Action<MyIGCMessage>> callbacks;
        IMyBroadcastListener listener;
        MyIGCMessage msg;

        foreach (var kv in this.listeners) {
            string channel = kv.Key;
            listener = kv.Value;

            if (listener.HasPendingMessage && this.handlers.TryGetValue(channel, out callbacks)) {
                while (listener.HasPendingMessage) {
                    msg = listener.AcceptMessage();
                    this.logs.Append($"[{this.Who(msg.Source)}] {msg.Tag}: {msg.Data}\n");

                    for (int cbIndex = callbacks.Count - 1; cbIndex >= 0; --cbIndex) {
                        callbacks[cbIndex](msg);
                    }
                    hadMessage = true;
                }
            }
        }

        while (this.unicastListener.HasPendingMessage) {
            msg = this.unicastListener.AcceptMessage();
            this.logs.Append($"[{this.Who(msg.Source)}] {msg.Tag}: {msg.Data}\n");

            if (this.unicastHandlers.TryGetValue(msg.Tag, out callbacks)) {
                for (int cbIndex = callbacks.Count - 1; cbIndex >= 0; --cbIndex) {
                    callbacks[cbIndex](msg);
                }
                hadMessage = true;
            }
        }

        return hadMessage;
    }

    public bool HasMessages() {
        if (this.unicastListener.HasPendingMessage) {
            return true;
        }

        foreach (var kv in this.listeners) {
            if (kv.Value.HasPendingMessage) {
                return true;
            }
        }

        return false;
    }

    public void Hello(string response = null) {
        response = response ?? this.p.Me.CubeGrid.CustomName;

        this
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                this.Emit("HELLO", response, msg.Source);
            })
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;

                if (this.verbose) {
                    this.p.Echo($"hello from {who}");
                }
            }, unicast: true);


        this.Emit("HELLO", response);
    }

    public void AddListener(string channel) {
        IMyBroadcastListener listener = this.p.IGC.RegisterBroadcastListener(channel);
        listener.SetMessageCallback(channel);
        this.listeners[channel] = listener;
    }

    public void AddHandler(string channel, Action<MyIGCMessage> handler, Dictionary<string, List<Action<MyIGCMessage>>> handlers) {
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
