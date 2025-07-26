using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DeconSwap.Scripts
{
    [BepInPlugin("net.Tallinu.stationeers.DeconSwap.Scripts", "Frame & Wall DeconSwap", "0.1.0")]   
    public class DeconSwapPlugin : BaseUnityPlugin
    {
        public static DeconSwapPlugin Instance;
        private const string header = "[F&WDeconSwap]: ";

        public static void Log(string line)
        {
            Debug.Log(header + line);
        }
        public static void LogWarning(string line)
        {
            Debug.LogWarning(header + line);
        }
        public static void LogError(string line)
        {
            Debug.LogError(header + line);
        }

        void Awake()
        {
            DeconSwapPlugin.Instance = this;
            try
            {
                // Harmony.DEBUG = true;
                var harmony = new Harmony("net.Tallinu.stationeers.DeconSwap.Scripts");
                harmony.PatchAll();
                Log("Patch succeeded");
            }
            catch (Exception e)
            {
                LogError("Patch Failed");
                LogError(e.ToString());
            }
        }
    }
}