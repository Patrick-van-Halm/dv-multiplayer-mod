using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(UnusedTrainCarDeleter), "AreDeleteConditionsFulfilled")]
    class StopRemovingTrainsWhenClient
    {
        static void Postfix(TrainCar trainCar, ref bool __result)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Instance.SaveTrainCarsLoaded)
                __result = false;
        }
    }
}
