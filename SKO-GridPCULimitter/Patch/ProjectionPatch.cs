using System.Collections.Generic;
using System.Reflection;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SKO.Torch.Shared.Utils;
using Torch;
using Torch.Managers;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Network;

namespace SKO.GridPCULimiter.Patch
{
    [PatchShim]
    public static class ProjectionPatch
    {
        private static readonly MethodInfo RemoveProjectionMethod =
            typeof(MyProjectorBase).GetMethod("OnRemoveProjectionRequest",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyProjectorBase).GetMethod("OnNewBlueprintSuccess",
                BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(
                typeof(ProjectionPatch).GetMethod("OnBluePrintSuccess",
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool OnBluePrintSuccess(MyProjectorBase __instance,
            ref List<MyObjectBuilder_CubeGrid> projectedGrids)
        {
            if (!SKOGridPCULimiterPlugin.Config.Enabled || SKOGridPCULimiterPlugin.Config.AllowProjection) return true;

            var _instance = __instance;
            if (_instance == null)
            {
                SKOGridPCULimiterPlugin.Log.Warn("PrefixNewBlueprint: Projector instance is NULL");
                return false;
            }

            var grid = projectedGrids[0];
            if (grid == null)
            {
                SKOGridPCULimiterPlugin.Log.Warn("PrefixNewBlueprint: Grid is NULL");
                return false;
            }

            if (SKOGridPCULimiterPlugin.Config.IgnoreNPCGrids)
            {
                var ownerId = PlayerUtils.GetOwner(__instance.CubeGrid);
                if (PlayerUtils.IsNpc(ownerId)) return true;
            }

            var value = MyEventContext.Current.Sender.Value;
            var player = MySession.Static.Players.TryGetPlayerBySteamId(value);
            var cubeBlocks = grid.CubeBlocks;
            if (player == null || cubeBlocks.Count == 0) return false;

            var projectionPCU = GridUtils.GetPCU(_instance.CubeGrid);
            if (projectionPCU >= SKOGridPCULimiterPlugin.Config.MaxGridPCU)
            {
                if (player?.Identity?.IdentityId > 0)
                {
                    SoundUtils.SendTo(player.Identity.IdentityId);
                    ChatUtils.SendTo(player.Identity.IdentityId, "Projection disabled.");
                    ChatUtils.SendTo(player.Identity.IdentityId, "Projected Grid PCU Limit reached.");
                }

                RemoveProjection(__instance, grid);
                return false;
            }

            var baseGridPCU = GridUtils.GetPCU(__instance.CubeGrid, true,
                SKOGridPCULimiterPlugin.Config.IncludeConnectedGridsPCU);
            if (baseGridPCU >= SKOGridPCULimiterPlugin.Config.MaxGridPCU)
            {
                if (player?.Identity?.IdentityId > 0)
                {
                    SoundUtils.SendTo(player.Identity.IdentityId);
                    ChatUtils.SendTo(player.Identity.IdentityId, "Projection disabled.");
                    ChatUtils.SendTo(player.Identity.IdentityId, "Base Grid PCU Limit reached.");
                }

                RemoveProjection(__instance, grid);
                return false;
            }

            if (baseGridPCU + projectionPCU >= SKOGridPCULimiterPlugin.Config.MaxGridPCU)
            {
                if (player?.Identity?.IdentityId > 0)
                {
                    SoundUtils.SendTo(player.Identity.IdentityId);
                    ChatUtils.SendTo(player.Identity.IdentityId, "Projection disabled.");
                    ChatUtils.SendTo(player.Identity.IdentityId, "Projected grid exceeds PCU limit of the base grid.");
                }

                RemoveProjection(__instance, grid);
                return false;
            }

            return true;
        }

        public static void RemoveProjection(MyProjectorBase projector, MyObjectBuilder_CubeGrid grid)
        {
            // Dirty dirty dirty :( but this handles the expected result in the UI.
            // Might required adjustments:
            // TODO: Check this with one of the Torch developers.
            MyAPIGateway.Parallel.Do(() =>
            {
                MyAPIGateway.Parallel.Sleep(1000);
                projector.Enabled = false;
                projector.SelectPrefab(string.Empty);
                projector.SelectBlueprint();
                projector.RaisePropertiesChanged();
                NetworkManager.RaiseEvent(projector, RemoveProjectionMethod);
            });
        }
    }
}