using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(GarageCarSpawner), "Update")]
    internal class StopCreatingCarsAsClient
    {
		private static bool shouldSpawn = false;
		private static bool Prefix(GarageCarSpawner __instance, bool ___spawnAllowed, ref bool ___playerEnteredLocoSpawnRange, Action<TrainCar> ___OnGarageCarDeleted)
        {
            if (NetworkManager.IsClient() && !NetworkManager.IsHost())
            {
                return false;
            }
            else if (NetworkManager.IsHost())
            {
				if(!___spawnAllowed || !SaveLoadController.carsAndJobsLoadingFinished)
				{
					return false;
				}
				bool isHostInSpawnArea = (PlayerManager.PlayerTransform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack;
				if (!___playerEnteredLocoSpawnRange && isHostInSpawnArea)
				{
					___playerEnteredLocoSpawnRange = true;
					if (!SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().All(p => (p.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack))
						shouldSpawn = true;
				}
				else if (!___playerEnteredLocoSpawnRange && !isHostInSpawnArea)
				{
					___playerEnteredLocoSpawnRange = true;
					if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().Any(p => (p.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack))
						shouldSpawn = true;
				}
				else if (___playerEnteredLocoSpawnRange && !isHostInSpawnArea)
				{
					if (!SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().Any(p => (p.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack))
						___playerEnteredLocoSpawnRange = false;
				}

                if (shouldSpawn)
                {
					if (__instance.garageSpawnedCar == null)
					{
						__instance.garageSpawnedCar = CarSpawner.SpawnCarOnClosestTrack(__instance.locoSpawnPoint.transform.position, __instance.locoType, __instance.flipSpawnLoco, true);
						if (__instance.garageSpawnedCar != null)
						{
							SingletonBehaviour<UnusedTrainCarDeleter>.Instance.MarkForDelete(__instance.garageSpawnedCar);
							__instance.garageSpawnedCar.OnDestroyCar += ___OnGarageCarDeleted;
							return false;
						}
						Main.Log("[ERROR] Couldn't spawn garage car!");
						return false;
					}
				}
			}
            return true;
        }
    }
}
