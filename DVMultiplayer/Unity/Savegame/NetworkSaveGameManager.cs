using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;

internal class NetworkSaveGameManager : SingletonBehaviour<NetworkSaveGameManager>
{
    private OfflineSaveGame offlineSave;
    public bool IsHostSaveReceived { get; private set; }
    public bool IsHostSaveLoadedFailed { get; internal set; }
    public bool IsHostSaveLoaded { get; private set; }
    public bool IsOfflineSaveLoaded { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void SyncSave()
    {
        if (NetworkManager.IsHost())
        {
            Main.Log("[CLIENT] > SAVEGAME_UPDATE");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(new SaveGame()
                {
                    SaveDataSwitches = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions).ToString(Formatting.None),
                    SaveDataTurntables = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables).ToString(Formatting.None),
                });
                Main.Log($"[CLIENT] > SAVEGAME_UPDATE {writer.Length}");

                using (Message message = Message.Create((ushort)NetworkTags.SAVEGAME_UPDATE, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
        }
        else if (!NetworkManager.IsHost() && NetworkManager.IsClient())
        {
            IsHostSaveReceived = false;
            Main.Log("[CLIENT] > SAVEGAME_GET");
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
        IsOfflineSaveLoaded = false;
        if (offlineSave != null && !NetworkManager.IsHost())
        {
            SaveGameManager.data.SetJObject(SaveGameKeys.Cars, JObject.Parse(offlineSave.SaveDataCars));
            SaveGameManager.data.SetObject(SaveGameKeys.Jobs, offlineSave.SaveDataJobs, JobSaveManager.serializeSettings);
            SaveGameManager.data.SetJObject(SaveGameKeys.Junctions, JObject.Parse(offlineSave.SaveDataSwitches));
            SaveGameManager.data.SetJObject(SaveGameKeys.Turntables, JObject.Parse(offlineSave.SaveDataTurntables));
            offlineSave = null;
            SaveGameUpgrader.Upgrade();

            SingletonBehaviour<CoroutineManager>.Instance.Run(LoadOfflineSave());
        }
        else
        {
            IsOfflineSaveLoaded = true;
        }
    }

    private IEnumerator LoadOfflineSave()
    {
        CustomUI.OpenPopup("Disconnecting", "Loading offline save");
        UUI.UnlockMouse(true);
        AppUtil.Instance.PauseGame();
        yield return new WaitUntil(() => AppUtil.IsPaused);
        TutorialController.movementAllowed = false;
        CarSpawner.useCarPooling = true;
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        bool carsLoadedSuccessfully = false;
        JObject jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables);
        if (jObject != null)
        {
            TurntableRailTrack.SetSaveData(jObject);
        }
        else
        {
            Main.Log("[WARNING] Turntables data not found!");
        }
        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions);
        if (jObject != null)
        {
            JunctionsSaveManager.Load(jObject);
        }
        else
        {
            Main.Log("[WARNING] Junctions save not found!");
        }

        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jObject != null)
        {
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jObject);
            if (!carsLoadedSuccessfully)
                Main.Log("[WARNING] Cars not loaded successfully!");
        }
        else
            Main.Log("[WARNING] Cars save not found!");

        if (carsLoadedSuccessfully)
        {
            JobsSaveGameData saveData = SaveGameManager.data.GetObject<JobsSaveGameData>(SaveGameKeys.Jobs, JobSaveManager.serializeSettings);
            if (saveData != null)
            {
                SingletonBehaviour<JobSaveManager>.Instance.LoadJobSaveGameData(saveData);
            }
            else
                Main.Log("[WARNING] Jobs save not found!");
            SingletonBehaviour<JobSaveManager>.Instance.MarkAllNonJobCarsAsUnused();
        }

        SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
        AppUtil.Instance.UnpauseGame();
        UUI.UnlockMouse(true);
        PlayerManager.TeleportPlayer(vector3_1 + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);
        yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
        UUI.UnlockMouse(false);
        CustomUI.Close();
        TutorialController.movementAllowed = true;

        SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = false;
        IsOfflineSaveLoaded = true;
    }

    private void OnSaveGameReceived(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] SAVEGAME_GET received | Packet size: {reader.Length}");
            while (reader.Position < reader.Length)
            {
                SaveGame save = reader.ReadSerializable<SaveGame>();
                offlineSave = new OfflineSaveGame()
                {
                    SaveDataCars = SaveGameManager.data.GetJObject(SaveGameKeys.Cars).ToString(Formatting.None),
                    SaveDataJobs = SaveGameManager.data.GetObject<JobsSaveGameData>(SaveGameKeys.Jobs, JobSaveManager.serializeSettings),
                    SaveDataSwitches = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions).ToString(Formatting.None),
                    SaveDataTurntables = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables).ToString(Formatting.None),
                };
                SaveGameManager.data.SetJObject(SaveGameKeys.Junctions, JObject.Parse(save.SaveDataSwitches));
                SaveGameManager.data.SetJObject(SaveGameKeys.Turntables, JObject.Parse(save.SaveDataTurntables));
                SaveGameUpgrader.Upgrade();
                IsHostSaveLoaded = false;
                IsHostSaveReceived = true;
            }
        }
    }

    public void LoadMultiplayerData()
    {
        SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();

        JObject jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables);
        if (jObject != null)
        {
            TurntableRailTrack.SetSaveData(jObject);
        }
        else
        {
            IsHostSaveLoadedFailed = true;
            Main.Log("[WARNING] Turntables data not found!");
        }
        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions);
        if (jObject != null)
        {
            JunctionsSaveManager.Load(jObject);
        }
        else
        {
            IsHostSaveLoadedFailed = true;
            Main.Log("[WARNING] Junctions save not found!");
        }
        IsHostSaveLoaded = !IsHostSaveLoadedFailed;
    }
}