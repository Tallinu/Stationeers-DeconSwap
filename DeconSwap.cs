using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using LaunchPadBooster;

namespace DeconSwap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class DeconSwapPlugin : BaseUnityPlugin
    {
        public static DeconSwapPlugin Instance;
        public const string PluginGuid = "stationeers.DeconSwap";
        public const string PluginName = "Frame & Wall DeconSwap";
        public const string PluginVersion = "1.2.3";
        private const string logPrefix = "[F&WDeconSwap] ";
        public static Mod mod = new Mod(PluginGuid, PluginVersion);


        public static ConfigEntry<int> grindTimeMultiplier;


        public static void Log(string line)
        {
            Debug.Log(logPrefix + line);
        }
        public static void LogWarning(string line)
        {
            Debug.LogWarning(logPrefix + line);
        }
        public static void LogError(string line)
        {
            Debug.LogError(logPrefix + line);
        }

        void Awake()
        {
            Instance = this;
            mod.SetMultiplayerRequired(); // Return to non-SLPBooster default behavior of not allowing MP join unless both parties have the mod!

            try
            {
                // Harmony.DEBUG = true;
                var harmony = new Harmony(PluginGuid);
                harmony.PatchAll();
                Log("Patch succeeded");
            }
            catch (Exception e)
            {
                LogError("Patch Failed");
                LogError(e.ToString());
            }

            grindTimeMultiplier = this.Config.Bind(new ConfigDefinition("TimeScale", "FrameGrindMultiplier"), 150, new ConfigDescription(
                "Percentage scalar to the time required to open an airtight, fully welded frame, to help avoid accidental decompression.\n"
                + "This affects only the final airtight build state, not the two incomplete states.\n"
                + "\n100% disables this feature, leaving the time unchanged at 0.5 second.\n"
                + "Default: 150% (0.75 sec) Max: 400% (2 sec)\n"
                + "\nHold Control & Left Click the slider to type a value.\n"
                + "\nChanges apply AFTER RESTART of Stationeers.", new AcceptableValueRange<int>(100, 400))
            );
        }
    }
}