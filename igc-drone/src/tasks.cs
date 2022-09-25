public delegate bool ActionItem(string arg = null);
public delegate bool ArglessActionItem();
Queue<Task> tasks = new Queue<Task>();

public struct Task {
    public readonly string name;
    public readonly ActionItem action;
    public readonly ArglessActionItem arglessAction;
    public readonly string arg;

    public Task(string name, ActionItem action, string arg) {
        this.name = name;
        this.action = action;
        this.arg = arg;
        this.arglessAction = null;
    }

    public Task(string name, ArglessActionItem arglessAction) {
        this.name = name;
        this.arglessAction = arglessAction;
        this.action = null;
        this.arg = null;
    }

    public bool TryComplete() {
        return this.action != null ? this.action(this.arg) : this.arglessAction();
    }
}

public void AddTask(string name, ArglessActionItem action) {
    tasks.Enqueue(new Task(name, action));
}

public void AddTask(string name, ActionItem action, string arg) {
    tasks.Enqueue(new Task(name, action, arg));
}

public void ProcessTasks() {
    logs.Clear();
    GetCurrentTask();
}

public void GetCurrentTask() {
    if (tasks.Count <= 0) {
        state = "Idle";
        return;
    }

    Task task = tasks.Peek();
    if (state != task.name) {
        Log($"> {task.name} ({tasks.Count})");
    }
    state = task.name;

    if (!task.TryComplete()) {
        return;
    }

    Task done;
    tasks.TryDequeue(out done);

    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
    if (remoteControl != null) {
        remoteControl.ClearWaypoints();
    }

    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

public void SetIdle() {
    tasks.Clear();
    state = "Idle";
}

public bool AbortTask(string reason) {
    Log($"AbortTask: {reason}");
    SetIdle();

    return true;
}

public void RemoveCurrentTask(string nextState = null) {
    Task task = tasks.Dequeue();
    // Log($" - {task.name}");
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
    if (nextState != null) {
        state = nextState;
    }
}
