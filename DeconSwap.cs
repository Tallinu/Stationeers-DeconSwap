using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using LaunchPadBooster;

namespace DeconSwap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class DeconSwapPlugin : BaseUnityPlugin
    {
        public static DeconSwapPlugin Instance;
        public const string PluginGuid = "stationeers.DeconSwap";
        public const string PluginName = "DeconSwap";
        public const string PluginVersion = "1.3.1";
        public static Mod mod = new Mod(PluginGuid, PluginVersion);

        public static BepInEx.Logging.ManualLogSource logger = new BepInEx.Logging.ManualLogSource("DeconSwap");

        public static ConfigEntry<int> grindTimeMultiplier;
        //public static ConfigEntry<bool> extraLogOutputs;

        void Awake()
        {
            Instance = this;
            mod.SetMultiplayerRequired(); // Return to non-SLPBooster default behavior of not allowing MP join unless both parties have the mod!

            BepInEx.Logging.Logger.Sources.Add(logger);

            try
            {
                // Harmony.DEBUG = true;
                var harmony = new Harmony(PluginGuid);
                harmony.PatchAll();
                logger.LogInfo("Patch Complete.");
            }
            catch (Exception e)
            {
                logger.LogError("Patch Failed!");
                logger.LogError(e.ToString());
            }

            grindTimeMultiplier = this.Config.Bind(new ConfigDefinition("Time Scale", "Frame Grind Multiplier"), 150, new ConfigDescription(
                "Percentage scalar to the time required to open an airtight, fully welded frame, to help avoid accidental decompression.\n"
                + "This affects only the final airtight build state, not the two incomplete states.\n"
                + "\n100% disables this feature, leaving the time unchanged at 0.5 second.\n"
                + "Default: 150% (0.75 sec) Max: 400% (2 sec)\n"
                + "\nHold Control & Left Click the slider to type a value.\n"
                + "\nChanges apply AFTER RESTART of Stationeers.", new AcceptableValueRange<int>(100, 400))
            );
            //extraLogOutputs = this.Config.Bind(new ConfigDefinition("Developer Tools", "Extra Log Outputs"), false, new ConfigDescription(
            //    "This causes the log to show additional information regarding which structures that are not currently handled have build states "
            //    + "with a mismatch between welder and grinder usage.\n\n"
            //    + "This option is useful only for development purposes, and will do nothing but waste a user's time. If it was possible, it would be hidden. :)"
            //    ));
        }
    }
}