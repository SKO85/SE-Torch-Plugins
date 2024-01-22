using Sandbox.Game;

namespace SKO.Torch.Shared.Utils
{
    public static class NotificationUtils
    {
        /// <summary>
        ///     Send a notification message to the specified playerId.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="timeS">Time the message is shown in seconds</param>
        /// <param name="playerId">The id of the player</param>
        /// <param name="color">Color used for the message</param>
        public static void SendTo(long playerId, string message, int timeS,
            string color = Constants.DEFAULT_NOTIFICATION_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.ShowNotification(message, timeS * 1000, color, playerId);
        }

        /// <summary>
        ///     Send a notification message to all players.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="timeS">Time the message is shown in seconds</param>
        /// <param name="color">Color used for the message</param>
        public static void SendToAll(string message, int timeS,
            string color = Constants.DEFAULT_NOTIFICATION_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.ShowNotification(message, timeS * 1000, color);
        }
    }
}