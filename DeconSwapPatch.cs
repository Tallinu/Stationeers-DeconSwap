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
                FrameDeconSwapPlugin.LogError("Failed to find needed tools. Wrench: " + (wrench != null ? wrench.PrefabName : "NULL")
                    + " Grinder: " + (grinder != null ? grinder.PrefabName : "NULL"));
                wrench = null;
                grinder = null;
                return;
            }
            FrameDeconSwapPlugin.Log("Found tools. Wrench: " + wrench.PrefabName + " Grinder: " + grinder.PrefabName);

            //Search prefabs list for structures to modify
            foreach (Thing thing in WorldManager.Instance.SourcePrefabs)
            {
                if (thing is Frame frame && thing.PrefabName.StartsWith("StructureFrame"))
                {
                    //Frames handled here.
                    int count = frame.BuildStates.Count;
                    FrameDeconSwapPlugin.Log("Found Frame " + frame.PrefabName + " with " + frame.BuildStates.Count + " build state" + (count != 1 ? "s." : "."));
                    SwapTool(frame.BuildStates);
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
                        int count = wall.BuildStates.Count;
                        FrameDeconSwapPlugin.Log("Found wall " + wall.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                        SwapTool(wall.BuildStates);
                    }
                    else if (thing is WallPillar wallpillar)
                    {
                        //These don't count as a Wall class and have to be handled separately
                        int count = wallpillar.BuildStates.Count;
                        FrameDeconSwapPlugin.Log("Found wall pillar " + wallpillar.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                        SwapTool(wallpillar.BuildStates);
                    }
                }
                else if (thing is WindowShutterController wsc)
                {
                    //Special casing this because internally it's not a Wall, but window shutter parts are all made from the same kit,
                    //and all of their Build State 0's are normally dismantled with the same tool (grinder) just like plain walls are.
                    int count = wsc.BuildStates.Count;
                    FrameDeconSwapPlugin.Log("Found wall " + wsc.PrefabName + " with " + count + " build state" + (count != 1 ? "s." : "."));
                    SwapTool(wsc.BuildStates);
                }
            }

            if (changeCount > 0)
                FrameDeconSwapPlugin.Log("Modified " + changeCount + " build states.");
            else
                FrameDeconSwapPlugin.LogWarning("Unexpected: No build states were modified!");
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
                    FrameDeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from wrench to grinder.");
                    states[i].Tool.ToolExit = grinder;
                    changeCount++;
                }
                else if (ReferenceEquals(states[i].Tool.ToolExit, grinder))
                {
                    FrameDeconSwapPlugin.Log("  Swapping build state " + i + " dismantle tool from grinder to wrench.");
                    states[i].Tool.ToolExit = wrench;
                    changeCount++;
                }
            }
        }

    }
}

