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

namespace DVMultiplayer
{
    public class Main
    {
        public static ModEntry mod;
        public static event Action OnGameFixedGUI; 
        public static event Action OnGameUpdate;
        private static bool isInitialized = false;
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

        static void OnUpdate(ModEntry entry, float time)
        {
            if(!isInitialized && enabled && PlayerManager.PlayerTransform && !LoadingScreenManager.IsLoading)
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
            if (!VRManager.IsVREnabled() && enabled && isInitialized)
            {
#if DEBUG
                DebugUI.OnGUI();
#endif

                if(TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress)
                {
                    GUI.Box(new Rect(Screen.width - 100, 0, 100, 50), "DVMultiplayer note");
                    GUI.Label(new Rect(Screen.width - 90, 25, 80, 20), "You need to finish the tutorial before you can use DVMultiplayer");
                }
                //else if (PlayerManager.)
                //{
                //    GUI.Box(new Rect(Screen.width - 100, 0, 100, 50), "DVMultiplayer note");
                //    GUI.Label(new Rect(Screen.width - 90, 25, 80, 20), "You need to finish the tutorial before you can use DVMultiplayer");
                //}
                else
                    OnGameFixedGUI?.Invoke();
            }
        }

        static void Initialize()
        {
            isInitialized = true;
            DebugLog("Initializing...");
            NetworkManager.Initialize();
        }
#if DEBUG
        public static void DebugLog(string msg)
        {
            mod.Logger.Log($"[DEBUG] {msg}");
        }
#endif
    }
}
