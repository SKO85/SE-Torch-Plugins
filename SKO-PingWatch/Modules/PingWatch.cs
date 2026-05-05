using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using SKO.Pack.Modules;
using SKO.Torch.Shared.Utils;

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
            MyVisualScriptLogicProvider.PlayerDisconnected -= OnPlayerDisconnected;
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
                while (IsRunning)
                {
                    try
                    {
                        CheckPing();
                    }
                    catch (Exception ex)
                    {
                        SKOPingWatchPlugin.Log.Error(ex, "CheckPing failed; will retry next interval.");
                        try { Thread.Sleep(Math.Max(1, Config.CheckIntervalMin) * 60 * 1000); } catch { }
                    }
                }
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

                if (Data.TryGetValue(player.SteamUserId, out var data))
                {
                    // Clear records (preserve Connections so reconnect cap still applies).
                    lock (data)
                    {
                        data.Pings.Clear();
                        data.Warnings.Clear();
                    }
                }
            }
        }

        public void CheckPing()
        {
            if (!Config.Enabled)
            {
                Thread.Sleep(Math.Max(1, Config.CheckIntervalMin) * 60 * 1000);
                return;
            }

            var ping = NetworkUtils.GetPings();
            if (ping.Count > CheckAfterNumPlayers)
            {
                foreach (var playerId in ping.Keys)
                {
                    var data = Data.GetOrAdd(playerId, id => new PingWatchPlayerData { SteamId = id });
                    lock (data) { data.Pings.Add(ping[playerId]); }
                }
            }
            else
            {
                // Don't wipe Data — we'd lose Connections counters that enforce MaxReconnectionsAllowed.
                // Just clear the per-player ping/warning samples so they age out cleanly.
                foreach (var entry in Data.Values)
                {
                    lock (entry)
                    {
                        entry.Pings.Clear();
                        entry.Warnings.Clear();
                    }
                }
            }

            if (Data.Count > CheckAfterNumPlayers)
                foreach (var steamId in Data.Keys)
                {
                    if (!Data.TryGetValue(steamId, out var data)) continue;

                    double avgPing;
                    bool overLimit;
                    lock (data)
                    {
                        if (data.Pings.Count < Config.MaxProbes) continue;

                        avgPing = Math.Round(data.Pings.Average(c => c));
                        data.LastAveragePing = avgPing;
                        overLimit = avgPing > Config.MaxPing;

                        var trim = data.Pings.Count - Config.MaxProbes + 1;
                        if (trim > 0) data.Pings.RemoveRange(0, trim);
                    }

                    if (overLimit)
                    {
                        var capturedSteamId = steamId;
                        var capturedPing = avgPing;
                        Task.Run(() =>
                        {
                            try { WarnOrDisconnectPlayer(capturedSteamId, capturedPing); }
                            catch (Exception ex)
                            {
                                SKOPingWatchPlugin.Log.Error(ex,
                                    $"WarnOrDisconnectPlayer failed for steamId {capturedSteamId}.");
                            }
                        });
                    }
                }

            Thread.Sleep(Math.Max(1, Config.CheckIntervalMin) * 60 * 1000);
        }

        public void WarnOrDisconnectPlayer(ulong steamId, double ping)
        {
            if (!Data.TryGetValue(steamId, out var data)) return;

            var player = PlayerUtils.GetIdentityByNameOrId(steamId.ToString());
            if (player == null) return;

            int warningCount;
            lock (data) { warningCount = data.Warnings.Count; }

            if (warningCount < Config.MaxWarnings)
            {
                if (Config.ShowLogWarnings)
                    SKOPingWatchPlugin.Log.Warn($"[PING Warning] {player.DisplayName} ({steamId}): Average {ping}ms");

                ChatUtils.SendTo(player.IdentityId, $"High Ping Warning ({warningCount + 1} / {Config.MaxWarnings}):");
                ChatUtils.SendTo(player.IdentityId, $"Your average ping: {ping} ms.");
                ChatUtils.SendTo(player.IdentityId, $"Max allowed ping: {Config.MaxPing} ms.");

                if (Config.MaxReconnectionsAllowed > 0)
                    ChatUtils.SendTo(player.IdentityId,
                        $"Reconnections: {data.Connections}/{Config.MaxReconnectionsAllowed}");

                ChatUtils.SendTo(player.IdentityId, "Please fix your ping or you will");
                ChatUtils.SendTo(player.IdentityId, "get auto-kicked.");

                if (warningCount + 1 == Config.MaxWarnings)
                {
                    NotificationUtils.SendTo(player.IdentityId, "Final High Ping Warning!", 10);

                    if (Config.KickPlayer && Config.MaxReconnectionsAllowed > 0)
                    {
                        var reconnectionsLeft = Config.MaxReconnectionsAllowed - data.Connections;
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
                lock (data) { data.Warnings.Add(DateTime.Now); }
            }
            else
            {
                var isKick = Config.KickPlayer && (Config.MaxReconnectionsAllowed < 1 ||
                                                   (Config.MaxReconnectionsAllowed > 0 && data.Connections >=
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
                        Data.TryRemove(steamId, out _);
                    }
                    else
                    {
                        MyMultiplayer.Static.KickClient(steamId, true, false);
                        lock (data)
                        {
                            data.Warnings.Clear();
                            data.Pings.Clear();
                            data.Connections++;
                        }
                    }

                    ChatUtils.SendTo(0, "High Ping Watch:");
                    ChatUtils.SendTo(0,
                        $"{(isKick ? "Kicking" : "Disconnecting")} {player.DisplayName} (Ping {ping}ms.)");
                }
                catch (Exception ex)
                {
                    SKOPingWatchPlugin.Log.Error(ex,
                        $"Failed to {(isKick ? "kick" : "disconnect")} {player.DisplayName} ({steamId}).");
                }
            }
        }
    }
}
