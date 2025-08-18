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
        public const string PluginVersion = "1.2.0";
        private const string logPrefix = "[F&WDeconSwap] ";

        public static Mod mod = new Mod(PluginGuid, PluginVersion);


        public static ConfigEntry<double> grindTimeMultiplier;


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
            grindTimeMultiplier = this.Config.Bind(
                new ConfigDefinition("TimeScale", "FrameGrindMultiplier"), 1.5,
                new ConfigDescription("Scales up the time required to open a fully welded frame's airtight build state, to help avoid accidental decompression. A value of 2.0 doubles the time, making it take 1 second instead of 0.5 second.")
            );
        }
    }
}