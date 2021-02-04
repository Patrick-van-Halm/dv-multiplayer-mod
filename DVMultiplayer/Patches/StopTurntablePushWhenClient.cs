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
    [HarmonyPatch(typeof(TurntableController), "GetPushingInput")]
    class StopTurntablePushWhenClient
    {
        static bool Prefix(Transform handle)
        {
            if (NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
