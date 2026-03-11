using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TMPro;

using UnityEngine;

namespace ChangeOptimizer;

static class RegisterScreenManager
{
    static readonly Dictionary<int, TextMeshProUGUI> _panels = [];
    static readonly HashSet<int> _initialized = [];
    static readonly Dictionary<int, List<(int denom, int count)>> _changeGroups = [];
    static readonly Dictionary<int, Dictionary<int, int>> _collected = [];

    const float ReservedBottom = 0.18f;

    public static void SetupScreen(Checkout checkout)
    {
        var regScreen = checkout.GetComponentInChildren<CashRegisterScreen>();
        if (regScreen == null) return;

        int id = checkout.GetInstanceID();
        if (!_initialized.Add(id)) return;

        var canvas = regScreen.GetComponent<Canvas>()
                  ?? regScreen.GetComponentInParent<Canvas>()
                  ?? regScreen.GetComponentInChildren<Canvas>(true);

        if (canvas == null)
        {
            Plugin.Log.LogWarning("[ChangeOptimizer] No Canvas found for CashRegisterScreen");
            return;
        }

        CompressExistingChildren(canvas);
        _panels[id] = CreatePanel(canvas, regScreen);
        Plugin.Log.LogInfo("[ChangeOptimizer] Panel installed");
    }

    // Squish default screen elements upward to leave room for CO panel.
    static void CompressExistingChildren(Canvas canvas)
    {
        float scale = 1f - ReservedBottom;
        int count = canvas.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            var child = canvas.transform.GetChild(i);
            var rt = child.GetComponent<RectTransform>();
            if (rt == null) continue;

            var min = rt.anchorMin;
            var max = rt.anchorMax;

            min.y = ReservedBottom + min.y * scale;
            max.y = ReservedBottom + max.y * scale;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y * scale);
            rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y * scale);

            // Scale font sizes within this child so text fits the smaller container
            foreach (TMP_Text tmp in child.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.enableAutoSizing) {
                    tmp.fontSizeMax *= scale;
                    tmp.fontSizeMin *= scale;
                } else
                    tmp.fontSize *= scale;
            }
        }
    }

    static TextMeshProUGUI CreatePanel(Canvas canvas, CashRegisterScreen regScreen)
    {
        var canvasRt = canvas.GetComponent<RectTransform>();

        var bg = new GameObject("OptimalChangeBG");
        bg.transform.SetParent(canvasRt, false);

        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(1f, ReservedBottom);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.184f, 0.345f, 0.424f, 1f);

        var textGo = new GameObject("OptimalChangeText");
        textGo.transform.SetParent(bg.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.03f, 0f);
        textRt.anchorMax = new Vector2(0.97f, 1f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        var existingTexts = regScreen.GetComponentsInChildren<TMP_Text>(true);
        if (existingTexts.Length > 0)
            tmp.font = existingTexts[0].font;

        tmp.enableAutoSizing   = true;
        tmp.fontSizeMin        = 8f;
        tmp.fontSizeMax        = 32f;
        tmp.alignment          = TextAlignmentOptions.Midline;
        tmp.color              = Color.white;
        tmp.enableWordWrapping = true;
        tmp.overflowMode       = TextOverflowModes.Ellipsis;
        tmp.text               = "Optimal Change";

        bg.SetActive(true);
        return tmp;
    }

    public static void ShowChange(Checkout checkout, int changeCents)
    {
        var panel = GetPanel(checkout);
        if (panel == null) return;

        int id = checkout.GetInstanceID();

        _changeGroups.Remove(id);
        _collected.Remove(id);

        if (changeCents <= 0)
        {
            _collected[id] = [];
            panel.text = Plugin.HappyMessage.Value;
        }
        else
        {
            var groups = ChangeCalculator.GetOptimalChange(changeCents)
                .GroupBy(c => c)
                .OrderByDescending(g => g.Key)
                .Select(g => (denom: g.Key, count: g.Count()))
                .ToList();

            _changeGroups[id] = groups;
            _collected[id] = [];
            panel.text = BuildChangeString(groups, _collected[id]);
        }

        panel.transform.parent.gameObject.SetActive(true);
    }

    public static void TrackCoin(Checkout checkout, int denomCents, bool add)
    {
        int id = checkout.GetInstanceID();

        if (!_collected.TryGetValue(id, out var collected)) return;

        collected.TryGetValue(denomCents, out int current);
        collected[denomCents] = Math.Max(0, current + (add ? 1 : -1));

        var panel = GetPanel(checkout);
        if (panel == null) return;

        // why would you be doin' that
        if (!_changeGroups.ContainsKey(id))
        {
            bool anyCoins = collected.Any(kvp => kvp.Value > 0);
            panel.text = anyCoins ? "Why are you giving them money?!" : Plugin.HappyMessage.Value;
            return;
        }

        if (Plugin.ShowHappyOnExactChange.Value && IsChangeComplete(_changeGroups[id], collected))
            panel.text = Plugin.HappyMessage.Value;
        else
            panel.text = BuildChangeString(_changeGroups[id], collected);
    }

    public static void ResetChange(Checkout checkout)
    {
        int id = checkout.GetInstanceID();

        _changeGroups.Remove(id);
        _collected.Remove(id);

        var panel = GetPanel(checkout);

        if (panel != null)
            panel.text = "Optimal Change";
    }

    static bool IsChangeComplete(List<(int denom, int count)> groups, Dictionary<int, int> collected)
    {
        foreach (var (denom, needed) in groups)
        {
            collected.TryGetValue(denom, out int given);

            if (given != needed)
                return false;
        }

        HashSet<int> groupDenoms = [.. groups.Select(g => g.denom)];

        foreach (var (denom, count) in collected)
        {
            if (!groupDenoms.Contains(denom) && count > 0)
                return false;
        }

        return true;
    }

    static string BuildChangeString(List<(int denom, int count)> groups, Dictionary<int, int> collected)
    {
        var sb = new StringBuilder();
        bool first = true;
        HashSet<int> groupDenoms = [.. groups.Select(g => g.denom)];

        foreach (var (denom, needed) in groups)
        {
            if (!first) sb.Append("  ");
            first = false;
            collected.TryGetValue(denom, out int given);
            string label = FormatDenom(denom, needed);
            string entry = given == needed ? $"<color=#00e676>{label}</color>"  // green  — exact
                         : given > needed  ? $"<color=#ef5350>{label}</color>"  // red — over
                         : given > 0       ? $"<color=#ffeb3b>{label}</color>"  // yellow — under
                         :                   label;                             // white  — nothing
            sb.Append(entry);
        }

        // Coins not in the optimal set — shown in red
        foreach (var (denom, count) in collected.OrderByDescending(k => k.Key))
        {
            if (groupDenoms.Contains(denom) || count <= 0) continue;
            if (!first) sb.Append("  ");
            first = false;
            sb.Append($"<color=#ef5350>{FormatDenom(denom, count)}</color>");
        }

        return sb.ToString();
    }

    static string FormatDenom(int cents, int count)
    {
        string denomStr = cents >= 100 ? $"${cents / 100}" : $"{cents}\u00a2";
        return $"{count}x{denomStr}";
    }

    static TextMeshProUGUI GetPanel(Checkout checkout)
    {
        _panels.TryGetValue(checkout.GetInstanceID(), out var panel);
        return panel;
    }
}
