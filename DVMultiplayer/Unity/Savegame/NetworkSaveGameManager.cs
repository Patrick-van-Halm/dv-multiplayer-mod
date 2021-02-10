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
using System.Collections;
using DV.TerrainSystem;
using Newtonsoft.Json;

class NetworkSaveGameManager : SingletonBehaviour<NetworkSaveGameManager>
{
    private SaveGameData offlineSave;
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
                    SaveDataSwitches = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions).ToString(Formatting.None),
                    SaveDataTurntables = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables).ToString(Formatting.None),
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
        if(offlineSave != null && !NetworkManager.IsHost())
        {
            SaveGameData onlineSave = SaveGameManager.data;
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
        CarSpawner.useCarPooling = true;
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        PlayerManager.PlayerTransform.position = vector3_1 + WorldMover.currentMove;
        bool carsLoadedSuccessfully = false;
        JObject jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables);
        if (jObject != null)
        {
            TurntableRailTrack.SetSaveData(jObject);
        }
        else
        {
            Main.DebugLog("[WARNING] Turntables data not found!");
        }
        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions);
        if (jObject != null)
        {
            JunctionsSaveManager.Load(jObject);
        }
        else
        {
            Main.DebugLog("[WARNING] Junctions save not found!");
        }

        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jObject != null)
        {
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jObject);
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
    }

    private void OnSaveGameReceived(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] SAVEGAME_GET received | Packet size: {reader.Length}");
            while (reader.Position < reader.Length)
            {
                SaveGame save = reader.ReadSerializable<SaveGame>();
                offlineSave = SaveGameManager.data;
                SaveGameManager.data.SetJObject(SaveGameKeys.Cars, JObject.Parse(save.SaveDataCars));
                SaveGameManager.data.SetJObject(SaveGameKeys.Junctions, JObject.Parse(save.SaveDataSwitches));
                SaveGameUpgrader.Upgrade();
                IsHostSaveLoaded = false;
                IsHostSaveReceived = true;
            }
        }
    }

    public void LoadMultiplayerData()
    {
        SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
        bool carsLoadedSuccessfully = false;
        
        JObject jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables);
        if (jObject != null)
        {
            TurntableRailTrack.SetSaveData(jObject);
        }
        else
        {
            Main.DebugLog("[WARNING] Turntables data not found!");
        }
        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions);
        if (jObject != null)
        {
            JunctionsSaveManager.Load(jObject);
        }
        else
        {
            Main.DebugLog("[WARNING] Junctions save not found!");
        }

        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jObject != null)
        {
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jObject);
            if (!carsLoadedSuccessfully)
                Debug.LogError((object)"Cars not loaded successfully!");
        }
        else
            Main.DebugLog("[WARNING] Cars save not found!");

        
        IsHostSaveLoadedFailed = !carsLoadedSuccessfully;
        IsHostSaveLoaded = carsLoadedSuccessfully;
    }
}