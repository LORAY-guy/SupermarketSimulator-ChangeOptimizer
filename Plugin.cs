using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;

namespace ChangeOptimizer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<string> HappyMessage;
    internal static ConfigEntry<bool> ShowHappyOnExactChange;

    private void CreateConfig()
    {
        Enabled = Config.Bind("General", "Enabled", true, "Enable or disable the Change Optimizer.");
        HappyMessage = Config.Bind("Miscellaneous", "HappyMessage", "You're good! :)", "The happy message that shows when the customer gives exact change.");
        ShowHappyOnExactChange = Config.Bind("Miscellaneous", "ShowHappyOnExactChange", true, "Whether to show the happy message when giving out exact change.");
    }

    public override void Load()
    {
        Log = base.Log;

        CreateConfig();

        if (!Enabled.Value)
        {
            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} is disabled via config.");
            return;
        }

        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} loaded.");
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
    }
}
