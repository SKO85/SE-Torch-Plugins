using System;

namespace SKO.Torch.Shared.Managers.Cooldown
{
    public class CurrentCooldown
    {
        private readonly long _currentCooldown;
        private long _startTime;
        private string command;

        public CurrentCooldown(long cooldown)
        {
            _currentCooldown = cooldown;
        }

        public void StartCooldown(string command)
        {
            this.command = command;
            _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public long GetRemainingSeconds(string command)
        {
            if (this.command != command) return 0L;
            var elapsedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;
            if (elapsedTime >= _currentCooldown) return 0L;
            return (_currentCooldown - elapsedTime) / 1000L;
        }
    }
}