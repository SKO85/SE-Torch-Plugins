using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace SKO.Torch.Shared.Utils
{
    public static class CreditsUtils
    {
        /// <summary>
        ///     Checks if the player or its faction has sufficient credits.
        /// </summary>
        /// <param name="playerId">The entityId of the player.</param>
        /// <param name="requiredCredits">The minimal required amount of credits on the balance.</param>
        /// <returns></returns>
        public static bool HasSufficientCredits(long playerId, long requiredCredits)
        {
            // Session should be active to continue.
            if (MySession.Static == null)
                return false;

            // Get balance of player.
            var balance = GetPlayerBalance(playerId);
            if (balance >= requiredCredits)
                return true;

            // Get balance of players faction (if any).
            var faction = FactionUtils.GetFactionOfPlayer(playerId);
            if (faction != null)
                faction.TryGetBalanceInfo(out balance);

            // Check faction balance.
            if (balance >= requiredCredits)
                return true;

            // Insufficient balance.
            return false;
        }

        public static bool AddCredits(long playerId, long creditsToAdd, bool addToFaction = true)
        {
            // Session should be active to continue.
            if (MySession.Static == null)
                return false;

            // Get player.
            var player = PlayerUtils.GetPlayer(playerId);
            if (player == null)
                return false;

            // Try to add to the faction first the player has one.
            if (addToFaction)
            {
                var faction = FactionUtils.GetFactionById(playerId);
                if (faction != null)
                {
                    faction.RequestChangeBalance(creditsToAdd);
                    return true;
                }
            }

            player.RequestChangeBalance(creditsToAdd);
            return true;
        }

        public static bool RemoveCredits(long playerId, long creditsToRemove)
        {
            // Session should be active to continue.
            if (MySession.Static == null)
                return false;

            var player = PlayerUtils.GetPlayer(playerId);
            if (player == null)
                return false;

            var balance = GetPlayerBalance(player);
            if (balance >= creditsToRemove)
            {
                player.RequestChangeBalance(-creditsToRemove);
                return true;
            }

            var faction = FactionUtils.GetFactionOfPlayer(playerId);
            if (faction == null)
                return false;

            // Try remove from faction if player has one.
            balance = GetFactionBalance(faction);
            if (balance >= creditsToRemove)
            {
                faction.RequestChangeBalance(-creditsToRemove);
                return true;
            }

            return false;
        }

        public static long GetPlayerBalance(IMyPlayer player)
        {
            long balance = 0;
            if (player != null) player.TryGetBalanceInfo(out balance);
            return balance;
        }

        public static long GetPlayerBalance(long playerId)
        {
            return GetPlayerBalance(PlayerUtils.GetPlayer(playerId));
        }

        public static long GetFactionBalance(IMyFaction faction)
        {
            long balance = 0;
            if (faction != null) faction.TryGetBalanceInfo(out balance);
            return balance;
        }

        public static long GetFactionBalance(long factionId)
        {
            return GetFactionBalance(FactionUtils.GetFactionById(factionId));
        }

        public static long GetFactionBalance(string factionTag)
        {
            long balance = 0;
            var faction = FactionUtils.GetFactionByTag(factionTag);
            if (faction != null) faction.TryGetBalanceInfo(out balance);
            return balance;
        }
    }
}