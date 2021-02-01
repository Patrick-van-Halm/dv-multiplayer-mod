﻿using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using DVMultiplayer;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using DV.TerrainSystem;
using Newtonsoft.Json;

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
            Main.DebugLog("[CLIENT] > SAVEGAME_UPDATE");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write<SaveGame>(new SaveGame()
                {
                    SaveDataCars = SaveGameManager.data.GetJObject(SaveGameKeys.Cars).ToString(Formatting.None),
                    PlayerPos = SaveGameManager.data.GetVector3("Player_position").Value
                });
                Main.DebugLog($"[CLIENT] > SAVEGAME_UPDATE {writer.Length}");

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
        if(offlineSave != null)
        {
            isLoadingSave = true;
            SaveGameManager.data = offlineSave;
            offlineSave = null;
            SaveGameUpgrader.Upgrade();

            SingletonBehaviour<CoroutineManager>.Instance.Run(LoadOfflineSave());
        }
    }

    private IEnumerator LoadOfflineSave()
    {
        SingletonBehaviour<NetworkJobsManager>.Instance.PlayerDisconnect();
        UUI.UnlockMouse(true);
        TutorialController.movementAllowed = false;
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        PlayerManager.PlayerTransform.position = vector3_1 + WorldMover.currentMove;
        bool carsLoadedSuccessfully = false;
        JObject jobject3 = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jobject3 != null)
        {
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jobject3);
            if (!carsLoadedSuccessfully)
                Main.DebugLog("[WARNING] Cars not loaded successfully!");
        }
        else
            Main.DebugLog("[WARNING] Cars save not found!");

        if (carsLoadedSuccessfully)
        {
            JobsSaveGameData saveData = SaveGameManager.data.GetObject<JobsSaveGameData>(SaveGameKeys.Jobs, JobSaveManager.serializeSettings);
            if (saveData != null)
            {
                SingletonBehaviour<JobSaveManager>.Instance.LoadJobSaveGameData(saveData);
            }
            else
                Main.DebugLog("[WARNING] Jobs save not found!");
            SingletonBehaviour<JobSaveManager>.Instance.MarkAllNonJobCarsAsUnused();
        }

        SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
        yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
        UUI.UnlockMouse(false);
        TutorialController.movementAllowed = true;
        SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = false;
        isLoadingSave = false;
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
                SaveGameManager.data.SetJObject(SaveGameKeys.Cars, JObject.Parse(save.SaveDataCars));
                SaveGameManager.data.SetVector3(SaveGameKeys.Player_position, save.PlayerPos);
                SaveGameUpgrader.Upgrade();
                IsHostSaveLoaded = false;
                IsHostSaveReceived = true;
            }
        }
    }

    public void LoadMultiplayerData()
    {
        SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        PlayerManager.PlayerTransform.position = vector3_1 + WorldMover.currentMove;
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