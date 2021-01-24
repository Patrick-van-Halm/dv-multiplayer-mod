using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(SaveGameManager), "Load")]
    class DontLoadSaveFileIfDataIsSet
    {
        static bool Prefix(bool loadBackup = false)
        {
            if (SaveGameManager.data != null && NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
