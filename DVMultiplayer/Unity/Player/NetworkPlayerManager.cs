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

                case NetworkTags.PLAYER_LOCATION_UPDATE:
                    UpdateNetworkPositionAndRotation(message);
                    break;
            }
        }
    }

    private void OnPlayerDisconnect(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

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
        Main.DebugLog("[CLIENT] > PLAYER_SPAWN");
        Vector3 pos = PlayerManager.PlayerTransform.position;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<NPlayer>(new NPlayer()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Username = PlayerManager.PlayerTransform.GetComponent<NetworkPlayerSync>().Username,
                Position = pos - WorldMover.currentMove,
                Rotation = PlayerManager.PlayerTransform.rotation,
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        SingletonBehaviour<WorldMover>.Instance.WorldMoved += WorldMoved;
        SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForHost());
    }

    private IEnumerator WaitForHost()
    {
        SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = true;
        UUI.UnlockMouse(true);
        TutorialController.movementAllowed = false;
        if (!NetworkManager.IsHost())
        {
            // Check if host is connected if so the savegame should be available to receive
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
            yield return new WaitUntil(() => networkPlayers.ContainsKey(0));

            // Get the online save game
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveReceived);

            // Load the online save game
            SingletonBehaviour<NetworkSaveGameManager>.Instance.LoadMultiplayerData();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoaded || SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoadedFailed);

            // Wait till world is loaded
            yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));

            if (SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveLoadedFailed)
            {
                Main.DebugLog("Connection failed syncing savegame");
                NetworkManager.Disconnect();
            }
        }
        else
        {
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();
        }

        SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
        SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = false;
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
        SingletonBehaviour<WorldMover>.Instance.WorldMoved -= WorldMoved;
        SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
    }

    private void SpawnNetworkPlayer(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Main.DebugLog($"[CLIENT] < PLAYER_SPAWN received: Username: {player.Username} ");
                    Vector3 pos = player.Position + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    GameObject playerObject = Instantiate(GetPlayerObject(), pos, player.Rotation);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.absPosition = player.Position;
                    playerSync.Id = player.Id;
                    playerSync.Username = player.Username;
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
                AbsPosition = position,
                NewRotation = rotation
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    private void UpdateNetworkPositionAndRotation(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            //Main.DebugLog($"[CLIENT] PLAYER_LOCATION_UPDATE received | Packet size: {reader.Length}"); //Commented due to console spam in debug
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed location update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                Location location = reader.ReadSerializable<Location>();

                GameObject playerObject = null;
                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(location.Id, out playerObject))
                {

                    Vector3 pos = location.AbsPosition + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.absPosition = location.AbsPosition;
                    playerSync.UpdateLocation(pos, location.NewRotation);
                }
            }
        }
    }

    private void WorldMoved(WorldMover mover, Vector3 newPos)
    {
        Main.DebugLog("[CLIENT] > PLAYER_WORLDMOVED");
        foreach(NetworkPlayerSync playerSync in GetAllNonLocalPlayerSync())
        {
            Vector3 pos = playerSync.absPosition + WorldMover.currentMove;
            pos = new Vector3(pos.x, pos.y + 1, pos.z);
            playerSync.UpdateLocation(pos);
        }
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<WorldMove>(new WorldMove()
            {
                WorldPosition = WorldMover.currentMove
            });
            Main.DebugLog($"[CLIENT] > PLAYER_WORLDMOVED {writer.Length}");

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_WORLDMOVED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void OnWorldMoved(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] SAVEGAME_WORLDMOVED received | Packet size: {reader.Length}");
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                WorldMove move = reader.ReadSerializable<WorldMove>();
                if (move.PlayerId != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(move.PlayerId, out GameObject playerObject))
                {
                    playerObject.GetComponent<NetworkPlayerSync>().currentWorldMove = move.WorldPosition;
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