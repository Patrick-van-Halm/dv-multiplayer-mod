using HarmonyLib;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using System.Reflection;
using UnityEngine;
using DVMultiplayer.Utils;
using DarkRift.Client.Unity;
using DVMultiplayer.Networking;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

namespace DVMultiplayer
{
    public class Main
    {
        public static ModEntry mod;
        public static event Action OnGameFixedGUI;
        public static event Action OnGameUpdate;
        public static bool isInitialized = false;
        private static bool enabled;

        private static Harmony harmony;

        static bool Load(ModEntry entry)
        {
            isInitialized = false;
            harmony = new Harmony(entry.Info.Id);
            mod = entry;
            mod.OnFixedGUI = OnFixedGUI;
            mod.OnToggle = OnToggle;
            mod.OnUpdate = OnUpdate;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            return true;
        }

        static bool OnToggle(ModEntry entry, bool enabled)
        {
            Main.enabled = enabled;
            return true;
        }

        public static string[] GetEnabledMods()
        {
            return modEntries.Where(m => m.Active && m.Loaded).Select(m => m.Info.Id).Where(m => m != "UnencryptedSaveGameMod").ToArray();
        }

        static void OnUpdate(ModEntry entry, float time)
        {
            if(!isInitialized && enabled && PlayerManager.PlayerTransform && !LoadingScreenManager.IsLoading && SingletonBehaviour<CanvasSpawner>.Instance)
            {
                Initialize();
            }

            if (enabled && isInitialized)
            {
#if DEBUG
                DebugUI.Update();
#endif
                OnGameUpdate?.Invoke();
            }
        }

        static void OnFixedGUI(ModEntry entry)
        {
            if (enabled && isInitialized)
            {
#if DEBUG
                DebugUI.OnGUI();
#endif
                OnGameFixedGUI?.Invoke();
            }
        }

        static void Initialize()
        {
            DebugLog("Initializing...");
            CustomUI.Initialize();
            FavoritesManager.CreateFavoritesFileIfNotExists();
            NetworkManager.Initialize();
            isInitialized = true;
        }
#if DEBUG
        public static void DebugLog(string msg)
        {
            mod.Logger.NativeLog($"[DEBUG] {msg}");
        }
#endif
    }
}
