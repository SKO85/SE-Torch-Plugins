using System.Reflection;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SKO.Torch.Plugins.Tweaks.Config;
using SKO.Torch.Plugins.Tweaks.Helpers;
using SKO.Torch.Plugins.Tweaks.Modules;
using Torch.Managers.PatchManager;

namespace SKO.Torch.Plugins.Tweaks.Patches
{
    [PatchShim]
    public static class PistonTensorPatch
    {
        public static void Patch(PatchContext ctx)
        {
            var aMethod = typeof(MyPistonBase).GetMethod("UpdateBeforeSimulation",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ctx.GetPattern(aMethod).Prefixes.Add(typeof(PistonTensorPatch).GetMethod("MyPatch",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool MyPatch(MyPistonBase __instance)
        {
            if (__instance != null && MainModule.Config.PluginEnabled && MainModule.Config.ShareInertiaTensor.Enabled)
                try
                {
                    if (__instance is MyPistonBase piston)
                    {
                        var needCheck =
                            MainModule.Config.ShareInertiaTensor.Pistons == EnabledStateEnum.AlwaysDisabled ||
                            MainModule.Config.ShareInertiaTensor.Pistons == EnabledStateEnum.AlwaysEnabled;

                        // Check cache expired?
                        if (needCheck && SKOTweaksPlugin.ExpireCache.Expired(piston.EntityId))
                        {
                            CheckPatchTensor(piston, MainModule.Config.ShareInertiaTensor.Pistons);

                            // Set cache.
                            SKOTweaksPlugin.ExpireCache.SetData(piston, 2);
                        }
                    }
                }
                catch
                {
                }

            return true;
        }

        public static void CheckPatchTensor(IMyTerminalBlock entity, EnabledStateEnum config)
        {
            if (entity != null && !entity.Closed)
                try
                {
                    var hasProperty = entity.GetProperty("ShareInertiaTensor") != null;
                    if (hasProperty)
                    {
                        var value = entity.GetValueBool("ShareInertiaTensor");
                        var needUpdate = false;

                        if (config == EnabledStateEnum.AlwaysDisabled && value)
                            needUpdate = true;
                        else if (config == EnabledStateEnum.AlwaysEnabled && value == false) needUpdate = true;

                        if (needUpdate) ShareInertiaTensorHelper.SetTensorValue(entity, value);
                    }
                }
                catch
                {
                }
        }
    }
}