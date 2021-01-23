using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(CanvasSpawner), "Close")]
    class EnableMovementOutsidePauseMenus
    {
        static void Postfix()
        {
            if (!TutorialController.movementAllowed && NetworkManager.IsClient())
                TutorialController.movementAllowed = true;
        }
    }
}
