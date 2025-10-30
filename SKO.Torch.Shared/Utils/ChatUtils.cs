using Sandbox.Game;
using System.Text;

namespace SKO.Torch.Shared.Utils
{
    public static class ChatUtils
    {
        /// <summary>
        ///     Sends a chat message to the specified player.
        /// </summary>
        /// <param name="playerId">Entity Id of the player</param>
        /// <param name="message">The message to send</param>
        /// <param name="from">Who is sending the message</param>
        /// <param name="color">Color used for the message</param>
        public static void SendTo(long playerId, string message, string from = "",
            string color = Constants.DEFAULT_CHAT_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.SendChatMessage(message, from, playerId, color);
        }

        /// <summary>
        ///     Sends a chat message to the specified player.
        /// </summary>
        /// <param name="playerId">Entity Id of the player</param>
        /// <param name="message">The message to send</param>
        /// <param name="from">Who is sending the message</param>
        /// <param name="color">Color used for the message</param>
        public static void SendTo(long playerId, StringBuilder message, string from = "",
            string color = Constants.DEFAULT_CHAT_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.SendChatMessage(message.ToString(), from, playerId, color);
        }

        /// <summary>
        ///     Sends a chat message to all players
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="from">Who is sending the message</param>
        /// <param name="color">Color used for the message</param>
        public static void SendToAll(string message, string from = "",
            string color = Constants.DEFAULT_CHAT_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.SendChatMessage(message, from, 0, color);
        }

        /// <summary>
        ///     Sends a chat message to all players
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="from">Who is sending the message</param>
        /// <param name="color">Color used for the message</param>
        public static void SendToAll(StringBuilder message, string from = "",
            string color = Constants.DEFAULT_CHAT_MESSAGE_COLOR)
        {
            MyVisualScriptLogicProvider.SendChatMessage(message.ToString(), from, 0, color);
        }
    }
}