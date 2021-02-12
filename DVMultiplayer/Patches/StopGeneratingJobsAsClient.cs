using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(StationProceduralJobsController), "TryToGenerateJobs")]
    internal class StopGeneratingJobsAsClient
    {
        private static bool Prefix()
        {
            if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                return false;

            return true;
        }
    }
}
