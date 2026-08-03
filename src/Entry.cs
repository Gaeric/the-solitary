using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace TheSolitary.Scripts;

// Required attribute for mod registration. The string must match the initializer method name.
[ModInitializer(nameof(Init))]
public class Entry
{
    // Initializer method
    public static void Init()
    {
        // Used for patching (i.e. modifying game code).
        // The argument can be arbitrary, just avoid collisions with other mods.
        var harmony = new Harmony("sts2.blaned.thesolitary");
        harmony.PatchAll();
        // Allow .tscn scenes to load custom scripts
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Info("the-solitary mod initialized!");
    }
}
