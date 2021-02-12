using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(UnusedTrainCarDeleter), "AreDeleteConditionsFulfilled")]
    internal class StopRemovingTrainsWhenClient
    {
        private static void Postfix(TrainCar trainCar, ref bool __result)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Instance.SaveCarsLoaded)
                __result = false;
        }
    }
}
