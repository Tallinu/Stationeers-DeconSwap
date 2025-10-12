using Assets.Scripts.Objects;
using HarmonyLib;
using JetBrains.Annotations;
using LaunchPadBooster.Utils;
using Objects.Electrical;
using Objects.Structures;

namespace DeconSwap
{
    [HarmonyPatch(typeof(Prefab))]
    public static class PrefabLoadAllPatch
    {
        private static int changeCount = 0;
        private const int expectedChanges = 92;

        [HarmonyPatch("LoadAll")]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void LoadAllPatch()
        {
            if (changeCount != 0)
            {
                DeconSwapPlugin.logger.LogError("More than one copy of this plugin is being loaded! Please remove the extras. Check your workshop subscriptions,"
                    + " the BepInEx plugins folder, and the Stationeers mods folder (next to the saves folder in Documents/My Games).");
                return;
            }

            //Search prefabs list for structures to modify.
            foreach (Thing thing in WorldManager.Instance.SourcePrefabs)
            {
                if (thing is Frame frame && thing.PrefabName.StartsWith("StructureFrame"))
                {
                    //Frames are handled here.
                    DeconSwapPlugin.logger.LogInfo("Found frame " + frame.PrefabName + " with " + frame.BuildStates.Count + " build state" + (frame.BuildStates.Count != 1 ? "s." : "."));
                    SwapAllTools(frame, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    if (frame.BuildStates.Count > 2 && DeconSwapPlugin.grindTimeMultiplier.Value > 100)
                    {
                        float newtime = (float)(((double)DeconSwapPlugin.grindTimeMultiplier.Value / 100.0) * frame.BuildStates[2].Tool.ExitTime);
                        DeconSwapPlugin.logger.LogInfo("  Increasing build state 2 dismantle time from " + frame.BuildStates[2].Tool.ExitTime + " to " + newtime);
                        frame.BuildStates[2].Tool.ExitTime = newtime;
                    }
                }
                else if ((thing is Wall || thing is WallPillar)
                      && (thing.PrefabName.StartsWith("StructureWall") /* Main wall whitelist */
                      || thing.PrefabName.StartsWith("StructureComposite")
                      || thing.PrefabName.StartsWith("StructureReinforced")) /* Next, blacklist of some that otherwise match: */
                      && !thing.PrefabName.Equals("StructureReinforcedWall") /* Already uses desired tools, don't swap them */
                      && !thing.PrefabName.Equals("StructureWallSmallPanelsOpen") /* Dismantled with crowbar */
                      && !thing.PrefabName.StartsWith("StructureCompositeWallType") /* Dismantled with crowbar */
                      && !thing.PrefabName.StartsWith("StructureCompositeFloorGrating")) /* Dismantled with crowbar */
                {
                    if (thing is Wall wall)
                    {
                        //Normal walls of all kinds, and 2 of 3 window shutter pieces get handled here.
                        DeconSwapPlugin.logger.LogInfo("Found wall " + wall.PrefabName + " with " + wall.BuildStates.Count + " build state" + (wall.BuildStates.Count != 1 ? "s." : "."));
                        SwapAllTools(wall, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                    else if (thing is WallPillar wallp)
                    {
                        //These don't use the Wall class and have to be handled separately
                        DeconSwapPlugin.logger.LogInfo("Found pillar " + wallp.PrefabName + " with " + wallp.BuildStates.Count + " build state" + (wallp.BuildStates.Count != 1 ? "s." : "."));
                        SwapAllTools(wallp, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                }
                else if (thing is WindowShutterController wsc)
                {
                    //Special case to swap this even though it's not a Wall class, because all window shutter parts are made from the same kit,
                    //and all of their Build State 0's are normally dismantled with the same tool (grinder) just like regular walls are.
                    int count = wsc.BuildStates.Count;
                    DeconSwapPlugin.logger.LogInfo("Found wall " + wsc.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                    SwapAllTools(wsc, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if ((thing is LandingPadModular pad) && thing.PrefabName.StartsWith("Landingpad_") && !thing.PrefabName.Equals("Landingpad_2x2CenterPiece01"))
                {
                    //Landing pad parts also use grinder to pick up initial placement (except new large tank) and wrench to unweld steel sheets.
                    DeconSwapPlugin.logger.LogInfo("Found pad " + pad.PrefabName + " with " + pad.BuildStates.Count + " build state" + (pad.BuildStates.Count != 1 ? "s." : "."));
                    if (pad.PrefabName.Equals("Landingpad_GasCylinderTankPiece"))
                    {
                        // Landingpad_GasCylinderTankPiece - Wrench (tank), Grinder (base) - Change BS 0 grinder to wrench, BS 1 wrench to drill
                        SwapTools(pad, 0, PrefabNames.AngleGrinder, PrefabNames.Wrench);
                        SwapTools(pad, 1, PrefabNames.Wrench, PrefabNames.Drill);
                    }
                    else if (pad.PrefabName.Equals("Landingpad_LargeTank"))
                    {
                        // Landingpad_LargeTank - Grinder (welded steel sheets, good), wrench (tanks), Hand Drill (base) - swap BS 0 and 1 (wrench, drill)
                        SwapTools(pad, 0, PrefabNames.Drill, PrefabNames.Wrench);
                        SwapTools(pad, 1, PrefabNames.Wrench, PrefabNames.Drill);
                    }
                    else
                    {
                        SwapAllTools(pad, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                }
                else if (thing is LandingPadModularDevice paddev && thing.PrefabName.StartsWith("Landingpad_"))
                {
                    //'Data & Power' and runway 'Threshhold' (yes, with an extra H) are a separate class
                    DeconSwapPlugin.logger.LogInfo("Found pad " + paddev.PrefabName + " with " + paddev.BuildStates.Count + " build state" + (paddev.BuildStates.Count != 1 ? "s." : "."));
                    SwapAllTools(paddev, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is LandingPadPump padpump && thing.PrefabName.StartsWith("Landingpad_"))
                {
                    //So are the gas/liquid in/out pump parts
                    DeconSwapPlugin.logger.LogInfo("Found pad " + padpump.PrefabName + " with " + padpump.BuildStates.Count + " build state" + (padpump.BuildStates.Count != 1 ? "s." : "."));
                    SwapAllTools(padpump, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
            }

            if (changeCount == expectedChanges)
                DeconSwapPlugin.logger.LogInfo("Modified " + changeCount + " build states (this is the expected result).");
            else if (changeCount > 0)
                DeconSwapPlugin.logger.LogWarning("Unexpected: Modified " + changeCount + " build states (expected: " + expectedChanges + "). Parts added by other mods or game updates may have been affected.");
            else
                DeconSwapPlugin.logger.LogError("Unexpected: No build states were modified!");
        }

        private static void SwapTools(Structure structure, int state, string tool1, string tool2)
        {
            if (structure.BuildStates[state].Tool.ToolExit?.PrefabName == tool1)
            {
                DeconSwapPlugin.logger.LogInfo("  Swapping build state " + state + " dismantle tool from " + tool1 + " to " + tool2 + ".");
                structure.SetExitTool(tool2, state);
                changeCount++;
            }
            else if (structure.BuildStates[state].Tool.ToolExit?.PrefabName == tool2)
            {
                DeconSwapPlugin.logger.LogInfo("  Swapping build state " + state + " dismantle tool from " + tool2 + " to " + tool1 + ".");
                structure.SetExitTool(tool1, state);
                changeCount++;
            }
        }

        private static void SwapAllTools(Structure structure, string tool1, string tool2)
        {
            //Receives list of build states from prefab search and swaps dismantling tool (ToolExit) used, if present.
            for (int i = 0; i < structure.BuildStates.Count; i++)
            {
                SwapTools(structure, i, tool1, tool2);
            }
        }

    }
}

