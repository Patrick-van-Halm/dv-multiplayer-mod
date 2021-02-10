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
    [HarmonyPatch(typeof(TrainCar), "ReturnCarToPool")]
    class StopTrainsToPoolWhenClient
    {
        static bool Prefix()
        {
            if (NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
