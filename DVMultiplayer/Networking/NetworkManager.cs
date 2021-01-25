using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server.Unity;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Utils;
using System;
using System.Collections;
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

        /// <summary>
        /// Initializes the NetworkManager by:
        /// Spawning all the components needed.
        /// Listening to events.
        /// </summary>
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

                client.Disconnected += OnClientDisconnected;

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

        private static void OnClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            UI.HideUI();
            if (scriptsInitialized)
            {
                DeInitializeUnityScripts();
                scriptsInitialized = false;
            }
            isClient = false;
            client.Close();
        }

        /// <summary>
        /// Deinitializing by destroying this gameobject.
        /// </summary>
        public static void Deinitialize()
        {
            Main.DebugLog("Deinitializing NetworkManager");

            if (UGameObject.Exists("NetworkManager"))
            {
                GameObject.Destroy(GameObject.Find("NetworkManager"));
            }
        }

        /// <summary>
        /// Connects to the server with a given host and port
        /// </summary>
        /// <param name="host">The hostname to connect to</param>
        /// <param name="port">The port of the server</param>
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

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public static void Disconnect()
        {
            if (!isClient)
                return;

            Main.DebugLog($"Disconnecting client");
            try
            {
                client.Disconnect();
            }
            catch (Exception ex)
            {
                Main.DebugLog($"[ERROR] {ex.InnerException}");
            }
        }

        /// <summary>
        /// Starts up the game server and connects to it automatically
        /// </summary>
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

        /// <summary>
        /// Stops the hosted server and disconnects from it
        /// </summary>
        public static void StopServer()
        {
            if (!isHost)
                return;

            Main.DebugLog("Stop hosting server");
            SingletonBehaviour<CoroutineManager>.Instance.Run(StopHosting());
        }

        static IEnumerator StopHosting()
        {
            Disconnect();
            yield return new WaitUntil(() => !isClient);
            try
            {
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
                UI.HideUI();
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
            Main.DebugLog($"[DISCONNECTING] NetworkPlayerManager Deinitializing");
            networkManager.GetComponent<NetworkPlayerManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkPlayerManager>());
            Main.DebugLog($"[DISCONNECTING] NetworkTrainManager Deinitializing");
            networkManager.GetComponent<NetworkTrainManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkTrainManager>());
            Main.DebugLog($"[DISCONNECTING] NetworkJunctionManager Deinitializing");
            networkManager.GetComponent<NetworkJunctionManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkJunctionManager>());
            Main.DebugLog($"[DISCONNECTING] NetworkJobsManager Deinitializing");
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkJobsManager>());
            Main.DebugLog($"[DISCONNECTING] NetworkSaveGameManager Deinitializing");
            networkManager.GetComponent<NetworkSaveGameManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkSaveGameManager>());

            Main.DebugLog($"[DISCONNECTING] NetworkPlayerSync Deinitializing");
            Object.Destroy(PlayerManager.PlayerTransform.gameObject.GetComponent<NetworkPlayerSync>());
        }

        /// <summary>
        /// Gets the value if the current local user is connected with a client.
        /// </summary>
        /// <returns>If the user is connected to a server as client</returns>
        public static bool IsClient()
        {
            return isClient;
        }

        /// <summary>
        /// Gets the value if the current local user is hosting a server.
        /// </summary>
        /// <returns>If the user is hosting a server</returns>
        public static bool IsHost()
        {
            return isHost;
        }
    }
}
