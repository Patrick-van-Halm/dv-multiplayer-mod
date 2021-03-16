using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace DVMultiplayer.Patches.PassengerJobs
{
    public static class Initializer
    {
        public static void Initialize(UnityModManager.ModEntry passengerJobsModEntry, Harmony harmony)
        {
            Main.Log("Patching passenger jobs...");
            try
            {
                // Passenger Jobs spawning
                Type passengerJobsGen = passengerJobsModEntry.Assembly.GetType("PassengerJobsMod.PassengerJobGenerator", true);
                var StartGenAsync = AccessTools.Method(passengerJobsGen, "StartGenerationAsync");
                var StartGenAsyncPrefix = AccessTools.Method(typeof(PassengerJobsAsyncGeneratorPatch), "Prefix");
                var StartGenAsyncPostfix = AccessTools.Method(typeof(PassengerJobsAsyncGeneratorPatch), "Postfix");
                harmony.Patch(StartGenAsync, prefix: new HarmonyMethod(StartGenAsyncPrefix), postfix: new HarmonyMethod(StartGenAsyncPostfix));
            }
            catch(Exception ex)
            {
                Main.Log($"Patching passenger jobs failed. Error: {ex.Message}");
            }
        }
    }

    class PassengerJobsAsyncGeneratorPatch
    {
        static bool Prefix()
        {
            if()
        }

        static void Postfix()
        {

        }
    }
}
