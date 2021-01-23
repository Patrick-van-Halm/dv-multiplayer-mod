using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.DTO;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkPlayerManager : SingletonBehaviour<NetworkPlayerManager>
{
    GameObject networkedPlayer;
    Dictionary<ushort, GameObject> networkPlayers = new Dictionary<ushort, GameObject>();

    protected override void Awake()
    {
        base.Awake();
        networkedPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        networkedPlayer.GetComponent<CapsuleCollider>().enabled = false;
        networkedPlayer.AddComponent<NetworkPlayerSync>();
        networkedPlayer.tag = "NPlayer";
        networkPlayers = new Dictionary<ushort, GameObject>();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
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
            Main.DebugLog($"[CLIENT] PLAYER_DISCONNECT received | Packet size: {reader.Length}");
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
                    Destroy(networkPlayers[disconnectedPlayer.PlayerId]);
                    networkPlayers.Remove(disconnectedPlayer.PlayerId);
                }
            }
        }
    }

    public void OnDisconnect()
    {
        base.OnDestroy();
        foreach(GameObject player in networkPlayers.Values)
        {
            Destroy(player);
        }
        networkPlayers.Clear();
    }

    private void SpawnNetworkPlayer(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] PLAYER_SPAWN received | Packet size: {reader.Length}");
            if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            {
                Main.mod.Logger.Warning("Received malformed spawn packet.");
                return;
            }

            while (reader.Position < reader.Length)
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    GameObject playerObject = Instantiate(networkedPlayer, player.Position, player.Rotation);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.Id = player.Id;
                    if (player.TrainId != "")
                    {
                        playerSync.train = SingletonBehaviour<NetworkTrainManager>.Instance.trainCars.FirstOrDefault(t => t.ID == player.TrainId);
                        if (playerSync.train)
                            playerSync.train.LoadInterior();
                    }
                    networkPlayers.Add(player.Id, playerObject);
                    playerObject.GetComponent<NetworkPlayerSync>().UpdateLocation(player.Position, player.Rotation);
                }
            }
        }
    }

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
            //Main.DebugLog($"[CLIENT] PLAYER_LOCATION_UPDATE received | Packet size: {reader.Length}"); //Commented due to console spam in debug
            if (reader.Length % 30 != 0)
            {
                Main.mod.Logger.Warning("Received malformed location update packet.");
                return;
            }

            while (reader.Position < reader.Length)
            {
                Location location = reader.ReadSerializable<Location>();

                GameObject playerObject = null;
                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(location.Id, out playerObject))
                {
                    playerObject.GetComponent<NetworkPlayerSync>().UpdateLocation(location.Position, location.Rotation);
                }
            }
        }
    }

    internal GameObject GetPlayerById(ushort playerId)
    {
        return networkPlayers[playerId];
    }

    internal GameObject GetLocalPlayer()
    {
        return PlayerManager.PlayerTransform.gameObject;
    }

    internal NetworkPlayerSync GetLocalPlayerSync()
    {
        return GetLocalPlayer().GetComponent<NetworkPlayerSync>();
    }

    internal NetworkPlayerSync GetPlayerSyncById(ushort playerId)
    {
        return networkPlayers[playerId].GetComponent<NetworkPlayerSync>();
    }

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

    internal int GetPlayerCount()
    {
        return networkPlayers.Count;
    }

    internal GameObject[] GetPlayersInTrain(TrainCar train)
    {
        return networkPlayers.Values.Where(p => p.GetComponent<NetworkPlayerSync>().train?.CarGUID == train.CarGUID).ToArray();
    }
}