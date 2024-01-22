using Sandbox.Game;
using VRage.Audio;

namespace SKO.Torch.Shared.Utils
{
    public static class SoundUtils
    {
        public static void SendTo(long playerId, MyGuiSounds sound = MyGuiSounds.HudGPSNotification3)
        {
            MyVisualScriptLogicProvider.PlayHudSound(sound, playerId);
        }

        public static void SendToAll(MyGuiSounds sound = MyGuiSounds.HudGPSNotification3)
        {
            MyVisualScriptLogicProvider.PlayHudSound(sound);
        }
    }
}