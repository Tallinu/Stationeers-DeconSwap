using Assets.Scripts.Objects;
using HarmonyLib;
using JetBrains.Annotations;
using Objects.Electrical;
using Objects.Structures;
using System;
using System.Collections.Generic;

namespace DeconSwap
{
    [HarmonyPatch(typeof(Prefab))]
    public static class PrefabLoadAllPatch
    {
        private static Item wrench;
        private static Item grinder;
        private static Item drill;
        private static int changeCount = 0;

        [HarmonyPatch("LoadAll")]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void LoadAllPatch()
        {
            if (changeCount != 0)
            {
                DeconSwapPlugin.LogError("More than one copy of this plugin is being loaded! Please remove the extras. Check your workshop subscriptions,"
                    + " the BepInEx plugins folder, and the Stationeers mods folder (next to the saves folder in Documents/My Games).");
                return;
            }

            //Find the tool references used for replacement.
            wrench = (Item)WorldManager.Instance.SourcePrefabs.Find(x => x?.PrefabName?.Equals("ItemWrench") ?? false);
            grinder = (Item)WorldManager.Instance.SourcePrefabs.Find(x => x?.PrefabName?.Equals("ItemAngleGrinder") ?? false);
            drill = (Item)WorldManager.Instance.SourcePrefabs.Find(x => x?.PrefabName?.Equals("ItemDrill") ?? false);
            if (wrench == null || grinder == null || drill == null)
            {
                DeconSwapPlugin.LogError("Failed to find needed tool(s):"
                    + (wrench == null ? " ItemWrench" : "") + (grinder == null ? " ItemAngleGrinder" : "") + (drill == null ? " ItemDrill" : ""));
                wrench = null;
                grinder = null;
                drill = null;
                return;
            }
            DeconSwapPlugin.Log("Found tools.");

            //Search prefabs list for structures to modify.
            foreach (Thing thing in WorldManager.Instance.SourcePrefabs)
            {
                if (thing is Frame frame && thing.PrefabName.StartsWith("StructureFrame"))
                {
                    //Frames are handled here.
                    DeconSwapPlugin.Log("Found frame " + frame.PrefabName + " with " + frame.BuildStates.Count + " build state" + (frame.BuildStates.Count != 1 ? "s." : "."));
                    SwapAllTools(frame.BuildStates, wrench, grinder);
                    if (frame.BuildStates.Count > 2)
                    {
                        float newtime = frame.BuildStates[2].Tool.ExitTime * 1.5f;
                        DeconSwapPlugin.Log("  Increasing build state 2 dismantle time from " + frame.BuildStates[2].Tool.ExitTime + " to " + newtime);
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
                        DeconSwapPlugin.Log("Found wall " + wall.PrefabName + " with " + wall.BuildStates.Count + " build state" + (wall.BuildStates.Count != 1 ? "s." : "."));
                        SwapAllTools(wall.BuildStates, wrench, grinder);
                    }
                    else if (thing is WallPillar wallp)
                    {
                        //These don't use the Wall class and have to be handled separately
                        DeconSwapPlugin.Log("Found pillar " + wallp.PrefabName + " with " + wallp.BuildStates.Count + " build state" + (wallp.BuildStates.Count != 1 ? "s." : "."));
                        SwapAllTools(wallp.BuildStates, wrench, grinder);
                    }
                }
                else if (thing is WindowShutterController wsc)
                {
                    //Special case to swap this even though it's not a Wall class, because all window shutter parts are made from the same kit,
                    //and all of their Build State 0's are normally dismantled with the same tool (grinder) just like regular walls are.
                    int count = wsc.BuildStates.Count;
                    DeconSwapPlugin.Log("Found wall " + wsc.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                    SwapAllTools(wsc.BuildStates, wrench, grinder);
                }
                else if ((thing is LandingPadModular pad) && thing.PrefabName.StartsWith("Landingpad_") && !thing.PrefabName.Equals("Landingpad_2x2CenterPiece01"))
                {
                    //Landing pad parts also use grinder to pick up initial placement (except new large tank) and wrench to unweld steel sheets.
                    DeconSwapPlugin.Log("Found pad " + pad.PrefabName + " with " + pad.BuildStates.Count + " build state" + (pad.BuildStates.Count != 1 ? "s." : "."));
                    if (pad.PrefabName.Equals("Landingpad_GasCylinderTankPiece"))
                    {
                        // Landingpad_GasCylinderTankPiece - Wrench (tank), Grinder (base) - Change BS 0 grinder to wrench, BS 1 wrench to drill
                        SwapTools(pad.BuildStates, 0, grinder, wrench);
                        SwapTools(pad.BuildStates, 1, wrench, drill);
                    }
                    else if (pad.PrefabName.Equals("Landingpad_LargeTank"))
                    {
                        // Landingpad_LargeTank - Grinder (welded steel sheets, good), wrench (tanks), Hand Drill (base) - swap BS 0 and 1 (wrench, drill)
                        SwapTools(pad.BuildStates, 0, drill, wrench);
                        SwapTools(pad.BuildStates, 1, wrench, drill);
                    }
                    else
                    {
                        SwapAllTools(pad.BuildStates, wrench, grinder);
                    }
                }
                else if (thing is LandingPadPump pump && thing.PrefabName.StartsWith("Landingpad_"))
                {
                    DeconSwapPlugin.Log("Found pad " + pump.PrefabName + " with " + pump.BuildStates.Count + " build state" + (pump.BuildStates.Count != 1 ? "s." : "."));
                    SwapAllTools(pump.BuildStates, wrench, grinder);
                }
            }

            if (changeCount == 89)
                DeconSwapPlugin.Log("Modified " + changeCount + " build states (this is the expected result).");
            else if (changeCount > 0)
                DeconSwapPlugin.LogWarning("Unexpected: Modified " + changeCount + " build states (expected: 89). Parts added by other mods may have been affected.");
            else
                DeconSwapPlugin.LogError("Unexpected: No build states were modified!");
            wrench = null; // Cleanup references? Does this do any good?
            grinder = null;
            drill = null;
        }

        private static void SwapTools(List<BuildState> states, int i, Item tool1, Item tool2)
        {
            if (ReferenceEquals(states[i].Tool.ToolExit, tool1))
            {
                DeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from " + tool1.PrefabName + " to " + tool2.PrefabName + ".");
                states[i].Tool.ToolExit = tool2;
                changeCount++;
            }
            else if (ReferenceEquals(states[i].Tool.ToolExit, tool2))
            {
                DeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from " + tool2.PrefabName + " to " + tool1.PrefabName + ".");
                states[i].Tool.ToolExit = tool1;
                changeCount++;
            }
        }

        private static void SwapAllTools(List<BuildState> states, Item tool1, Item tool2)
        {
            //Receives list of build states from prefab search and swaps dismantling tool (ToolExit) used, if present.
            for (int i = 0; i < states.Count; i++)
            {
                SwapTools(states, i, tool1, tool2);
            }
        }

    }
}

