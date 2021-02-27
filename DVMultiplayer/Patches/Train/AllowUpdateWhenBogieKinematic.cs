using DV;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(Bogie), "FixedUpdate")]
    internal class AllowUpdateWhenBogieKinematic
	{
        private static void Postfix(Bogie __instance)
        {
            if (NetworkManager.IsClient() && __instance.rb.isKinematic)
            {
                MethodInfo dynMethod = __instance.GetType().GetMethod("UpdatePointSetTraveller", BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(__instance, null);
            }
		}
    }
}
