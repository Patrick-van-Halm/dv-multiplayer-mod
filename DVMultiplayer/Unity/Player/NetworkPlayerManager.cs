using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using DVMultiplayer.Utils.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayerManager : SingletonBehaviour<NetworkPlayerManager>
{
    private Dictionary<ushort, GameObject> networkPlayers = new Dictionary<ushort, GameObject>();
    private SetSpawn spawnData;
    private Coroutine playersLoaded;
    private bool modMismatched = false;

    public bool IsSynced { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        networkPlayers = new Dictionary<ushort, GameObject>();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    private GameObject GetNewPlayerObject(Vector3 pos, Quaternion rotation, string username)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.transform.position = pos;
        player.transform.rotation = rotation;
        player.GetComponent<CapsuleCollider>().enabled = false;
        player.AddComponent<NetworkPlayerSync>();

        GameObject nametagCanvas = new GameObject("Nametag Canvas");
        nametagCanvas.transform.parent = player.transform;
        nametagCanvas.transform.localPosition = new Vector3(0, 1.5f, 0);
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
        tag.text = username;

        rectTransform = nametag.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 3f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);

        GameObject p = Instantiate(player);
        Destroy(player);
        return p;
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

                case NetworkTags.PLAYER_LOADED:
                    SetPlayerLoaded(message);
                    break;
            }
        }
    }

    private void SetPlayerLoaded(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                PlayerLoaded player = reader.ReadSerializable<PlayerLoaded>();
                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    if (networkPlayers.TryGetValue(player.Id, out GameObject playerObject))
                    {
                        playerObject.GetComponent<NetworkPlayerSync>().IsLoaded = true;
                    }
                    else
                    {
                        Main.mod.Logger.Critical($"Player with ID: {player.Id} not found");
                    }
                }
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

                if (missingMods.Length > 0)
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
                    Position = SavedPositions.Stations.Values.OrderBy(x => Math.Abs((x - pos).sqrMagnitude)).First()
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
        UUI.UnlockMouse(true);
        TutorialController.movementAllowed = false;
        if (!NetworkManager.IsHost())
        {
            AppUtil.Instance.PauseGame();
            CustomUI.OpenPopup("Connecting", "Loading savegame");
            Main.DebugLog($"[CLIENT] Receiving savegame");
            // Check if host is connected if so the savegame should be available to receive
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
            yield return new WaitUntil(() => networkPlayers.ContainsKey(0) || modMismatched);
            if (modMismatched)
            {
                Main.DebugLog($"Mods Mismatched so disconnecting player");
                UUI.UnlockMouse(false);
                TutorialController.movementAllowed = true;
                NetworkManager.Disconnect();
                yield break;
            }

            // Wait till spawn is set
            yield return new WaitUntil(() => spawnData != null);

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

            // Initialize trains on save
            Main.DebugLog($"Save should be loaded. Run OnFinishedLoading in NetworkTrainManager");
            SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.SaveCarsLoaded);

            // Load Train data from server that changed since uptime
            Main.DebugLog($"Syncing Junctions");
            SingletonBehaviour<NetworkJunctionManager>.Instance.SyncJunction();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkJunctionManager>.Instance.IsSynced);

            // Load Turntable data from server that changed since uptime
            Main.DebugLog($"Syncing Turntables");
            SingletonBehaviour<NetworkTurntableManager>.Instance.SyncTurntables();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTurntableManager>.Instance.IsSynced);

            // Load Train data from server that changed since uptime
            Main.DebugLog($"Syncing traincars");
            SingletonBehaviour<NetworkTrainManager>.Instance.SendInitCarsRequest();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced);

            // Load Job data from server that changed since uptime
            Main.DebugLog($"Syncing jobs");
            SingletonBehaviour<NetworkJobsManager>.Instance.SendJobsRequest();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkJobsManager>.Instance.IsSynced);
            SingletonBehaviour<NetworkJobsManager>.Instance.OnFinishLoading();

            AppUtil.Instance.UnpauseGame();
            yield return new WaitUntil(() => !AppUtil.IsPaused);
            yield return new WaitForEndOfFrame();
            PlayerManager.TeleportPlayer(spawnData.Position + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);
            UUI.UnlockMouse(true);
            // Wait till world is loaded
            yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
            CustomUI.Close();
        }
        else
        {
            Main.DebugLog($"[CLIENT] Sending savegame");
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();

            Main.DebugLog($"Save should be loaded. Run OnFinishedLoading in NetworkTrainManager");
            SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
            Main.DebugLog($"Run OnFinishedLoading in NetworkJobsManager");
            SingletonBehaviour<NetworkJobsManager>.Instance.OnFinishLoading();
        }

        SendIsLoaded();
        Main.DebugLog($"Finished loading everything. Unlocking mouse and allow movement");
        UUI.UnlockMouse(false);
        TutorialController.movementAllowed = true;
        // Move to spawn
    }

    private void SendIsLoaded()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<PlayerLoaded>(new PlayerLoaded()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOADED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    /// <summary>
    /// This method is called upon the player disconnects.
    /// </summary>
    public void PlayerDisconnect()
    {
        base.OnDestroy();
        foreach (GameObject player in networkPlayers.Values)
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

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Location playerPos = reader.ReadSerializable<Location>();
                    Main.DebugLog($"[CLIENT] < PLAYER_SPAWN: Username: {player.Username} ");

                    Vector3 pos = playerPos.Position;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    Quaternion rotation = Quaternion.identity;
                    if (playerPos.Rotation.HasValue)
                        rotation = playerPos.Rotation.Value;
                    GameObject playerObject = GetNewPlayerObject(pos, rotation, player.Username);
                    WorldMover.Instance.AddObjectToMove(playerObject.transform);

                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.Id = player.Id;
                    playerSync.Username = player.Username;
                    playerSync.Mods = player.Mods;
                    playerSync.IsLoaded = player.IsLoaded;

                    networkPlayers.Add(player.Id, playerObject);
                    if (!player.IsLoaded)
                        WaitForPlayerLoaded();
                }
            }
        }
    }

    private void WaitForPlayerLoaded()
    {
        if (playersLoaded == null)
        {
            playersLoaded = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForAllPlayersLoaded());
        }
    }

    private IEnumerator WaitForAllPlayersLoaded()
    {
        CustomUI.OpenPopup("Player is connecting", "A new player is connecting");
        AppUtil.Instance.PauseGame();
        yield return new WaitUntil(() => networkPlayers.All(p => p.Value.GetComponent<NetworkPlayerSync>().IsLoaded));
        AppUtil.Instance.UnpauseGame();
        playersLoaded = null;
        CustomUI.Close();
    }

    /// <summary>
    /// This method is called to send a position update to the server
    /// </summary>
    /// <param name="position">The players position</param>
    /// <param name="rotation">The players rotation</param>
    public void UpdateLocalPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (AppUtil.IsPaused)
            return;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Location>(new Location()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Position = position - WorldMover.currentMove,
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

                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(location.Id, out GameObject playerObject))
                {
                    Vector3 pos = location.Position;
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
    /// Gets all player objects
    /// </summary>
    /// <returns>GameObjects of all player</returns>
    internal IEnumerable<GameObject> GetPlayers()
    {
        return networkPlayers.Values;
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
        foreach (GameObject playerObject in networkPlayers.Values)
        {
            NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
            if (playerSync != null)
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

    internal bool IsAnyoneInLocalPlayerRegion()
    {
        foreach (GameObject playerObject in networkPlayers.Values)
        {
            if (SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(playerObject.transform.position - WorldMover.currentMove))
            {
                return true;
            }
        }
        return false;
    }

    internal bool IsPlayerCloseToStation(GameObject player, StationController station)
    {
        StationJobGenerationRange stationRange = station.GetComponent<StationJobGenerationRange>();
        float playerSqrDistanceFromStationCenter = (player.transform.position - stationRange.stationCenterAnchor.position).sqrMagnitude;
        return stationRange.IsPlayerInJobGenerationZone(playerSqrDistanceFromStationCenter);
    }
}