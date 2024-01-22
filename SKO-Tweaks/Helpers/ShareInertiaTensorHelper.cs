using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SKO.Torch.Plugins.Tweaks.Config;
using SKO.Torch.Plugins.Tweaks.Modules;

namespace SKO.Torch.Plugins.Tweaks.Helpers
{
    public static class ShareInertiaTensorHelper
    {
        public static void SetTensorValue(IMyTerminalBlock entity, bool? currentValue = null)
        {
            if (MainModule.Config.PluginEnabled && entity != null)
            {
                if (entity is IMyTerminalBlock == false) return;

                var isValid = false;
                var configState = EnabledStateEnum.PlayerDefined;

                if (entity is IMyPistonBase)
                {
                    configState = MainModule.Config.ShareInertiaTensor.Pistons;
                    isValid = true;
                }
                else if (entity is IMyMotorStator && entity.BlockDefinition.SubtypeId.Contains("Stator"))
                {
                    configState = MainModule.Config.ShareInertiaTensor.Rotors;
                    isValid = true;
                }
                else if (entity is IMyMotorStator && entity.BlockDefinition.SubtypeId.Contains("Hinge"))
                {
                    configState = MainModule.Config.ShareInertiaTensor.Hinges;
                    isValid = true;
                }

                if (isValid)
                {
                    var updated = false;
                    switch (configState)
                    {
                        case EnabledStateEnum.AlwaysEnabled:
                        case EnabledStateEnum.EnableAfterRestart:
                            if (currentValue.HasValue)
                            {
                                if (currentValue.Value == false)
                                {
                                    entity.SetValueBool("ShareInertiaTensor", true);
                                    updated = true;
                                }
                            }
                            else
                            {
                                entity.SetValueBool("ShareInertiaTensor", true);
                            }

                            break;

                        case EnabledStateEnum.AlwaysDisabled:
                        case EnabledStateEnum.DisableAfterRestart:
                            if (currentValue.HasValue)
                            {
                                if (currentValue.Value)
                                {
                                    entity.SetValueBool("ShareInertiaTensor", false);
                                    updated = true;
                                }
                            }
                            else
                            {
                                entity.SetValueBool("ShareInertiaTensor", false);
                            }

                            break;
                    }

                    if (updated)
                        if (MainModule.Config.ShareInertiaTensor.Log)
                            SKOTweaksPlugin.Log.Warn(
                                $"Share inertia tensor for block '{entity.DisplayNameText}' on grid '{entity.CubeGrid.CustomName}' modified: {configState}");
                }
            }
        }
    }
}