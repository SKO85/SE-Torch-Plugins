using SKO.Torch.Plugins.Tweaks.Modules;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace SKO.Torch.Plugins.Tweaks.Commands
{
    [Category("sko-tweaks")]
    public class ConfigCommands : CommandModule
    {
        [Command("config", ".")]
        [Permission(MyPromoteLevel.Admin)]
        public void Config(string args = null)
        {
            if (string.IsNullOrEmpty(args))
            {
                var msg = new DialogMessage("Configuration", Constants.PluginName, MainModule.Config.GetConfigJSON());
                ModCommunication.SendMessageTo(msg, Context.Player.SteamUserId);
            }
            else
            {
                switch (args.ToLowerInvariant().Trim())
                {
                    case "load":
                    case "reload":
                        SKOTweaksPlugin.Module.LoadConfig();
                        Context.Respond("Config (re)loaded.");
                        break;

                    case "save":
                        SKOTweaksPlugin.Module.SaveConfig();
                        Context.Respond("Config saved.");
                        break;

                    default:
                        Context.Respond("Unknown arguments.");
                        break;
                }
            }
        }
    }
}