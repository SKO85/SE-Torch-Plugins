using System.Collections.Concurrent;

namespace SKO.Torch.Shared.Managers.Cooldown
{
    public class CooldownManager
    {
        private readonly ConcurrentDictionary<ICooldownKey, CurrentCooldown> cooldownMap =
            new ConcurrentDictionary<ICooldownKey, CurrentCooldown>();

        public bool CheckCooldown(ICooldownKey key, string command, out long remainingSeconds)
        {
            remainingSeconds = 0L;
            if (cooldownMap.TryGetValue(key, out var currentCooldown))
            {
                remainingSeconds = currentCooldown.GetRemainingSeconds(command);
                if (remainingSeconds > 0L) return false;
            }

            return true;
        }

        public void StartCooldown(ICooldownKey key, string command, long cooldown)
        {
            var currentCooldown = new CurrentCooldown(cooldown);
            cooldownMap[key] = currentCooldown;
            currentCooldown.StartCooldown(command);
        }

        public void StopCooldown(ICooldownKey key)
        {
            cooldownMap.TryRemove(key, out _);
        }
    }
}