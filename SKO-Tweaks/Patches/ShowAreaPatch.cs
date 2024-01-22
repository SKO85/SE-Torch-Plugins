using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Interfaces;
using SKO.Torch.Plugins.Tweaks.Modules;
using SKO.Torch.Shared.Utils;
using SpaceEngineers.Game.Entities.Blocks;
using System.Collections.Concurrent;
using System.Reflection;
using Torch.Managers.PatchManager;

namespace SKO.Torch.Plugins.Tweaks.Patches
{
    [PatchShim]
    public static class ShowAreaPatch
    {
        private static readonly ConcurrentDictionary<long, MyTerminalBlock> BlocksToRemoveArea =
            new ConcurrentDictionary<long, MyTerminalBlock>();

        public static void Patch(PatchContext ctx)
        {
            var aMethod = typeof(MyShipWelder).GetMethod("UpdateBeforeSimulation10",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            var bMethod = typeof(MyShipDrill).GetMethod("UpdateBeforeSimulation100",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            ctx.GetPattern(aMethod).Prefixes.Add(typeof(ShowAreaPatch).GetMethod("MyPatch",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
            ctx.GetPattern(bMethod).Prefixes.Add(typeof(ShowAreaPatch).GetMethod("MyPatch",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool MyPatch(MyTerminalBlock __instance)
        {
            try
            {
                if (__instance == null || !MainModule.Config.PluginEnabled || !MainModule.Config.ShowArea.Enabled ||
                    __instance.Closed)
                    return true;

                if (!SKOTweaksPlugin.ExpireCache.Expired(__instance.EntityId)) return true;

                if (__instance is MyShipDrill || __instance is MyShipWelder)
                {
                    var foundObject = false;
                    var propertyId = string.Empty;

                    if (MainModule.Config.ShowArea.DisableOnBuildAndRepair &&
                        __instance.BlockDefinition.Id.SubtypeName.Contains("BuildAndRepairSystem"))
                    {
                        foundObject = true;
                        propertyId = "BuildAndRepair.ShowArea";
                    }
                    else if (MainModule.Config.ShowArea.DisableOnAdvancedDrills &&
                             __instance.BlockDefinition.Id.SubtypeName.Contains("NanobotDrillSystem"))
                    {
                        foundObject = true;
                        propertyId = "Drill.ShowArea";
                    }

                    if (foundObject)
                    {
                        var showAreaProperty = __instance.GetProperty(propertyId);
                        if (showAreaProperty != null)
                        {
                            if (BlocksToRemoveArea.ContainsKey(__instance.EntityId))
                            {
                                if (MainModule.Config.ShowArea.Log)
                                    SKOTweaksPlugin.Log.Warn(
                                        $"Removing ShowArea for '{__instance.DisplayNameText}' on grid '{__instance.CubeGrid.DisplayName}'");

                                __instance.SetValueBool(propertyId, false);
                                __instance.CubeGrid.RaiseGridChanged();

                                BlocksToRemoveArea.TryRemove(__instance.EntityId, out _);
                                return true;
                            }

                            // Try to queue for removal.
                            var showAreaValue = __instance.GetValueBool(propertyId);
                            if (showAreaValue)
                            {
                                var disable = false;
                                if (MainModule.Config.ShowArea.RemoveOnlyWhenNoPlayersNearby &&
                                    !BlockUtils.PlayersNarby(__instance, 1000))
                                    disable = true;
                                else if (!MainModule.Config.ShowArea.RemoveOnlyWhenNoPlayersNearby) disable = true;

                                if (disable)
                                {
                                    BlocksToRemoveArea[__instance.EntityId] = __instance;

                                    // Give it time to use.
                                    SKOTweaksPlugin.ExpireCache.SetData(__instance,
                                        MainModule.Config.ShowArea.AllowShowAreaIntervalSeconds);
                                    return true;
                                }
                            }
                        }
                    }

                    // Skip for 5 sec.
                    SKOTweaksPlugin.ExpireCache.SetData(__instance, 5);
                }
            }
            catch
            {
            }

            return true;
        }
    }
}