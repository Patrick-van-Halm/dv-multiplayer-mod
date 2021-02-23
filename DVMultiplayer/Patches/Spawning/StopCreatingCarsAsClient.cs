using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(GarageCarSpawner), "Update")]
    internal class StopCreatingCarsAsClient
    {
        private static bool Prefix(GarageCarSpawner __instance)
        {
            if (NetworkManager.IsClient())
            {
                if((PlayerManager.PlayerTransform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack)
                {
                    bool allowedToSpawn = true;
                    foreach (GameObject player in SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers())
                    {
                        if ((player.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack)
                        {
                            allowedToSpawn = false;
                            break;
                        }
                    }
                    return allowedToSpawn;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
