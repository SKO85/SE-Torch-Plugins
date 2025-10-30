using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using VRage.GameServices;
using VRage.Network;
using VRage.Serialization;

namespace SKO.Torch.Shared.Utils
{
    public static class NetworkUtils
    {
        /// <summary>
        ///     Get Players IP address
        /// </summary>
        /// <param name="steamId">SteamId of the player</param>
        /// <returns></returns>
        public static IPAddress GetIPAddressOfClient(ulong steamId)
        {
            try
            {
                var state = new MyP2PSessionState();
                MyGameService.Peer2Peer.GetSessionState(steamId, ref state);
                var ip = new IPAddress(BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray());
                return ip;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///     Get Ping of Online Players
        /// </summary>
        /// <returns></returns>
        public static Dictionary<ulong, short> GetPings()
        {
            var result = new Dictionary<ulong, short>();
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.ReplicationLayer != null)
            {
                SerializableDictionary<ulong, short> pings;
                ((MyReplicationServer)MyMultiplayer.Static.ReplicationLayer).GetClientPings(out pings);
                result = pings.Dictionary;
            }

            return result;
        }
    }
}