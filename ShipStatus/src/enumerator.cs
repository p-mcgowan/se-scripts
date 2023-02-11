/*
 * ENUMERATOR
 */
public void RunStateMachine() {
    if (stateMachine == null) {
        return;
    }
    bool hasMoreSteps = stateMachine.MoveNext();

    if (hasMoreSteps) {
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
    } else {
        stateMachine.Dispose();
        stateMachine = null;
        Runtime.UpdateFrequency &= ~UpdateFrequency.Once;
    }
}

public IEnumerator<string> RunStuffOverTime()  {
    string content;
    string outputName;

    while (templates.Count > 0) {
        outputName = templates.Keys.Last();
        templates.Pop(templates.Keys.Last(), out content);

        Dictionary<string, bool> tokens;
        if (template.IsPrerendered(outputName, content)) {
            tokens = template.templateVars[outputName];
        } else {
            log.Append($"Adding or updating {outputName}\n");
            tokens = template.PreRender(outputName, content);
        }

        foreach (var kv in tokens) {
            config.Set(kv.Key, config.Get(kv.Key, "true")); // don't override globals
        }

        yield return $"templates {outputName}";

        if (templates.Count == 0) {
            powerDetails.Reset();
            cargoStatus.Reset();
            blockHealth.Reset();
            productionDetails.Reset();
            gasStatus.Reset();
            airlock.Reset();

            yield return "reset";
            RefetchBlocks();
        }

        yield return "updated";
    }

    if (config.Enabled("power")) {
        powerDetails.Refresh();
        yield return "powerDetails";
    }
    if (config.Enabled("cargo")) {
        cargoStatus.Refresh();
        yield return "cargoStatus";
    }
    if (config.Enabled("health")) {
        blockHealth.Refresh();
        yield return "blockHealth";
    }
    if (config.Enabled("production")) {
        productionDetails.Refresh();
        yield return "productionDetails";
    }
    if (config.Enabled("gas")) {
        gasStatus.Refresh();
        yield return "gasStatus";
    }


    DrawingSurface.Options themeOpts = null;
    string screenConfig = config.Get("config");

    if (screenConfig != null) {
        themeOpts = template.StringToOptions(screenConfig);
    }

    for (int j = 0; j < drawables.Count; ++j) {
        var dw = drawables.ElementAt(j);
        if (themeOpts != null) {
            template.ConfigureScreen(dw.Value, themeOpts);
        }
        template.Render(dw.Value);

        yield return $"render {dw.Key}";
    }
    config.Set("config", null);

    yield break;
}
/* ENUMERATOR */
