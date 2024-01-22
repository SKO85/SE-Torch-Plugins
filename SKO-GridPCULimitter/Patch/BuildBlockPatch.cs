using Sandbox.Definitions;
using Sandbox.Game.Entities;
using SKO.Torch.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Torch.Managers.PatchManager;
using VRage.Network;

namespace SKO.GridPCULimiter.Patch
{
    [PatchShim]
    public static class BuildBlockPatch
    {
        public static void Patch(PatchContext ctx)
        {
            var typeFromHandle = typeof(MyCubeGrid);
            var aMethod = typeFromHandle.GetMethod("BuildBlocksRequest",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            ctx.GetPattern(aMethod).Prefixes.Add(typeof(BuildBlockPatch).GetMethod("BuildBlocksRequest",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool BuildBlocksRequest(MyCubeGrid __instance, HashSet<MyCubeGrid.MyBlockLocation> locations)
        {
            if (!SKOGridPCULimiterPlugin.Config.Enabled) return true;

            if (__instance == null)
            {
                SKOGridPCULimiterPlugin.Log.Warn("BuildBlocksRequest: Grid is NULL.");
                return true;
            }

            var definition =
                MyDefinitionManager.Static.GetCubeBlockDefinition(locations.FirstOrDefault().BlockDefinition);

            if (definition == null)
            {
                SKOGridPCULimiterPlugin.Log.Warn("BuildBlocksRequest: Definition is NULL.");
                return true;
            }

            if (SKOGridPCULimiterPlugin.Config.IgnoreNPCGrids)
            {
                var ownerId = PlayerUtils.GetOwner(__instance);
                if (PlayerUtils.IsNpc(ownerId)) return true;
            }

            var steamId = MyEventContext.Current.Sender.Value;
            var playerId = PlayerUtils.GetIdentityByNameOrId(steamId.ToString()).IdentityId;
            var player = PlayerUtils.GetPlayer(playerId);
            if (player != null && PlayerUtils.IsAdmin(player) &&
                PlayerUtils.IsPCULimitIgnored(player.SteamUserId)) return true;

            if (GridUtils.GetPCU(__instance, true, SKOGridPCULimiterPlugin.Config.IncludeConnectedGridsPCU) >=
                SKOGridPCULimiterPlugin.Config.MaxGridPCU)
            {
                if (playerId > 0)
                {
                    SoundUtils.SendTo(playerId);
                    ChatUtils.SendTo(playerId, "Grid PCU Limit reached.");
                }

                return false;
            }

            return true;
        }
    }
}