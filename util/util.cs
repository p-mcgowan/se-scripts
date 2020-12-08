/*
 * UTIL
 */
public static class Util {
    public static System.Text.RegularExpressions.Regex surfaceExtractor =
        Util.Regex(@"\s<(\d+)>$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string FormatNumber(VRage.MyFixedPoint input) {
        string fmt;
        int n = Math.Max(0, (int)input);
        if (n < 10000) {
            fmt = "##";
        } else if (n < 1000000) {
            fmt = "###0,K";
        } else {
            fmt = "###0,,M";
        }
        return n.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string TimeFormat(double ms, bool s = false) {
        TimeSpan t = TimeSpan.FromMilliseconds(ms);
        if (t.Hours != 0) {
            return String.Format("{0:D}h{1:D}m", t.Hours, t.Minutes);
        } else if (t.Minutes != 0) {
            return String.Format("{0:D}m", t.Minutes);
        } else {
            return (s ? String.Format("{0:D}s", t.Seconds) : "< 1m");
        }
    }

    public static string ToItemName(MyProductionItem i) {
        string id = i.BlueprintId.ToString();
        if (id.Contains('/')) {
            return id.Split('/')[1];
        }
        return id;
    }

    public static string PctString(float val) {
        return String.Format("{0,3:0}%", 100 * val);
    }

    public static System.Text.RegularExpressions.Regex Regex(
        string pattern,
        System.Text.RegularExpressions.RegexOptions opts = System.Text.RegularExpressions.RegexOptions.None
    ) {
        return new System.Text.RegularExpressions.Regex(pattern, opts);
    }

    public static string Plural(int count, string ifOne, string otherwise) {
        return count == 1 ? ifOne : otherwise;
    }

    public static int ParseInt(string str, int defaultValue = 0) {
        int output;
        if (!int.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }

    public static float ParseFloat(string str, float defaultValue = 0f) {
        float output;
        if (!float.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }

    public static bool ParseBool(string str, bool defaultValue = false) {
        bool output;
        if (!bool.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }
}
}

public static class Dict {
    public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
        TValue value;
        return dict.TryGetValue(key, out value) ? value : defaultValue;
    }

    public static string Print<TKey, TValue>(this Dictionary<TKey, TValue> dict) {
        StringBuilder sb = new StringBuilder("{ ");
        foreach (KeyValuePair<TKey, TValue> keyValues in dict) {
            sb.Append($"{keyValues.Key}: {keyValues.Value}, ");
        }

        return sb.Append("}").ToString();
    }
/* UTIL */
