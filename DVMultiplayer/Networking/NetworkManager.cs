using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server.Unity;
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
        private static bool isConnecting;
        private static string host;
        private static int port;
        internal static string username;
        private static bool scriptsInitialized = false;
        private static int tries = 1;

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
                UI.Setup();
            }
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
        public static void Connect(string host, int port, string username)
        {
            NetworkManager.username = username;
            NetworkManager.host = host;
            NetworkManager.port = port;
            ClientConnect();
        }

        private static void ClientConnect()
        {
            if (isClient || isConnecting)
                return;

            isConnecting = true;
            Main.DebugLog("[CLIENT] Connecting to server");
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
        public static void StartServer(string username, ushort port)
        {
            if (isHost)
                return;

            NetworkManager.username = username;
            Main.DebugLog("Start hosting server");
            server.port = port;
            NetworkManager.port = port;
            try
            {
                SingletonBehaviour<CoroutineManager>.Instance.Run(StartHosting());
            }
            catch (Exception ex)
            {
                Main.mod.Logger.Error(ex.Message);
            }
        }

        private static IEnumerator StartHosting()
        {
            server.Create();
            yield return new WaitUntil(() => server.CheckTCPSocketReady());
            Main.DebugLog($"Server should be started connecting client now");
            isHost = true;
            host = "127.0.0.1";
            ClientConnect();
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

        private static IEnumerator StopHosting()
        {
            Disconnect();
            yield return new WaitUntil(() => !isClient);
            try
            {
                server.Close();
                isHost = false;
                TutorialController.movementAllowed = true;
            }
            catch (Exception ex)
            {
                Main.DebugLog($"[ERROR] {ex.Message}");
            }
        }

        private static void OnConnected(Exception ex)
        {
            isConnecting = false;
            if (ex != null && !string.IsNullOrEmpty(ex.Message))
            {
                isClient = false;
                Main.DebugLog($"[ERROR] {ex.Message}");
                Main.mod.Logger.Log($"[CLIENT] Connecting failed retrying..., tries: {tries}/5");
                if (tries <= 5)
                {
                    tries++;
                    ClientConnect();
                }
                else
                {
                    Main.mod.Logger.Log($"[CLIENT] Connecting failed stopping retries.");
                    tries = 0;
                }
            }
            else
            {
                isClient = true;
                UI.HideUI();
                if (!scriptsInitialized)
                {
                    Main.DebugLog($"Client connected loading required unity scripts");
                    InitializeUnityScripts();
                    scriptsInitialized = true;
                }

                Main.DebugLog($"Disabling autosave");
                SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = true;
                CarSpawner.useCarPooling = false;

                Main.DebugLog($"Everything should be initialized running PlayerConnect method");
                SingletonBehaviour<NetworkPlayerManager>.Instance.PlayerConnect();
                SingletonBehaviour<NetworkTrainManager>.Instance.PlayerConnect();
                Main.DebugLog($"Connecting finished");
            }
        }

        private static void InitializeUnityScripts()
        {
            Main.DebugLog($"[CLIENT] Initializing Player");
            NetworkPlayerSync playerSync = PlayerManager.PlayerTransform.gameObject.AddComponent<NetworkPlayerSync>();
            playerSync.IsLocal = true;
            playerSync.Username = username;

            Main.DebugLog($"[CLIENT] Initializing NetworkPlayerManager");
            networkManager.AddComponent<NetworkPlayerManager>();
            Main.DebugLog($"[CLIENT] Initializing NetworkTrainManager");
            networkManager.AddComponent<NetworkTrainManager>();
            Main.DebugLog($"[CLIENT] Initializing NetworkJunctionManager");
            networkManager.AddComponent<NetworkJunctionManager>();
            Main.DebugLog($"[CLIENT] Initializing NetworkSaveGameManager");
            networkManager.AddComponent<NetworkSaveGameManager>();
            Main.DebugLog($"[CLIENT] Initializing NetworkJobsManager");
            networkManager.AddComponent<NetworkJobsManager>();
            Main.DebugLog($"[CLIENT] Initializing NetworkTurntableManager");
            networkManager.AddComponent<NetworkTurntableManager>();
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
            Object.Destroy(networkManager.GetComponent<NetworkJobsManager>());
            Main.DebugLog($"[DISCONNECTING] NetworkSaveGameManager Deinitializing");
            networkManager.GetComponent<NetworkSaveGameManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkSaveGameManager>());
            Main.DebugLog($"[DISCONNECTING] Initializing NetworkTurntableManager");
            networkManager.GetComponent<NetworkTurntableManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkTurntableManager>());

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
