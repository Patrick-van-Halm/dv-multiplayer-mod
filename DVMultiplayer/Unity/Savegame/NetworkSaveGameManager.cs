using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using DVMultiplayer;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;

class NetworkSaveGameManager : SingletonBehaviour<NetworkSaveGameManager>
{
    private SaveGameData offlineSave;
    public bool isLoadingSave;
    public bool IsHostSaveReceived { get; private set; }
    public bool IsHostSaveLoaded { get; private set; }
    public bool IsHostSaveLoadedFailed { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void SyncSave()
    {
        if (NetworkManager.IsHost())
        {
            Main.DebugLog("[CLIENT] > SAVEGAME_SYNC");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write<SaveGame>(new SaveGame()
                {
                    SaveDataString = SaveGameManager.data.GetJsonString()
                });
                Main.DebugLog($"[CLIENT] > SAVEGAME_SYNC {writer.Length}");

                using (Message message = Message.Create((ushort)NetworkTags.SAVEGAME_UPDATE, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
        }
        else if (!NetworkManager.IsHost() && NetworkManager.IsClient())
        {
            IsHostSaveReceived = false;
            Main.DebugLog("[CLIENT] > SAVEGAME_GET");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                using (Message message = Message.Create((ushort)NetworkTags.SAVEGAME_GET, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.SAVEGAME_GET:
                    OnSaveGameReceived(message);
                    break;
            }
        }
    }

    public void PlayerDisconnect()
    {
        SaveGameManager.data = offlineSave;
        SaveGameUpgrader.Upgrade();
    }

    private void OnSaveGameReceived(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] SAVEGAME_GET received | Packet size: {reader.Length}");
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                SaveGame save = reader.ReadSerializable<SaveGame>();
                offlineSave = SaveGameManager.data;
                SaveGameManager.data = SaveGameData.LoadFromString(save.SaveDataString);
                SaveGameUpgrader.Upgrade();
                IsHostSaveLoaded = false;
                IsHostSaveReceived = true;
            }
        }
    }

    public void LoadMultiplayerData()
    {
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        Vector3 vector3_2 = SaveGameManager.data.GetVector3("Player_rotation").Value;
        PlayerManager.PlayerTransform.position = vector3_1 + WorldMover.currentMove;
        PlayerManager.PlayerTransform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Quaternion.Euler(vector3_2) * Vector3.forward, Vector3.up).normalized);
        bool carsLoadedSuccessfully = false;
        JObject jobject3 = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jobject3 != null)
        {
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jobject3);
            if (!carsLoadedSuccessfully)
                Debug.LogError((object)"Cars not loaded successfully!");
        }
        else
            Main.DebugLog("[WARNING] Cars save not found!");

        
        IsHostSaveLoadedFailed = !carsLoadedSuccessfully;
        IsHostSaveLoaded = carsLoadedSuccessfully;
    }

    private void OnGUI()
    {
        if (isLoadingSave)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "DV Multiplayer");
            GUI.Label(new Rect(10, Screen.height / 2 - 20, Screen.width - 20, 40), "Loading SaveGame", UUI.GenerateStyle(fontSize: 40, allignment: TextAnchor.MiddleCenter));
        }
    }
}