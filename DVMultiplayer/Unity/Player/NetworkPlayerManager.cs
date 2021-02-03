using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.DTO;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkPlayerManager : SingletonBehaviour<NetworkPlayerManager>
{
    Dictionary<ushort, GameObject> networkPlayers = new Dictionary<ushort, GameObject>();
    private SetSpawn spawnData;
    private bool modMismatched = false;

    protected override void Awake()
    {
        base.Awake();
        networkPlayers = new Dictionary<ushort, GameObject>();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    private GameObject GetPlayerObject()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.GetComponent<CapsuleCollider>().enabled = false;
        player.AddComponent<NetworkPlayerSync>();

        GameObject nametagCanvas = new GameObject("Nametag Canvas");
        nametagCanvas.transform.parent = player.transform;
        nametagCanvas.transform.position = new Vector3(0, 1.5f, 0);
        nametagCanvas.AddComponent<Canvas>();
        nametagCanvas.AddComponent<RotateTowardsPlayer>();

        RectTransform rectTransform = nametagCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.localScale = new Vector3(.0013f, .0004f, 0);

        GameObject nametagBackground = new GameObject("Nametag BG");
        nametagBackground.transform.parent = nametagCanvas.transform;
        nametagBackground.transform.localPosition = new Vector3(0, 0, 0);

        RawImage bg = nametagBackground.AddComponent<RawImage>();
        bg.color = new Color(69 / 255, 69 / 255, 69 / 255, .45f);

        rectTransform = nametagBackground.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 1f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);

        GameObject nametag = new GameObject("Nametag");
        nametag.transform.parent = nametagCanvas.transform;
        nametag.transform.localPosition = new Vector3(0, 0, 0);

        Text tag = nametag.AddComponent<Text>();
        tag.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        tag.fontSize = 200;
        tag.alignment = TextAnchor.MiddleCenter;

        rectTransform = nametag.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 3f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);

        return player;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.PLAYER_DISCONNECT:
                    OnPlayerDisconnect(message);
                    break;

                case NetworkTags.PLAYER_SPAWN:
                    SpawnNetworkPlayer(message);
                    break;

                case NetworkTags.PLAYER_MODS_MISMATCH:
                    OnModMismatch(message);
                    break;

                case NetworkTags.PLAYER_LOCATION_UPDATE:
                    UpdateNetworkPositionAndRotation(message);
                    break;

                case NetworkTags.PLAYER_SPAWN_SET:
                    SetSpawnPosition(message);
                    break;
            }
        }
    }

    private void SetSpawnPosition(Message message)
    {
        Main.DebugLog("[CLIENT] < PLAYER_SPAWN_SET");
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                spawnData = reader.ReadSerializable<SetSpawn>();
            }
        }
    }

    private void OnModMismatch(Message message)
    {
        Main.DebugLog("[CLIENT] Client disconnected due to mods mismatch");
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                string[] missingMods = reader.ReadStrings();
                string[] extraMods = reader.ReadStrings();
                
                if(missingMods.Length > 0)
                    Main.mod.Logger.Error($"[MOD MISMATCH] You are missing the following mods: {string.Join(", ", missingMods)}");

                if (extraMods.Length > 0)
                    Main.mod.Logger.Error($"[MOD MISMATCH] You installed mods the host doesn't have, these are: {string.Join(", ", extraMods)}");

                modMismatched = true;
            }
        }
    }

    private void OnPlayerDisconnect(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Disconnect disconnectedPlayer = reader.ReadSerializable<Disconnect>();

                if (disconnectedPlayer.PlayerId != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Main.DebugLog($"[CLIENT] < PLAYER_DISCONNECT: Username: {networkPlayers[disconnectedPlayer.PlayerId].GetComponent<NetworkPlayerSync>().Username}");
                    Destroy(networkPlayers[disconnectedPlayer.PlayerId]);
                    networkPlayers.Remove(disconnectedPlayer.PlayerId);
                }
            }
        }
    }

    /// <summary>
    /// This method is called upon the player connects.
    /// </summary>
    public void PlayerConnect()
    {
        Vector3 pos = PlayerManager.PlayerTransform.position;
        if (NetworkManager.IsHost())
        {
            Main.DebugLog("[CLIENT] > PLAYER_SPAWN_SET");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(new SetSpawn()
                {
                    Position = pos - WorldMover.currentMove
                });

                using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SPAWN_SET, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
        }

        Main.DebugLog("[CLIENT] > PLAYER_INIT");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<NPlayer>(new NPlayer()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Username = PlayerManager.PlayerTransform.GetComponent<NetworkPlayerSync>().Username,
                Mods = Main.GetEnabledMods()
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_INIT, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        
        Main.DebugLog($"Wait for connection initializiation is finished");
        SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForInit());
    }

    private IEnumerator WaitForInit()
    {
        SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = true;
        UUI.UnlockMouse(true);
        TutorialController.movementAllowed = false;
        if (!NetworkManager.IsHost())
        {
            Main.DebugLog($"[CLIENT] Receiving savegame");
            // Check if host is connected if so the savegame should be available to receive
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
            yield return new WaitUntil(() => networkPlayers.ContainsKey(0) || modMismatched);
            if (modMismatched)
            {
                Main.DebugLog($"Mods Mismatched so disconnecting player");
                SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = false;
                UUI.UnlockMouse(false);
                TutorialController.movementAllowed = true;
                NetworkManager.Disconnect();
                yield break;
            }

            // Wait till spawn is set
            yield return new WaitUntil(() => spawnData != null);
            // Move to spawn
            PlayerManager.TeleportPlayer(spawnData.Position + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);

            // Get the online save game
            Main.DebugLog($"Syncing Save");
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveReceived);

            // Load the online save game
            Main.DebugLog($"Syncing Loading save");
            SingletonBehaviour<NetworkSaveGameManager>.Instance.LoadMultiplayerData();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoaded || SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoadedFailed);
            if (SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoadedFailed)
            {
                Main.DebugLog("Connection failed syncing savegame");
                NetworkManager.Disconnect();
            }

            // Wait till world is loaded
            yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));

            // Initialize trains on save
            Main.DebugLog($"Save should be loaded. Run OnFinishedLoading in NetworkTrainManager");
            SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.SaveTrainCarsLoaded);

            // Load Train data from server that changed since uptime
            Main.DebugLog($"Syncing traincars");
            SingletonBehaviour<NetworkTrainManager>.Instance.SyncTrainCars();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced);

            // Load Train data from server that changed since uptime
            Main.DebugLog($"Syncing Junctions");
            SingletonBehaviour<NetworkJunctionManager>.Instance.SyncJunction();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkJunctionManager>.Instance.IsSynced);

            // Load Turntable data from server that changed since uptime
            Main.DebugLog($"Syncing Turntables");
            SingletonBehaviour<NetworkTurntableManager>.Instance.SyncTurntables();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTurntableManager>.Instance.IsSynced);
        }
        else
        {
            Main.DebugLog($"[CLIENT] Sending savegame");
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();

            Main.DebugLog($"Save should be loaded. Run OnFinishedLoading in NetworkTrainManager");
            SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
        }

        SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = false;
        Main.DebugLog($"Finished loading everything. Unlocking mouse and allow movement");
        UUI.UnlockMouse(false);
        TutorialController.movementAllowed = true;
    }

    /// <summary>
    /// This method is called upon the player disconnects.
    /// </summary>
    public void PlayerDisconnect()
    {
        base.OnDestroy();
        foreach(GameObject player in networkPlayers.Values)
        {
            Destroy(player);
        }
        networkPlayers.Clear();
        spawnData = null;
        SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
    }

    private void SpawnNetworkPlayer(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();
                Location playerPos = reader.ReadSerializable<Location>();

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Main.DebugLog($"[CLIENT] < PLAYER_SPAWN: Username: {player.Username} ");

                    Vector3 pos = playerPos.Position + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    Quaternion rotation = Quaternion.identity;
                    if (playerPos.Rotation.HasValue)
                        rotation = playerPos.Rotation.Value;
                    GameObject playerObject = Instantiate(GetPlayerObject(), pos, rotation);

                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.Id = player.Id;
                    playerSync.Username = player.Username;
                    playerSync.Mods = player.Mods;

                    playerObject.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = player.Username;
                    networkPlayers.Add(player.Id, playerObject);
                    WorldMover.Instance.AddObjectToMove(playerObject.transform);
                }
            }
        }
    }

    /// <summary>
    /// This method is called to send a position update to the server
    /// </summary>
    /// <param name="position">The players position</param>
    /// <param name="rotation">The players rotation</param>
    public void UpdateLocalPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Location>(new Location()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Position = position,
                Rotation = rotation
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    private void UpdateNetworkPositionAndRotation(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Location location = reader.ReadSerializable<Location>();

                GameObject playerObject = null;
                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(location.Id, out playerObject))
                {
                    Vector3 pos = location.Position + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.UpdateLocation(pos, location.Rotation);
                }
            }
        }
    }

    /// <summary>
    /// Gets any player with the specified ID
    /// </summary>
    /// <param name="playerId">The network ID assigned to the player</param>
    /// <returns>GameObject of player</returns>
    internal GameObject GetPlayerById(ushort playerId)
    {
        return networkPlayers[playerId];
    }

    /// <summary>
    /// Gets the local player gameobject
    /// </summary>
    /// <returns>Local Player GameObject</returns>
    internal GameObject GetLocalPlayer()
    {
        return PlayerManager.PlayerTransform.gameObject;
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of the local player
    /// </summary>
    /// <returns>NetworkPlayerSync of local player</returns>
    internal NetworkPlayerSync GetLocalPlayerSync()
    {
        return GetLocalPlayer().GetComponent<NetworkPlayerSync>();
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of the player with the specified ID
    /// </summary>
    /// <param name="playerId">The network ID assigned to the player</param>
    /// <returns>NetworkPlayerSync of player with the specified ID</returns>
    internal NetworkPlayerSync GetPlayerSyncById(ushort playerId)
    {
        return networkPlayers[playerId].GetComponent<NetworkPlayerSync>();
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of all non local players.
    /// </summary>
    /// <returns>Readonly list containing the NetworkPlayerSync script of all non local players</returns>
    internal IReadOnlyList<NetworkPlayerSync> GetAllNonLocalPlayerSync()
    {
        List<NetworkPlayerSync> networkPlayerSyncs = new List<NetworkPlayerSync>();
        foreach(GameObject playerObject in networkPlayers.Values)
        {
            NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
            if(playerSync != null)
                networkPlayerSyncs.Add(playerSync);
        }
        return networkPlayerSyncs;
    }

    /// <summary>
    /// Gets the amount of players with the local player NOT included.
    /// </summary>
    /// <returns>The amount of players with the local player NOT included</returns>
    internal int GetPlayerCount()
    {
        return networkPlayers.Count;
    }

    /// <summary>
    /// Gets all the player game objects that are in/on a given traincar.
    /// </summary>
    /// <param name="train">The requested traincar</param>
    /// <returns>An array containing all the players gameobjects in/on the given traincar</returns>
    internal GameObject[] GetPlayersInTrain(TrainCar train)
    {
        return networkPlayers.Values.Where(p => p.GetComponent<NetworkPlayerSync>().Train?.CarGUID == train.CarGUID).ToArray();
    }
}