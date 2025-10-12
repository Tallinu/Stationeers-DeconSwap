using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using JetBrains.Annotations;
using LaunchPadBooster.Utils;
using Objects.Electrical;
using Objects.Rockets;
using Objects.Structures;

namespace DeconSwap
{
    [HarmonyPatch(typeof(Prefab))]
    public static class PrefabLoadAllPatch
    {
        private static int changeCount = 0;
        private const int expectedChanges = 92;
        private static bool ExtraLogOutput = true;

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
                    LogFoundStructure("frame", thing as Structure);
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
                    if (thing is Wall)
                    {
                        //Normal walls of all kinds, and 2 of 3 window shutter pieces get handled here.
                        LogFoundStructure("wall", thing as Structure);
                        SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                    else if (thing is WallPillar)
                    {
                        //These don't use the Wall class and have to be handled separately
                        LogFoundStructure("pillar", thing as Structure);
                        SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                }
                else if (thing is WindowShutterController)
                {
                    //Special case to swap this even though it's not a Wall class, because all window shutter parts are made from the same kit,
                    //and all of their Build State 0's are normally dismantled with the same tool (grinder) just like regular walls are.
                    LogFoundStructure("wall", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if ((thing is LandingPadModular) && thing.PrefabName.StartsWith("Landingpad_") && !thing.PrefabName.Equals("Landingpad_2x2CenterPiece01"))
                {
                    //Landing pad parts also use grinder to pick up initial placement (except new large tank) and wrench to unweld steel sheets.
                    LogFoundStructure("pad", thing as Structure);
                    if (thing.PrefabName.Equals("Landingpad_GasCylinderTankPiece"))
                    {
                        // Landingpad_GasCylinderTankPiece - Wrench (tank), Grinder (base) - Change BS 0 grinder to wrench, BS 1 wrench to drill
                        SwapTools(thing as Structure, 0, PrefabNames.AngleGrinder, PrefabNames.Wrench);
                        SwapTools(thing as Structure, 1, PrefabNames.Wrench, PrefabNames.Drill);
                    }
                    else if (thing.PrefabName.Equals("Landingpad_LargeTank"))
                    {
                        // Landingpad_LargeTank - Grinder (welded steel sheets, good), wrench (tanks), Hand Drill (base) - swap BS 0 and 1 (wrench, drill)
                        SwapTools(thing as Structure, 0, PrefabNames.Drill, PrefabNames.Wrench);
                        SwapTools(thing as Structure, 1, PrefabNames.Wrench, PrefabNames.Drill);
                    }
                    else
                    {
                        SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                    }
                }
                else if (thing is LandingPadModularDevice && thing.PrefabName.StartsWith("Landingpad_"))
                {
                    //'Data & Power' and runway 'Threshhold' (yes, with an extra H) are a separate class
                    LogFoundStructure("pad", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is LandingPadPump && thing.PrefabName.StartsWith("Landingpad_"))
                {
                    //So are the gas/liquid in/out pump parts
                    LogFoundStructure("pad", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is RocketAvionicsDevice && thing.PrefabName.Equals("StructureRocketAvionics"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapTools(thing as Structure, 2, PrefabNames.Drill, PrefabNames.AngleGrinder);
                }
                else if (thing is RocketCelestialTracker && thing.PrefabName.Equals("StructureRocketCelestialTracker"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapTools(thing as Structure, 2, PrefabNames.Drill, PrefabNames.AngleGrinder);
                }
                else if (thing is RocketScanner && thing.PrefabName.Equals("StructureRocketScanner"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapTools(thing as Structure, 1, PrefabNames.Crowbar, PrefabNames.AngleGrinder);
                }
                else if (thing is RocketChuteStorage && thing.PrefabName.Equals("StructureCargoStorageMedium"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapTools(thing as Structure, 1, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is RocketGasCollector && thing.PrefabName.Equals("StructureRocketGasCollector"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapTools(thing as Structure, 1, PrefabNames.AngleGrinder, PrefabNames.Wrench);
                }
                else if (thing is EngineFuselage && thing.PrefabName.Equals("StructureEngineMountTypeA1"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is NoseCone && thing.PrefabName.StartsWith("StructureFairingTypeA"))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (thing is Fuselage && (thing.PrefabName.Equals("StructureFuselageTypeA1")
                                            || thing.PrefabName.Equals("StructureFuselageTypeA2")
                                            || thing.PrefabName.Equals("StructureFuselageTypeA4")
                                            || thing.PrefabName.Equals("StructureFuselageTypeC5")))
                {
                    LogFoundStructure("rocket part", thing as Structure);
                    SwapAllTools(thing as Structure, PrefabNames.Wrench, PrefabNames.AngleGrinder);
                }
                else if (DeconSwapPlugin.extraLogOutputs.Value && thing is Structure struc)
                {
                    //Extra log outputs of build states that break Welder/Grinder Symmetry
                    for (int i = 0; i < struc.BuildStates.Count; i++)
                    {
                        if (struc.BuildStates[i].Tool.ToolEntry?.PrefabName == PrefabNames.Welder
                            && struc.BuildStates[i].Tool.ToolExit?.PrefabName != PrefabNames.AngleGrinder)
                        {
                            DeconSwapPlugin.logger.LogInfo("[Debug] Prefab " + struc.PrefabName + " (Class " + struc.GetType().Name + ") build state " + i + " unwelds with " + struc.BuildStates[i].Tool.ToolExit.PrefabName);
                        }
                    }
                    if (struc.BuildStates[0].Tool.ToolExit?.PrefabName == PrefabNames.AngleGrinder)
                    {
                        DeconSwapPlugin.logger.LogInfo("[Debug] Prefab " + struc.PrefabName + " (Class " + struc.GetType().Name + ") build state 0 grinds initial placement");
                    }
                    if (struc.BuildStates.Count > 1)
                    {
                        for (int i = 1; i < struc.BuildStates.Count; i++)
                        {
                            if (struc.BuildStates[i].Tool.ToolExit?.PrefabName != null
                                && struc.BuildStates[i].Tool.ToolExit?.PrefabName == PrefabNames.AngleGrinder
                                && struc.BuildStates[i].Tool.ToolEntry?.PrefabName != null
                                && struc.BuildStates[i].Tool.ToolEntry?.PrefabName != PrefabNames.Welder)
                            {
                                DeconSwapPlugin.logger.LogInfo("[Debug] Prefab " + struc.PrefabName + " (Class " + struc.GetType().Name + ") build state " + i + " grinds a " + struc.BuildStates[i].Tool.ToolEntry.PrefabName + " job");
                            }
                        }
                    }

                }
            }

            if (changeCount == expectedChanges)
                DeconSwapPlugin.logger.LogInfo("Modified " + changeCount + " build states (this is the expected result).");
            else if (changeCount > 0)
                DeconSwapPlugin.logger.LogWarning("Unexpected: Modified " + changeCount + " build states (expected: " + expectedChanges + "). Parts added by other mods or recent game updates may have been affected.");
            else
                DeconSwapPlugin.logger.LogError("Unexpected: No build states were modified!");
        }

        private static void LogFoundStructure(string what, Structure struc)
        {
            DeconSwapPlugin.logger.LogInfo("Found " + what + ": " + struc.PrefabName + " with " + struc.BuildStates.Count + " build state" + (struc.BuildStates.Count != 1 ? "s." : "."));
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

