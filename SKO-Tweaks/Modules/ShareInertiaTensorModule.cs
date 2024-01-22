using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SKO.Torch.Plugins.Tweaks.Config;
using SKO.Torch.Plugins.Tweaks.Helpers;
using SKO.Torch.Shared.Managers.Grid;
using SKO.Torch.Shared.Plugin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SKO.Torch.Plugins.Tweaks.Modules
{
    public class ShareInertiaTensorModule : PluginExtensionModule<SKOTweaksPlugin>
    {
        public ShareInertiaTensorModule(SKOTweaksPlugin pluginInstance) : base(pluginInstance)
        {
        }

        public override void Start()
        {
            Task.Run(() =>
            {
                try
                {
                    // Pistons.
                    if (MainModule.EntityManager != null)
                        if (MainModule.Config.PluginEnabled && MainModule.Config.ShareInertiaTensor.Enabled)
                        {
                            if (MainModule.Config.ShareInertiaTensor.Pistons == EnabledStateEnum.PlayerDefined &&
                                MainModule.Config.ShareInertiaTensor.Rotors == EnabledStateEnum.PlayerDefined &&
                                MainModule.Config.ShareInertiaTensor.Hinges == EnabledStateEnum.PlayerDefined) return;

                            var pistons = new List<IMyPistonBase>();
                            var rotors = new List<IMyMotorStator>();
                            var hinges = new List<IMyMotorStator>();

                            foreach (var item in MainModule.EntityManager.GetOf<MyCubeGrid>())
                            {
                                GridBlocksCacheManager.Track(item);

                                if (MainModule.Config.ShareInertiaTensor.Pistons != EnabledStateEnum.PlayerDefined)
                                    pistons.AddRange(GridBlocksCacheManager.GetBlocks<IMyPistonBase>(item.EntityId)
                                        .Values);

                                if (MainModule.Config.ShareInertiaTensor.Rotors != EnabledStateEnum.PlayerDefined)
                                    rotors.AddRange(GridBlocksCacheManager
                                        .GetBlocks<IMyMotorStator>(item.EntityId, "Stator").Values);

                                if (MainModule.Config.ShareInertiaTensor.Hinges != EnabledStateEnum.PlayerDefined)
                                    hinges.AddRange(GridBlocksCacheManager
                                        .GetBlocks<IMyMotorStator>(item.EntityId, "Hinge").Values);
                            }

                            // Set values.
                            pistons.ForEach(c => ShareInertiaTensorHelper.SetTensorValue(c));
                            rotors.ForEach(c => ShareInertiaTensorHelper.SetTensorValue(c));
                            hinges.ForEach(c => ShareInertiaTensorHelper.SetTensorValue(c));
                        }
                }
                catch
                {
                    // ignored
                }
            });
        }

        protected override void InitializeModule()
        {
        }
    }
}