using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(GarageCarSpawner), "Update")]
    internal class StopCreatingCarsAsClient
    {
        private static bool Prefix()
        {
            if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                return false;

            return true;
        }
    }
}
