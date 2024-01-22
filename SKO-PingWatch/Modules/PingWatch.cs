using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using SKO.Pack.Modules;
using SKO.Torch.Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SKO.PingWatch.Modules
{
    public class PingWatch
    {
        public static PingWatchConfig Config = new PingWatchConfig();
        public static int CheckAfterNumPlayers = 1;

        public static ConcurrentDictionary<ulong, PingWatchPlayerData> Data =
            new ConcurrentDictionary<ulong, PingWatchPlayerData>();

        public bool IsRunning;

        public void Clean()
        {
            IsRunning = false;
            Data.Clear();
        }

        public void Initialize()
        {
            if (Config.ShowLogWarnings)
            {
                SKOPingWatchPlugin.Log.Warn("Configuration:");
                SKOPingWatchPlugin.Log.Warn($"> Max Ping: \t\t{Config.MaxPing}");
                SKOPingWatchPlugin.Log.Warn($"> Max Probes: \t\t{Config.MaxProbes}");
                SKOPingWatchPlugin.Log.Warn($"> Max Warnings: \t{Config.MaxWarnings}");
                SKOPingWatchPlugin.Log.Warn($"> Reconnections: \t{Config.MaxReconnectionsAllowed}");
                SKOPingWatchPlugin.Log.Warn($"> Interval: \t\t{Config.CheckIntervalMin} min.");
            }

            // Disconnect event to clear data.
            MyVisualScriptLogicProvider.PlayerDisconnected += OnPlayerDisconnected;

            // Start the watch process.
            IsRunning = true;
            Task.Run(() =>
            {
                while (IsRunning) CheckPing();
            });
        }

        private void OnPlayerDisconnected(long playerId)
        {
            if (!Config.Enabled)
                return;

            var player = PlayerUtils.GetPlayer(playerId);

            if (player != null)
            {
                if (Config.ShowLogWarnings)
                    SKOPingWatchPlugin.Log.Warn(
                        $"[Disconnected] {player.DisplayName} ({playerId}) ({player.SteamUserId})");

                if (Data.ContainsKey(player.SteamUserId))
                {
                    // Clear records.
                    Data[player.SteamUserId].Pings.Clear();
                    Data[player.SteamUserId].Warnings.Clear();
                }
            }
        }

        public void CheckPing()
        {
            if (!Config.Enabled)
            {
                Thread.Sleep(Config.CheckIntervalMin * 60 * 1000);
                return;
            }

            var ping = NetworkUtils.GetPings();
            if (ping.Count > CheckAfterNumPlayers)
                foreach (var playerId in ping.Keys)
                {
                    if (!Data.ContainsKey(playerId))
                        Data[playerId] = new PingWatchPlayerData
                        {
                            SteamId = playerId
                        };

                    Data[playerId].Pings.Add(ping[playerId]);
                }
            else
                Data.Clear();

            if (Data.Count > CheckAfterNumPlayers)
                foreach (var steamId in Data.Keys)
                {
                    var probes = Data[steamId].Pings;
                    if (probes.Count >= Config.MaxProbes)
                    {
                        var avgPing = Math.Round(probes.Average(c => c));
                        Data[steamId].LastAveragePing = avgPing;

                        if (avgPing > Config.MaxPing)
                            Task.Run(() => { WarnOrDisconnectPlayer(steamId, avgPing); });
                        probes.RemoveRange(0, probes.Count - Config.MaxProbes + 1);
                    }
                }

            Thread.Sleep(Config.CheckIntervalMin * 60 * 1000);
        }

        public void WarnOrDisconnectPlayer(ulong steamId, double ping)
        {
            var warningCount = Data[steamId].Warnings.Count;
            var player = PlayerUtils.GetIdentityByNameOrId(steamId.ToString());
            if (warningCount < Config.MaxWarnings)
            {
                if (Config.ShowLogWarnings)
                    SKOPingWatchPlugin.Log.Warn($"[PING Warning] {player.DisplayName} ({steamId}): Average {ping}ms");

                ChatUtils.SendTo(player.IdentityId, $"High Ping Warning ({warningCount + 1} / {Config.MaxWarnings}):");
                ChatUtils.SendTo(player.IdentityId, $"Your average ping: {ping} ms.");
                ChatUtils.SendTo(player.IdentityId, $"Max allowed ping: {Config.MaxPing} ms.");

                if (Config.MaxReconnectionsAllowed > 0)
                    ChatUtils.SendTo(player.IdentityId,
                        $"Reconnections: {Data[steamId].Connections}/{Config.MaxReconnectionsAllowed}");

                ChatUtils.SendTo(player.IdentityId, "Please fix your ping or you will");
                ChatUtils.SendTo(player.IdentityId, "get auto-kicked.");

                if (warningCount + 1 == Config.MaxWarnings)
                {
                    NotificationUtils.SendTo(player.IdentityId, "Final High Ping Warning!", 10);

                    if (Config.KickPlayer && Config.MaxReconnectionsAllowed > 0)
                    {
                        var reconnectionsLeft = Config.MaxReconnectionsAllowed - Data[steamId].Connections;
                        NotificationUtils.SendTo(player.IdentityId,
                            $"You have {reconnectionsLeft} reconnection(s) left.", 10);
                        if (reconnectionsLeft == 0)
                            NotificationUtils.SendTo(player.IdentityId,
                                "You are going to be kicked until next server restart...", 10);
                    }
                }
                else
                {
                    NotificationUtils.SendTo(player.IdentityId, "High Ping Warning!", 5);
                }

                SoundUtils.SendTo(player.IdentityId);
                Data[steamId].Warnings.Add(DateTime.Now);
            }
            else
            {
                var isKick = Config.KickPlayer && (Config.MaxReconnectionsAllowed < 1 ||
                                                   (Config.MaxReconnectionsAllowed > 0 && Data[steamId].Connections >=
                                                       Config.MaxReconnectionsAllowed));

                if (Config.ShowLogWarnings)
                    SKOPingWatchPlugin.Log.Warn(
                        $"[PING {(isKick ? "Kick" : "Disconnect")}] {player.DisplayName} ({steamId}): Average {ping}ms");

                ChatUtils.SendTo(player.IdentityId, $"{player.DisplayName}, your ping is still high.");
                ChatUtils.SendTo(player.IdentityId, $"We need to {(isKick ? "kick" : "disconnect")} you.");
                ChatUtils.SendTo(player.IdentityId, $"Auto-{(isKick ? "kick" : "disconnect")} in 5 sec...");
                SoundUtils.SendTo(player.IdentityId);

                try
                {
                    Thread.Sleep(5000);
                    if (isKick)
                    {
                        MyMultiplayer.Static.KickClient(steamId);
                        Data.Remove(steamId);
                    }
                    else
                    {
                        MyMultiplayer.Static.KickClient(steamId, true, false);
                        Data[steamId].Warnings.Clear();
                        Data[steamId].Pings.Clear();
                        Data[steamId].Connections++;
                    }

                    ChatUtils.SendTo(0, "High Ping Watch:");
                    ChatUtils.SendTo(0,
                        $"{(isKick ? "Kicking" : "Disconnecting")} {player.DisplayName} (Ping {ping}ms.)");
                }
                catch (Exception)
                {
                }
            }
        }
    }
}