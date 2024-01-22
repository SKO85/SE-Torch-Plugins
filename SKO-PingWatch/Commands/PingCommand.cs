using SKO.Pack.Modules;
using SKO.Torch.Shared.Utils;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace SKO.PingWatch.Commands
{
    public class PingCommand
    {
        [Category("ping")]
        public class PingCommands : CommandModule
        {
            [Command("show", "Shows the average ping of the specified player.")]
            [Permission(MyPromoteLevel.None)]
            public void Ping(string nameOrId = "")
            {
                IMyPlayer player = null;
                if (string.IsNullOrEmpty(nameOrId.Trim()))
                {
                    player = Context.Player;
                }
                else
                {
                    var identity = PlayerUtils.GetIdentityByNameOrId(nameOrId);
                    if (identity != null) player = PlayerUtils.GetPlayer(identity.IdentityId);
                }

                if (player != null)
                    SendPingReponse(player);
                else
                    Context.Respond("Cannot find player");
            }

            [Command("reload", "Reloads the config for PingWatch")]
            [Permission(MyPromoteLevel.Admin)]
            public void Reload()
            {
                Modules.PingWatch.Config =
                    ConfigUtils.Load<PingWatchConfig>(SKOPingWatchPlugin.Instance, "SKOPingWatch.cfg");
                Context.Respond("Configuration reloaded.");
            }

            [Command("config", "Show the config")]
            [Permission(MyPromoteLevel.Admin)]
            public void Config()
            {
                if (Modules.PingWatch.Config != null)
                {
                    Context.Respond("SKO-PingWatch Config:");
                    Context.Respond($"> Enabled: {Modules.PingWatch.Config.Enabled}");
                    Context.Respond($"> Max Ping: {Modules.PingWatch.Config.MaxPing}");
                    Context.Respond($"> Max Probes: {Modules.PingWatch.Config.MaxProbes}");
                    Context.Respond($"> Max Warnings: {Modules.PingWatch.Config.MaxWarnings}");
                    Context.Respond($"> Reconnections: {Modules.PingWatch.Config.MaxReconnectionsAllowed}");
                    Context.Respond($"> Interval: {Modules.PingWatch.Config.CheckIntervalMin} min.");
                }
            }

            private void SendPingReponse(IMyPlayer player)
            {
                if (Modules.PingWatch.Data != null && Modules.PingWatch.Data.ContainsKey(player.SteamUserId))
                {
                    var data = Modules.PingWatch.Data[player.SteamUserId];
                    if (data != null && data.LastAveragePing > 0)
                        Context.Respond($"Average ping is: {data.LastAveragePing} ms.");
                    else
                        Context.Respond("Insufficient data available. Try again later.");
                }
                else
                {
                    Context.Respond("No ping data available.");
                }
            }
        }
    }
}