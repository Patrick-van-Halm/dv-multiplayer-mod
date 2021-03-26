using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches.Train.Steamer
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateTick")]
    internal class SteamLocoSimulation_SimulateTick_Patch
    {
        private static void Prefix(SteamLocoSimulation __instance)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainSync trainSync = __instance.GetComponent<NetworkTrainSync>();

                if (__instance.fireOn.value == 1 && __instance.fireOn.nextValue == 0 && __instance.coalbox.value == 0)
                    trainSync.OnSteamerFireOnChanged(0);
            }
        }
    }
}
