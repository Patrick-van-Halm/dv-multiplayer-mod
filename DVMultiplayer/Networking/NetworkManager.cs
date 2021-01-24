using DarkRift;
using DarkRift.Client.Unity;
using DarkRift.Server.Unity;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Utils;
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DVMultiplayer.Networking
{
    public static class NetworkManager
    {
        public static UnityClient client;
        public static XmlUnityServer server;
        private static NetworkingUI UI;
        private static GameObject networkManager;
        private static bool isHost;
        private static bool isClient;
        private static string host;
        private static int port;
        private static bool scriptsInitialized = false;

        public static void Initialize()
        {
            Main.DebugLog("Initializing NetworkManager");
            isHost = false;
            isClient = false;
            if (!UGameObject.Exists("NetworkManager"))
            {
                networkManager = Object.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity);
                networkManager.name = "NetworkManager";
                server = networkManager.AddComponent<XmlUnityServer>();
                client = networkManager.AddComponent<UnityClient>();

                Object.DontDestroyOnLoad(networkManager);

                server.configuration = new TextAsset(File.ReadAllText("./Mods/DVMultiplayer/Resources/config.xml"));
            }

            if (UI == null)
            {
                UI = new NetworkingUI();
            }

            Main.OnGameUpdate += UI.ListenToInputs;
            Main.OnGameFixedGUI += UI.Draw;
        }

        public static void Deinitialize()
        {
            Main.DebugLog("Deinitializing NetworkManager");

            if (UGameObject.Exists("NetworkManager"))
            {
                GameObject.Destroy(GameObject.Find("NetworkManager"));
            }
        }

        public static void Connect(string host, int port)
        {
            NetworkManager.host = host;
            NetworkManager.port = port;
            ClientConnect();
        }

        private static void ClientConnect()
        {
            if (isClient)
                return;

            isClient = true;
            client.ConnectInBackground(host, port, true, OnConnected);
        }

        public static void Disconnect()
        {
            if (!isClient)
                return;

            Main.DebugLog($"Disconnecting client");
            client.Close();
            if (scriptsInitialized)
            {
                DeInitializeUnityScripts();
                scriptsInitialized = false;
            }
            isClient = false;
            SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = false;
        }

        public static void StartServer()
        {
            if (isHost)
                return;

            Main.DebugLog("Start hosting server");
            try
            {
                server.Create();
                isHost = true;
                host = "127.0.0.1";
                port = 4296;
                ClientConnect();
            }
            catch (Exception ex)
            {
                Main.mod.Logger.Error(ex.Message);
            }
        }

        public static void StopServer()
        {
            if (!isHost)
                return;

            Main.DebugLog("Stop hosting server");
            try
            {
                Disconnect();
                server.Close();
                isHost = false;
            }
            catch (Exception ex)
            {
                Main.DebugLog($"[ERROR] {ex.Message}");
            }
        }

        private static void OnConnected(Exception ex)
        {
            if (ex != null && !string.IsNullOrEmpty(ex.Message))
            {
                isClient = false;
                Main.DebugLog($"[ERROR] {ex.Message}");
                ClientConnect();
            }
            else
            {
                if (!scriptsInitialized)
                {
                    InitializeUnityScripts();
                    scriptsInitialized = true;
                }
                SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = true;
                SingletonBehaviour<NetworkPlayerManager>.Instance.PlayerConnect();
            }
        }

        private static void InitializeUnityScripts()
        {
            networkManager.AddComponent<NetworkPlayerManager>();
            networkManager.AddComponent<NetworkTrainManager>();
            networkManager.AddComponent<NetworkJunctionManager>();
            networkManager.AddComponent<NetworkSaveGameManager>();
            networkManager.AddComponent<NetworkJobsManager>();

            PlayerManager.PlayerTransform.gameObject.AddComponent<NetworkPlayerSync>().IsLocal = true;
        }

        private static void DeInitializeUnityScripts()
        {
            networkManager.GetComponent<NetworkPlayerManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkPlayerManager>());
            networkManager.GetComponent<NetworkTrainManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkTrainManager>());
            networkManager.GetComponent<NetworkJunctionManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkJunctionManager>());
            networkManager.GetComponent<NetworkSaveGameManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkSaveGameManager>());
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkJobsManager>());

            Object.Destroy(PlayerManager.PlayerTransform.gameObject.GetComponent<NetworkPlayerSync>());
        }

        public static bool IsClient()
        {
            return isClient;
        }

        public static bool IsHost()
        {
            return isHost;
        }
    }
}
