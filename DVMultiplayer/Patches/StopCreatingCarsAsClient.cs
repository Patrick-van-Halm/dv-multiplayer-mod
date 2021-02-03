using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(GarageCarSpawner), "Update")]
    class StopCreatingCarsAsClient
    {
        static bool Prefix()
        {
            if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                return false;

            return true;
        }
    }
}
