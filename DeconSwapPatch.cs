using Assets.Scripts.Objects;
using HarmonyLib;
using JetBrains.Annotations;
using Objects.Structures;
using System.Collections.Generic;

namespace DeconSwap.Scripts
{
    [HarmonyPatch(typeof(Prefab))]
    public static class PrefabLoadAllPatch
    {
        private static Item wrench;
        private static Item grinder;
        private static int changeCount = 0;

        [HarmonyPatch("LoadAll")]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void LoadAllPatch()
        {
            //Find the tool references used for replacement
            wrench  = (Item)(WorldManager.Instance.SourcePrefabs.Find(x => x.PrefabName.Equals("ItemWrench")));
            grinder = (Item)(WorldManager.Instance.SourcePrefabs.Find(x => x.PrefabName.Equals("ItemAngleGrinder")));
            if (wrench == null || grinder == null)
            {
                DeconSwapPlugin.LogError("Failed to find needed tools. Wrench: " + (wrench != null ? wrench.PrefabName : "NULL")
                    + " Grinder: " + (grinder != null ? grinder.PrefabName : "NULL"));
                wrench = null;
                grinder = null;
                return;
            }
            DeconSwapPlugin.Log("Found tools. Wrench: " + wrench.PrefabName + " Grinder: " + grinder.PrefabName);

            //Search prefabs list for structures to modify
            foreach (Thing thing in WorldManager.Instance.SourcePrefabs)
            {
                if (thing is Frame frame && thing.PrefabName.StartsWith("StructureFrame"))
                {
                    //Frames handled here.
                    DeconSwapPlugin.Log("Found frame " + frame.PrefabName + " with " + frame.BuildStates.Count + " build state" + (frame.BuildStates.Count != 1 ? "s." : "."));
                    SwapTool(frame.BuildStates);
                    if (frame.BuildStates.Count > 2)
                    {
                        float newtime = frame.BuildStates[2].Tool.ExitTime * 1.5f;
                        DeconSwapPlugin.Log("  Increasing build state 2 dismantle time from " + frame.BuildStates[2].Tool.ExitTime + " to " + newtime);
                        frame.BuildStates[2].Tool.ExitTime = newtime;
                    }
                }
                else if ((thing is Wall || thing is WallPillar)
                      && (thing.PrefabName.StartsWith("StructureWall")
                      || thing.PrefabName.StartsWith("StructureComposite")
                      || thing.PrefabName.StartsWith("StructureReinforced")))
                {
                    if (thing is Wall wall)
                    {
                        //Normal walls of all kinds, and 2 of 3 window shutter pieces get handled here.
                        //Floor gratings also meet the search criteria, but remain unaltered since they're dismantled with a crowbar.
                        DeconSwapPlugin.Log("Found wall " + wall.PrefabName + " with " + wall.BuildStates.Count + " build state" + (wall.BuildStates.Count != 1 ? "s." : "."));
                        SwapTool(wall.BuildStates);
                    }
                    else if (thing is WallPillar wallp)
                    {
                        //These don't count as a Wall class and have to be handled separately
                        DeconSwapPlugin.Log("Found wall pillar " + wallp.PrefabName + " with " + wallp.BuildStates.Count + " build state" + (wallp.BuildStates.Count != 1 ? "s." : "."));
                        SwapTool(wallp.BuildStates);
                    }
                }
                else if (thing is WindowShutterController wsc)
                {
                    //Special casing this because internally it's not a Wall, but window shutter parts are all made from the same kit,
                    //and all of their Build State 0's are normally dismantled with the same tool (grinder) just like plain walls are.
                    int count = wsc.BuildStates.Count;
                    DeconSwapPlugin.Log("Found wall " + wsc.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                    SwapTool(wsc.BuildStates);
                }
            }

            if (changeCount > 0)
                DeconSwapPlugin.Log("Modified " + changeCount + " build states.");
            else
                DeconSwapPlugin.LogWarning("Unexpected: No build states were modified!");
            wrench = null;
            grinder = null;
        }

        private static void SwapTool(List<BuildState> states)
        {
            //Receives list of build states from prefab search and swaps dismantling tool (ToolExit) used if wrench or grinder is found there.
            for (int i = 0; i < states.Count; i++)
            {
                if (ReferenceEquals(states[i].Tool.ToolExit, wrench))
                {
                    DeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from wrench to grinder.");
                    states[i].Tool.ToolExit = grinder;
                    changeCount++;
                }
                else if (ReferenceEquals(states[i].Tool.ToolExit, grinder))
                {
                    DeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from grinder to wrench.");
                    states[i].Tool.ToolExit = wrench;
                    changeCount++;
                }
            }
        }

    }
}

