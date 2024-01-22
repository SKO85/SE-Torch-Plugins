using SKO.Torch.Shared.Utils;
using SpaceEngineers.Game.Entities.Blocks;
using System.Reflection;
using Torch.Managers.PatchManager;

namespace SKO.GridPCULimiter.Patch
{
    [PatchShim]
    public static class MergeBlockPatch
    {
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyShipMergeBlock).GetMethod("UpdateBeforeSimulation10",
                BindingFlags.Instance | BindingFlags.Public)).Prefixes.Add(
                typeof(MergeBlockPatch).GetMethod("MergeCheck", BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool MergeCheck(MyShipMergeBlock __instance)
        {
            if (!SKOGridPCULimiterPlugin.Config.Enabled || SKOGridPCULimiterPlugin.Config.AllowMerge) return true;

            if ((__instance != null ? __instance.Other : null) == null) return true;

            if (__instance.IsLocked || !__instance.IsFunctional || !__instance.Other.IsFunctional) return true;

            if (SKOGridPCULimiterPlugin.Config.IgnoreNPCGrids)
            {
                var ownerId = PlayerUtils.GetOwner(__instance.CubeGrid);
                if (PlayerUtils.IsNpc(ownerId)) return true;
            }

            if (GridUtils.GetPCU(__instance.CubeGrid, true, SKOGridPCULimiterPlugin.Config.IncludeConnectedGridsPCU) +
                GridUtils.GetPCU(__instance.Other.CubeGrid, true,
                    SKOGridPCULimiterPlugin.Config.IncludeConnectedGridsPCU) >=
                SKOGridPCULimiterPlugin.Config.MaxGridPCU)
            {
                // Disable it to avoid lagging.
                __instance.Enabled = false;

                return false;
            }

            return true;
        }
    }
}