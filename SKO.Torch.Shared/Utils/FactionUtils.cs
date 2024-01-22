using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace SKO.Torch.Shared.Utils
{
    public class FactionUtils
    {
        public static IMyFaction GetFactionById(long factionId)
        {
            return MySession.Static.Factions.TryGetFactionById(factionId);
        }

        public static bool IsFriendlyWith(long sourcePlayerId, long targetPlayerId)
        {
            var friendlyReputation = 500;
            var factionA = GetFactionOfPlayer(sourcePlayerId);
            var factionB = GetFactionOfPlayer(targetPlayerId);

            if (factionA != null && factionB != null)
                return !MyVisualScriptLogicProvider.AreFactionsEnemies(factionA.Tag, factionB.Tag);

            if (factionA == null && factionB != null)
            {
                var rep = MyVisualScriptLogicProvider.GetRelationBetweenPlayerAndFaction(sourcePlayerId, factionB.Tag);
                if (rep >= friendlyReputation)
                    return true;
            }
            else if (factionB == null && factionA != null)
            {
                var rep = MyVisualScriptLogicProvider.GetRelationBetweenPlayerAndFaction(targetPlayerId, factionA.Tag);
                if (rep >= friendlyReputation)
                    return true;
            }

            return false;
        }

        public static IMyFaction GetFactionByTag(string factionTag)
        {
            return MySession.Static.Factions.TryGetFactionByTag(factionTag);
        }

        public static IMyFaction GetFactionOfPlayer(long playerId)
        {
            return MySession.Static.Factions.TryGetPlayerFaction(playerId);
        }

        public static string GetFactionTagOfPlayer(long playerId)
        {
            var faction = GetFactionOfPlayer(playerId);
            if (faction == null)
                return "";
            return faction.Tag;
        }

        public static bool HavePlayersSameFaction(long playerA_Id, long playerB_Id)
        {
            var factionA = GetFactionOfPlayer(playerA_Id);
            var factionB = GetFactionOfPlayer(playerB_Id);
            return factionA == factionB;
        }

        public static bool ExistsFaction(long factionId)
        {
            return GetFactionById(factionId) != null;
        }

        public static bool ExistsFaction(string factionTag)
        {
            return GetFactionByTag(factionTag) != null;
        }

        public static HashSet<IMyPlayer> GetOnlineFactionMembers(IMyFaction faction)
        {
            var players = new HashSet<IMyPlayer>();
            if (faction != null)
            {
                var onlinePlayers = MyVisualScriptLogicProvider.GetOnlinePlayers();
                foreach (var member in faction.Members)
                    if (onlinePlayers.Contains(member.Value.PlayerId))
                        players.Add(PlayerUtils.GetPlayer(member.Value.PlayerId));
            }

            return players;
        }

        public static bool AnyFactionMemberOnline(IMyFaction faction)
        {
            if (faction != null)
            {
                // Check if players are online of this faction.
                var factionMemberOnline = false;
                var onlinePlayers = MyVisualScriptLogicProvider.GetOnlinePlayers();
                foreach (var member in faction.Members)
                {
                    if (onlinePlayers.Contains(member.Value.PlayerId))
                    {
                        factionMemberOnline = true;
                        break;
                    }

                    ;
                }

                return factionMemberOnline;
            }

            return false;
        }
    }
}