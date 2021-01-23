using DarkRift;
using DarkRift.Client.Unity;
using DarkRift.Server.Unity;
using DVMultiplayer.DTO.Player;
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
            DeInitializeUnityScripts();
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
                InitializeUnityScripts();
                SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = true;
                Vector3 pos = PlayerManager.PlayerTransform.position;
                NetworkPlayerSync localPlayer = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
                localPlayer.train = PlayerManager.Car;
                if (localPlayer.train)
                    localPlayer.GetComponent<NetworkTrainSync>().ListenToTrainInputEvents();
                Main.DebugLog("[CLIENT] > PLAYER_SPAWN");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write<NPlayer>(new NPlayer()
                    {
                        Id = client.ID,
                        Position = new Vector3(pos.x, pos.y + 1, pos.z),
                        Rotation = PlayerManager.PlayerTransform.rotation,
                        TrainId = localPlayer.train ? localPlayer.train.ID : ""
                    });

                    using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                        client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        private static void InitializeUnityScripts()
        {
            networkManager.AddComponent<NetworkPlayerManager>();
            networkManager.AddComponent<NetworkTrainManager>();
            networkManager.AddComponent<NetworkJunctionManager>();

            PlayerManager.PlayerTransform.gameObject.AddComponent<NetworkPlayerSync>().IsLocal = true;
        }

        private static void DeInitializeUnityScripts()
        {
            networkManager.GetComponent<NetworkPlayerManager>().OnDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkPlayerManager>());
            networkManager.GetComponent<NetworkTrainManager>().PlayerDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkTrainManager>());
            networkManager.GetComponent<NetworkJunctionManager>().OnDisconnect();
            Object.Destroy(networkManager.GetComponent<NetworkJunctionManager>());

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
