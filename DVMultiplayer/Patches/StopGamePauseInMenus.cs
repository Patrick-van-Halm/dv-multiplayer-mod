using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(CanvasSpawner), "Open", new[] { typeof(MenuScreen), typeof(bool) })]
    class StopGamePauseInMenus
    {
        static void Prefix(MenuScreen screenToOpen, ref bool pauseGame)
        {
            if(NetworkManager.IsClient())
            {
                pauseGame = false;
                TutorialController.movementAllowed = false;
            }
        }
    }
}
