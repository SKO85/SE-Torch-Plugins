using Sandbox.Game.Entities.Cube;
using SKO.Torch.Plugins.Tweaks.Config;
using SKO.Torch.Plugins.Tweaks.Modules;
using System.Reflection;
using Torch.Managers.PatchManager;

namespace SKO.Torch.Plugins.Tweaks.Patches
{
    [PatchShim]
    public static class RotorTensorPatch
    {
        public static void Patch(PatchContext ctx)
        {
            var typeFromHandle = typeof(MyMotorStator);
            var aMethod = typeFromHandle.GetMethod("UpdateBeforeSimulation",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ctx.GetPattern(aMethod).Prefixes.Add(typeof(RotorTensorPatch).GetMethod("MyPatch",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool MyPatch(MyMotorStator __instance)
        {
            if (__instance != null && MainModule.Config.PluginEnabled && MainModule.Config.ShareInertiaTensor.Enabled)
                try
                {
                    if (__instance is MyMotorStator stator)
                    {
                        var isHinge = stator.BlockDefinition.Id.SubtypeName.Contains("Hinge");
                        var config = isHinge
                            ? MainModule.Config.ShareInertiaTensor.Hinges
                            : MainModule.Config.ShareInertiaTensor.Rotors;
                        var needCheck = config == EnabledStateEnum.AlwaysDisabled ||
                                        config == EnabledStateEnum.AlwaysEnabled;

                        // Check cache expired?
                        if (needCheck && SKOTweaksPlugin.ExpireCache.Expired(stator.EntityId))
                        {
                            PistonTensorPatch.CheckPatchTensor(stator, config);

                            // Set cache.
                            SKOTweaksPlugin.ExpireCache.SetData(stator, 2);
                        }
                    }
                }
                catch
                {
                }

            return true;
        }
    }
}