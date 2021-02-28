using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.CabControls;
using DV.Logic.Job;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTrainManager : SingletonBehaviour<NetworkTrainManager>
{
    public List<TrainCar> localCars = new List<TrainCar>();
    public List<WorldTrain> serverCarStates = new List<WorldTrain>();
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; private set; }
    public bool SaveCarsLoaded { get; internal set; }
    public bool IsSpawningTrains { get; set; } = false;
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        base.Awake();
        IsChangeByNetwork = false;
        localCars = new List<TrainCar>();
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += OnMessageReceived;

        Main.Log($"Listening to CarChanged event");
        PlayerManager.CarChanged += OnPlayerSwitchTrainCarEvent;
        CarSpawner.CarSpawned += OnCarSpawned;
        CarSpawner.CarAboutToBeDeleted += OnCarAboutToBeDeleted;
    }

    private void Update()
    {
        if (IsSpawningTrains || IsSynced)
            return;

        foreach(TrainCar car in localCars.ToList())
        {
            if(car.logicCar == null)
            {
                localCars.Remove(car);
            }
        }
    }

    #region Events
    public void OnFinishedLoading()
    {
        SaveCarsLoaded = false;
        localCars = GameObject.FindObjectsOfType<TrainCar>().ToList();
        Main.Log($"{localCars.Count} traincars found, {localCars.Where(car => car.IsLoco).Count()} are locomotives");

        foreach (TrainCar trainCar in localCars)
        {
            Main.Log($"Initializing TrainCar Coupling scripts");
            trainCar.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
            trainCar.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
            Main.Log($"Initializing TrainCar Positioning script");
            trainCar.gameObject.AddComponent<NetworkTrainPosSync>();

            if (trainCar.IsLoco)
            {
                Main.Log($"Initializing TrainCar input script");
                trainCar.gameObject.AddComponent<NetworkTrainSync>();
            }
        }

        SendInitializedCars();
        SaveCarsLoaded = true;
    }

    private void OnCarAboutToBeDeleted(TrainCar car)
    {
        if (IsChangeByNetwork || !IsSynced)
            return;

        localCars.Remove(car);
        SendCarBeingRemoved(car);
    }

    private void OnCarSpawned(TrainCar car)
    {
        if (IsChangeByNetwork || !IsSynced || IsSpawningTrains)
            return;

        if (car.IsLoco || car.playerSpawnedCar)
        {
            AddNetworkingScripts(car);

            SendNewCarSpawned(car);
            AppUtil.Instance.PauseGame();
            CustomUI.OpenPopup("Streaming", "New Area being loaded");
        }
    }

    internal void PlayerConnect()
    {
        if (!NetworkManager.IsHost())
        {
            foreach (TrainCar spCar in GameObject.FindObjectsOfType<TrainCar>())
            {
                CarSpawner.DeleteCar(spCar);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Instance)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= OnMessageReceived;
        PlayerManager.CarChanged -= OnPlayerSwitchTrainCarEvent;
        CarSpawner.CarSpawned -= OnCarSpawned;
        CarSpawner.CarAboutToBeDeleted -= OnCarAboutToBeDeleted;
        if (localCars == null)
            return;

        foreach (TrainCar trainCar in localCars)
        {
            if (!trainCar)
                continue;

            if (trainCar.GetComponent<NetworkTrainPosSync>())
                DestroyImmediate(trainCar.GetComponent<NetworkTrainPosSync>());
            if (trainCar.GetComponent<NetworkTrainSync>())
                DestroyImmediate(trainCar.GetComponent<NetworkTrainSync>());
            if (trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                DestroyImmediate(trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>());
            if (trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                DestroyImmediate(trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>());
        }

        if (!NetworkManager.IsHost())
        {
            foreach (TrainCar trainCar in localCars)
            {
                if (!trainCar)
                    continue;

                CarSpawner.DeleteCar(trainCar);
            }
        }

        localCars.Clear();
    }

    private void OnPlayerSwitchTrainCarEvent(TrainCar trainCar)
    {
        if (trainCar)
        {
            if (!trainCar.GetComponent<NetworkTrainSync>() && trainCar.IsLoco)
                trainCar.gameObject.AddComponent<NetworkTrainSync>();

            if (!trainCar.GetComponent<NetworkTrainPosSync>())
                trainCar.gameObject.AddComponent<NetworkTrainPosSync>();

            if (!trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                trainCar.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

            if (!trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                trainCar.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        }

        NetworkPlayerSync playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
        if (playerSync.Train && playerSync.Train.IsLoco)
            playerSync.Train.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = false;

        playerSync.Train = trainCar;
        SendPlayerCarChange(trainCar);

        if (trainCar && trainCar.IsLoco)
            trainCar.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = true;
    }

    private void NetworkTrainManager_OnTrainCarInitialized(TrainCar train)
    {
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
        if (!train.IsLoco && serverState.CargoType != CargoType.None)
        {
            train.logicCar.LoadCargo(serverState.CargoAmount, serverState.CargoType);
        }
    }
    #endregion

    #region Messaging

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.TRAIN_LEVER:
                    OnLocoLeverMessage(message);
                    break;

                case NetworkTags.TRAIN_LOCATION_UPDATE:
                    OnCarLocationMessage(message);
                    break;

                case NetworkTags.TRAIN_SWITCH:
                    OnPlayerCarChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_DERAIL:
                    OnCarDerailmentMessage(message);
                    break;

                case NetworkTags.TRAIN_COUPLE:
                    OnCarCoupleChangeMessage(message, true);
                    break;

                case NetworkTags.TRAIN_UNCOUPLE:
                    OnCarCoupleChangeMessage(message, false);
                    break;

                case NetworkTags.TRAIN_COUPLE_HOSE:
                    OnCarCouplerHoseChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_COUPLE_COCK:
                    OnCarCouplerCockChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_SYNC_ALL:
                    OnCarSyncAllMessage(message);
                    break;

                case NetworkTags.TRAIN_RERAIL:
                    OnCarRerailMessage(message);
                    break;

                case NetworkTags.TRAINS_INIT:
                    OnCarInitMessage(message);
                    break;

                case NetworkTags.TRAINS_INIT_FINISHED:
                    OnAllClientsNewTrainsLoaded();
                    break;

                case NetworkTags.TRAIN_REMOVAL:
                    OnCarRemovalMessage(message);
                    break;

                case NetworkTags.TRAIN_DAMAGE:
                    OnCarDamageMessage(message);
                    break;

                case NetworkTags.TRAIN_AUTH_CHANGE:
                    OnAuthorityChangeMessage(message);
                    break;
            }
        }
    }
    #endregion

    #region Receiving Messages
    private void OnAllClientsNewTrainsLoaded()
    {
        Main.Log("[CLIENT] < TRAINS_INIT_FINISHED");
        CustomUI.Close();
        AppUtil.Instance.UnpauseGame();
        IsSpawningTrains = false;
    }

    private void OnCarDamageMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarDamageMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Main.Log($"[CLIENT] < TRAIN_DAMAGE");
                CarDamage damage = reader.ReadSerializable<CarDamage>();
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == damage.Guid);
                if (train)
                {
                    IsChangeByNetwork = true;
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == damage.Guid);
                    UpdateServerStateDamage(serverState, damage.DamageType, damage.NewHealth);
                    SyncLocomotiveWithServerState(train, serverState);
                    switch (damage.DamageType)
                    {
                        case DamageType.Car:
                            train.CarDamage.LoadCarDamageState(damage.NewHealth);
                            break;

                        case DamageType.Cargo:
                            train.CargoDamage.LoadCargoDamageState(damage.NewHealth);
                            break;
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarRemovalMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarRemovalMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                CarRemoval carRemoval = reader.ReadSerializable<CarRemoval>();
                TrainCar train = localCars.ToList().FirstOrDefault(t => t.CarGUID == carRemoval.Guid);
                if (train)
                {
                    IsChangeByNetwork = true;
                    localCars.Remove(train);
                    CarSpawner.DeleteCar(train);
                    for(int i = localCars.Count - 1; i >= 0 ; i--)
                    {
                        if (!localCars[i] || localCars[i].logicCar == null)
                            localCars.RemoveAt(i);
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarInitMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarInitMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                IsSpawningTrains = true;
                Main.Log($"[CLIENT] < TRAINS_INIT");
                IsChangeByNetwork = true;
                WorldTrain[] trains = reader.ReadSerializables<WorldTrain>();
                SingletonBehaviour<CoroutineManager>.Instance.Run(SpawnSendedTrains(trains));
                IsChangeByNetwork = false;
            }
        }
    }

    private void OnCarRerailMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarRerailMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainRerail data = reader.ReadSerializable<TrainRerail>();
                Main.Log($"[CLIENT] < TRAIN_RERAIL: ID: {data.Guid}");
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == data.Guid);
                if (train)
                {
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == data.Guid);
                    if (serverState != null)
                    {
                        serverState.Position = data.Position;
                        serverState.Rotation = data.Rotation;
                        serverState.Forward = data.Forward;
                        serverState.Bogies[0] = new TrainBogie()
                        {
                            TrackName = data.Bogie1TrackName,
                            PositionAlongTrack = data.Bogie1PositionAlongTrack,
                            Derailed = false
                        };
                        serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                        {
                            TrackName = data.Bogie2TrackName,
                            PositionAlongTrack = data.Bogie2PositionAlongTrack,
                            Derailed = false
                        };
                        serverState.CarHealth = data.CarHealth;
                        if (!serverState.IsLoco)
                            serverState.CargoHealth = data.CargoHealth;
                        else
                        {
                            serverState.Throttle = 0;
                            serverState.Sander = 0;
                            serverState.Brake = 0;
                            serverState.IndepBrake = 1;
                            serverState.Reverser = 0f;
                            if (serverState.Shunter != null)
                            {
                                serverState.Shunter.IsEngineOn = false;
                                serverState.Shunter.IsMainFuseOn = false;
                                serverState.Shunter.IsSideFuse1On = false;
                                serverState.Shunter.IsSideFuse2On = false;
                            }
                        }
                    }
                    SingletonBehaviour<CoroutineManager>.Instance.Run(RerailDesynced(train, data.Position, data.Forward));
                }
            }
        }
    }

    private void OnCarSyncAllMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Main.Log($"[CLIENT] < TRAIN_SYNC_ALL");
                serverCarStates = reader.ReadSerializables<WorldTrain>().ToList();
                SingletonBehaviour<CoroutineManager>.Instance.Run(SyncCarsFromServerState());
            }
        }
    }

    private void OnPlayerCarChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnPlayerCarChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCarChange changedCar = reader.ReadSerializable<TrainCarChange>();
                NetworkPlayerSync targetPlayerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerSyncById(changedCar.PlayerId);
                
                if (changedCar.TrainId == "")
                {
                    Main.Log($"[CLIENT] < TRAIN_SWITCH: Player left train");
                    targetPlayerSync.Train = null;
                }
                else
                {
                    TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == changedCar.TrainId);
                    if (train)
                    {
                        if (!train.GetComponent<NetworkTrainSync>() && train.IsLoco)
                            train.gameObject.AddComponent<NetworkTrainSync>();

                        if (!train.GetComponent<NetworkTrainPosSync>())
                            train.gameObject.AddComponent<NetworkTrainPosSync>();

                        if (!train.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                            train.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

                        if (!train.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                            train.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

                        if (!train.IsInteriorLoaded)
                        {
                            train.LoadInterior();
                            train.keepInteriorLoaded = true;
                        }
                        Main.Log($"[CLIENT] < TRAIN_SWITCH: Train found: {train}, ID: {train.ID}, GUID: {train.CarGUID}");
                        targetPlayerSync.Train = train;
                    }
                    else
                    {
                        Main.Log($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {changedCar.TrainId}");
                    }
                }
            }
        }
    }

    private void OnCarDerailmentMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarDerailmentMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainDerail data = reader.ReadSerializable<TrainDerail>();
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == data.TrainId);

                if (train)
                {
                    IsChangeByNetwork = true;
                    Main.Log($"[CLIENT] < TRAIN_DERAIL: Packet size: {reader.Length}, TrainId: {train.ID}");
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (serverState == null)
                    {
                        serverState = new WorldTrain()
                        {
                            Guid = train.CarGUID,
                        };
                        if (train.carType == TrainCarType.LocoShunter)
                            serverState.Shunter = new Shunter();
                        serverCarStates.Add(serverState);
                    }
                    serverState.Bogies[0] = new TrainBogie()
                    {
                        TrackName = data.Bogie1TrackName,
                        PositionAlongTrack = data.Bogie1PositionAlongTrack,
                        Derailed = data.IsBogie1Derailed
                    };
                    serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                    {
                        TrackName = data.Bogie2TrackName,
                        PositionAlongTrack = data.Bogie2PositionAlongTrack,
                        Derailed = data.IsBogie1Derailed
                    };
                    serverState.CarHealth = data.CarHealth;
                    if (!serverState.IsLoco)
                        serverState.CargoHealth = data.CargoHealth;

                    train.GetComponent<NetworkTrainPosSync>().isDerailed = true;
                    train.Derail();
                    SyncDamageWithServerState(train, serverState);
                    IsChangeByNetwork = false;
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {data.TrainId}");
                }
            }
        }
    }

    private void OnCarLocationMessage(Message message)
    {
        if (!IsSynced)
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLocation[] locations = reader.ReadSerializables<TrainLocation>();
                foreach(TrainLocation location in locations)
                {
                    TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == location.TrainId);
                    if (train)
                    {
                        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                        if (serverState == null)
                        {
                            serverState = new WorldTrain()
                            {
                                Guid = train.CarGUID,
                            };
                            if (train.carType == TrainCarType.LocoShunter)
                                serverState.Shunter = new Shunter();
                            serverCarStates.Add(serverState);
                        }

                        serverState.Position = location.Position;
                        serverState.Rotation = location.Rotation;
                        serverState.Forward = location.Forward;
                        serverState.Bogies = location.Bogies;
                        serverState.IsStationary = location.IsStationary;

                        //Main.Log($"[CLIENT] < TRAIN_LOCATION_UPDATE: TrainID: {train.ID}");
                        if(train.GetComponent<NetworkTrainPosSync>())
                            SingletonBehaviour<CoroutineManager>.Instance.Run(train.GetComponent<NetworkTrainPosSync>().UpdateLocation(location));
                    }
                }
            }
        }
    }

    private void OnLocoLeverMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnLocoLeverMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLever lever = reader.ReadSerializable<TrainLever>();

                TrainCar train = localCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == lever.TrainId);
                if (train && train.IsLoco)
                {
                    WorldTrain serverTrainState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (serverTrainState != null)
                    {
                        UpdateServerStateLeverChange(serverTrainState, lever.Lever, lever.Value);
                    }
                    Main.Log($"[CLIENT] < TRAIN_LEVER: Packet size: {reader.Length}, TrainID: {train.ID}, Lever: {lever.Lever}, Value: {lever.Value}");
                    IsChangeByNetwork = true;
                    LocoControllerBase baseController = train.GetComponent<LocoControllerBase>();
                    switch (lever.Lever)
                    {
                        case Levers.Throttle:
                            baseController.SetThrottle(lever.Value);
                            break;

                        case Levers.Brake:
                            baseController.SetBrake(lever.Value);
                            break;

                        case Levers.IndependentBrake:
                            baseController.SetIndependentBrake(lever.Value);
                            break;

                        case Levers.Reverser:
                            baseController.SetReverser(lever.Value);
                            break;

                        case Levers.Sander:
                            baseController.SetSanders(lever.Value);
                            break;

                        case Levers.SideFuse_1:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.SideFuse_2:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.MainFuse:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.FusePowerStarter:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.powerRotaryObj.GetComponent<RotaryBase>().SetValue(lever.Value);
                            }
                            break;

                        case Levers.Horn:
                            float valHorn = lever.Value;
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().hornObj.GetComponent<LeverBase>().SetValue(valHorn);
                                if (valHorn < 0.5)
                                    valHorn *= 2;
                                else
                                    valHorn = (valHorn - 0.5f) * 2;
                            }
                            baseController.UpdateHorn(valHorn);
                            break;
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarCoupleChangeMessage(Message message, bool isCoupled)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCoupleChangeMessage, message, isCoupled))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                TrainCouplingChange coupled = reader.ReadSerializable<TrainCouplingChange>();
                TrainCar trainCoupler1 = localCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC1);
                TrainCar trainCoupler2 = localCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC2);
                if (NetworkManager.IsHost())
                {
                    trainCoupler1.GetComponent<NetworkTrainPosSync>().resetAuthority = true;
                    if (!isCoupled)
                        trainCoupler2.GetComponent<NetworkTrainPosSync>().resetAuthority = true;
                }
                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }
                    if (train != null)
                    {
                        if (isCoupled)
                        {
                            if (coupled.IsC1Front)
                                train.IsFrontCouplerCoupled = true;
                            else
                                train.IsRearCouplerCoupled = true;
                        }
                        else
                        {
                            if (coupled.IsC1Front)
                                train.IsFrontCouplerCoupled = false;
                            else
                                train.IsRearCouplerCoupled = false;
                        }
                    }
                    train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        if (trainCoupler2.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }
                    if (train != null)
                    {
                        if (isCoupled)
                        {
                            if (coupled.IsC2Front)
                                train.IsFrontCouplerCoupled = true;
                            else
                                train.IsRearCouplerCoupled = true;
                        }
                        else
                        {
                            if (coupled.IsC2Front)
                                train.IsFrontCouplerCoupled = false;
                            else
                                train.IsRearCouplerCoupled = false;
                        }
                    }

                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {coupled.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {coupled.IsC2Front})");
                    Coupler C1 = coupled.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = coupled.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if (C1.GetFirstCouplerInRange() == C2 && isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.TryCouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if (C1.coupledTo == C2 && !isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.Uncouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if (C1.coupledTo != C2 && !isCoupled)
                    {
                        Main.Log($"[CLIENT] < TRAIN_COUPLE: Couplers were already uncoupled");
                    }
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {coupled.TrainIdC1}, TrainID_C2: {coupled.TrainIdC2}");
                }
            }
        }
    }

    private void OnCarCouplerCockChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCouplerCockChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerCockChange cockChange = reader.ReadSerializable<TrainCouplerCockChange>();
                TrainCar trainCoupler = localCars.FirstOrDefault(t => t.CarGUID == cockChange.TrainIdCoupler);

                if (trainCoupler)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == cockChange.TrainIdCoupler);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler.CarGUID,
                        };
                        if (trainCoupler.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }

                    if (cockChange.IsCouplerFront)
                        train.IsFrontCouplerHoseConnected = cockChange.IsOpen;
                    else
                        train.IsRearCouplerHoseConnected = cockChange.IsOpen;
                    IsChangeByNetwork = true;
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_COCK: Packet size: {reader.Length}, TrainID: {trainCoupler.ID} (isFront: {cockChange.IsCouplerFront}), isOpen: {cockChange.IsOpen}");
                    Coupler coupler = cockChange.IsCouplerFront ? trainCoupler.frontCoupler : trainCoupler.rearCoupler;
                    coupler.IsCockOpen = cockChange.IsOpen;
                    IsChangeByNetwork = false;
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_COCK: Trains not found, TrainID: {cockChange.TrainIdCoupler}, isOpen: {cockChange.IsOpen}");
                }
            }
        }
    }

    private void OnCarCouplerHoseChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCouplerHoseChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerHoseChange hoseChange = reader.ReadSerializable<TrainCouplerHoseChange>();
                TrainCar trainCoupler1 = localCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC1);
                TrainCar trainCoupler2 = null;
                if (hoseChange.IsConnected)
                    trainCoupler2 = localCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC2);

                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;
                    train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        if (trainCoupler2.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC2Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_HOSE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {hoseChange.IsC2Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = hoseChange.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if ((C1.IsCoupled() && C1.coupledTo == C2) || C1.GetFirstCouplerInRange() == C2)
                    {
                        IsChangeByNetwork = true;
                        C1.ConnectAirHose(C2, true);
                        IsChangeByNetwork = false;
                    }
                }
                else if (trainCoupler1 && !hoseChange.IsConnected)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;

                    Main.Log($"[CLIENT] < TRAIN_COUPLE_HOSE: TrainID: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    C1.DisconnectAirHose(true);
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {hoseChange.TrainIdC1}, TrainID_C2: {hoseChange.TrainIdC2}, IsConnected: {hoseChange.IsConnected}");
                }
            }
        }
    }

    private void OnAuthorityChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnAuthorityChangeMessage, message))
            return;
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                CarsAuthChange authChange = reader.ReadSerializable<CarsAuthChange>();
                Main.Log($"[CLIENT] < TRAIN_AUTH_CHANGE: Train: {authChange.Guids[0]}, PlayerId: {authChange.PlayerId}");
                foreach(string guid in authChange.Guids)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == guid);
                    if (train != null)
                    {
                        train.AuthorityPlayerId = authChange.PlayerId;
                    }
                }
            }
        }
    }
    #endregion

    #region Sending Messages
    private void SendNewTrainsInitializationFinished()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);
            Main.Log("[CLIENT] > TRAINS_INIT_FINISHED");
            using (Message message = Message.Create((ushort)NetworkTags.TRAINS_INIT_FINISHED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarDamaged(string carGUID, DamageType type, float amount)
    {
        if (!IsSynced)
            return;

        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == carGUID);
        UpdateServerStateDamage(serverState, type, amount);

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarDamage()
            {
                Guid = carGUID,
                DamageType = type,
                NewHealth = amount
            });
            Main.Log($"[CLIENT] > TRAIN_DAMAGE");
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_DAMAGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendCarBeingRemoved(TrainCar car)
    {
        if (!IsSynced)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarRemoval()
            {
                Guid = car.CarGUID
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_REMOVAL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendNewCarSpawned(TrainCar car)
    {
        SendNewCarsSpawned(new TrainCar[] { car });
    }

    private void SendNewCarsSpawned(IEnumerable<TrainCar> cars)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            foreach (TrainCar car in cars)
            {
                AddNetworkingScripts(car);
            }

            WorldTrain[] newServerTrains = GenerateServerCarsData(cars);
            serverCarStates.AddRange(newServerTrains);
            localCars.AddRange(cars);
            writer.Write(newServerTrains);
            Main.Log($"[CLIENT] > TRAINS_INIT: {newServerTrains.Length}");

            using (Message message = Message.Create((ushort)NetworkTags.TRAINS_INIT, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendInitializedCars()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            serverCarStates.Clear();
            Main.Log($"Host synching trains with server. Train amount: {localCars.Count}");
            serverCarStates.AddRange(GenerateServerCarsData(localCars));

            Main.Log($"[CLIENT] > TRAIN_HOSTSYNC: AmountOfTrains: {serverCarStates.Count}");
            writer.Write(serverCarStates.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        IsSynced = true;
    }

    internal void SendRerailCarUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            float cargoDmg = 0;
            if (!trainCar.IsLoco)
                cargoDmg = trainCar.CargoDamage.currentHealth;

            TrainRerail data = new TrainRerail()
            {
                Guid = trainCar.CarGUID,
                Position = trainCar.transform.position - WorldMover.currentMove,
                Forward = trainCar.transform.forward,
                Rotation = trainCar.transform.rotation,
                Bogie1TrackName = bogie1.track.name,
                Bogie2TrackName = bogie2.track.name,
                Bogie1PositionAlongTrack = bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                CarHealth = trainCar.CarDamage.currentHealth,
                CargoHealth = cargoDmg
            };

            IsChangeByNetwork = true;
            WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
            if (serverState != null)
            {
                serverState.Position = data.Position;
                serverState.Rotation = data.Rotation;
                serverState.Forward = data.Forward;
                serverState.Bogies[0] = new TrainBogie()
                {
                    TrackName = data.Bogie1TrackName,
                    PositionAlongTrack = data.Bogie1PositionAlongTrack,
                    Derailed = false
                };
                serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                {
                    TrackName = data.Bogie2TrackName,
                    PositionAlongTrack = data.Bogie2PositionAlongTrack,
                    Derailed = false
                };
                serverState.CarHealth = data.CarHealth;
                if (!serverState.IsLoco)
                    serverState.CargoHealth = data.CargoHealth;
                else
                {
                    serverState.Throttle = 0;
                    serverState.Sander = 0;
                    serverState.Brake = 0;
                    serverState.IndepBrake = 1;
                    serverState.Reverser = 0f;
                    if (serverState.Shunter != null)
                    {
                        serverState.Shunter.IsEngineOn = false;
                        serverState.Shunter.IsMainFuseOn = false;
                        serverState.Shunter.IsSideFuse1On = false;
                        serverState.Shunter.IsSideFuse2On = false;
                    }
                    SyncLocomotiveWithServerState(trainCar, serverState);
                }
            }
            IsChangeByNetwork = false;

            writer.Write(data);

            Main.Log($"[CLIENT] > TRAIN_RERAIL: ID: {trainCar.CarGUID}");

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_RERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendDerailCarUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_DERAIL: ID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            float cargoDmg = 0;
            if (!trainCar.IsLoco)
                cargoDmg = trainCar.CargoDamage.currentHealth;
            writer.Write(new TrainDerail()
            {
                TrainId = trainCar.CarGUID,
                IsBogie1Derailed = bogie1.HasDerailed,
                IsBogie2Derailed = bogie2.HasDerailed,
                Bogie1TrackName = bogie1.HasDerailed ? "" : bogie1.track.name,
                Bogie2TrackName = bogie2.HasDerailed ? "" : bogie2.track.name,
                Bogie1PositionAlongTrack = bogie1.HasDerailed ? 0 : bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.HasDerailed ? 0 : bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                CarHealth = trainCar.CarDamage.currentHealth,
                CargoHealth = cargoDmg
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_DERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarLocationUpdate(TrainCar trainCar, bool reliable = false)
    {
        if (!IsSynced)
            return;

        //Main.Log($"[CLIENT] > TRAIN_LOCATION_UPDATE: TrainID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<TrainLocation> locations = new List<TrainLocation>();
            foreach(TrainCar car in trainCar.trainset.cars)
            {
                List<TrainBogie> bogies = new List<TrainBogie>();
                foreach(Bogie bogie in car.Bogies)
                {
                    bogies.Add(new TrainBogie()
                    {
                        TrackName = bogie.HasDerailed ? "" : bogie.track.name,
                        Derailed = bogie.HasDerailed,
                        PositionAlongTrack = bogie.HasDerailed ? 0 : bogie.traveller.pointRelativeSpan + bogie.traveller.curPoint.span,
                        Position = bogie.transform.position,
                        Rotation = bogie.transform.rotation
                    });
                }

                locations.Add(new TrainLocation()
                {
                    TrainId = car.CarGUID,
                    Forward = car.transform.forward,
                    Position = car.transform.position - WorldMover.currentMove,
                    Rotation = car.transform.rotation,
                    Bogies = bogies.ToArray(),
                    IsStationary = car.isStationary,
                    Velocity = car.rb.velocity
                });
            }

            writer.Write(locations.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, reliable ? SendMode.Reliable : SendMode.Unreliable);
        }
    }

    internal void SendNewLocoLeverValue(Levers lever, float value)
    {
        if (!IsSynced)
            return;

        TrainCar curTrain = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Train;
        UpdateServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == curTrain.CarGUID), lever, value);
        Main.Log($"[CLIENT] > TRAIN_LEVER: TrainID: {curTrain.ID}, Lever: {lever}, value: {value}");
        if (!curTrain.IsLoco)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainLever>(new TrainLever()
            {
                TrainId = curTrain.CarGUID,
                Lever = lever,
                Value = value
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LEVER, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendPlayerCarChange(TrainCar train)
    {
        if (!IsSynced)
            return;

        if (train)
            Main.Log($"[CLIENT] > TRAIN_SWITCH: ID: {train.CarGUID}");
        else
            Main.Log($"[CLIENT] > TRAIN_SWITCH: Player left train");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            TrainCarChange val = null;
            if (train)
            {
                Bogie bogie1 = train.Bogies[0];
                Bogie bogie2 = train.Bogies[train.Bogies.Length - 1];
                val = new TrainCarChange()
                {
                    PlayerId = SingletonBehaviour<UnityClient>.Instance.ID,
                    TrainId = train.CarGUID
                };
            }
            else
            {
                val = new TrainCarChange()
                {
                    PlayerId = SingletonBehaviour<UnityClient>.Instance.ID,
                    TrainId = ""
                };
            }

            writer.Write<TrainCarChange>(val);

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SWITCH, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCoupledChange(Coupler thisCoupler, Coupler otherCoupler, bool viaChainInteraction, bool isCoupled)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE: Coupler_1: {thisCoupler.train.ID}, Coupler_2: {otherCoupler.train.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            if (NetworkManager.IsHost())
            {
                thisCoupler.train.GetComponent<NetworkTrainPosSync>().resetAuthority = true;
                if(!isCoupled)
                    otherCoupler.train.GetComponent<NetworkTrainPosSync>().resetAuthority = true;
            }

            writer.Write<TrainCouplingChange>(new TrainCouplingChange()
            {
                TrainIdC1 = thisCoupler.train.CarGUID,
                IsC1Front = thisCoupler.isFrontCoupler,
                TrainIdC2 = otherCoupler.train.CarGUID,
                IsC2Front = otherCoupler.isFrontCoupler,
                ViaChainInteraction = viaChainInteraction
            });

            using (Message message = Message.Create(isCoupled ? (ushort)NetworkTags.TRAIN_COUPLE : (ushort)NetworkTags.TRAIN_UNCOUPLE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendInitCarsRequest()
    {
        IsSynced = false;
        Main.Log($"[CLIENT] > TRAIN_SYNC_ALL");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SYNC_ALL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCouplerCockChanged(Coupler coupler, bool isCockOpen)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE_COCK: Coupler: {coupler.train.ID}, isOpen: {isCockOpen}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainCouplerCockChange>(new TrainCouplerCockChange()
            {
                TrainIdCoupler = coupler.train.CarGUID,
                IsCouplerFront = coupler.isFrontCoupler,
                IsOpen = isCockOpen
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_COUPLE_COCK, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCouplerHoseConChanged(Coupler coupler, bool isConnected)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE_HOSE: Coupler: {coupler.train.ID}, IsConnected: {isConnected}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Coupler C2 = null;
            if (isConnected)
                C2 = coupler.GetAirHoseConnectedTo();

            writer.Write<TrainCouplerHoseChange>(new TrainCouplerHoseChange()
            {
                TrainIdC1 = coupler.train.CarGUID,
                IsC1Front = coupler.isFrontCoupler,
                TrainIdC2 = C2 != null ? C2.train.CarGUID : "",
                IsC2Front = C2 != null && C2.isFrontCoupler,
                IsConnected = isConnected
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_COUPLE_HOSE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendNewJobChainCars(List<TrainCar> trainCarsForJobChain)
    {
        
        SendNewCarsSpawned(trainCarsForJobChain);
        AppUtil.Instance.PauseGame();
        CustomUI.OpenPopup("Streaming", "New Area being loaded");
    }

    internal void SendAuthorityChange(Trainset set, ushort id)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_AUTH_CHANGE: Train: {set.firstCar.CarGUID}, PlayerId: {id}");

        string[] carGuids = new string[set.cars.Count];
        for(int i = 0; i < set.cars.Count; i++)
        {
            WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == set.cars[i].CarGUID);
            train.AuthorityPlayerId = id;
            carGuids[i] = set.cars[i].CarGUID;
        }

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarsAuthChange() { Guids = carGuids, PlayerId = id });
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_AUTH_CHANGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion

    #region Car Functions
    internal Vector3 CalculateWorldPosition(Vector3 position, Vector3 forward, float zBounds)
    {
        return position + forward * zBounds;
    }

    private void ResyncCoupling(TrainCar train, WorldTrain serverState)
    {
        if (serverState.IsFrontCouplerCoupled.HasValue && serverState.IsFrontCouplerCoupled.Value && !train.frontCoupler.coupledTo)
        {
            if (serverState.IsFrontCouplerCockOpen.HasValue && serverState.IsFrontCouplerHoseConnected.HasValue)
            {
                if (serverState.IsFrontCouplerCockOpen.Value && serverState.IsFrontCouplerHoseConnected.Value)
                    train.frontCoupler.TryCouple(false);
                else
                    train.frontCoupler.TryCouple(false, true);
            }
        }
        else
        {
            if (serverState.IsFrontCouplerCockOpen.HasValue && serverState.IsFrontCouplerCockOpen.Value && !train.frontCoupler.IsCockOpen)
                train.frontCoupler.IsCockOpen = true;

            if (serverState.IsFrontCouplerHoseConnected.HasValue && serverState.IsFrontCouplerHoseConnected.Value && !train.frontCoupler.GetAirHoseConnectedTo() && train.frontCoupler.GetFirstCouplerInRange())
                train.frontCoupler.ConnectAirHose(train.frontCoupler.GetFirstCouplerInRange(), false);
        }

        if (serverState.IsRearCouplerCoupled.HasValue && serverState.IsRearCouplerCoupled.Value && !train.rearCoupler.coupledTo)
        {
            if (serverState.IsRearCouplerCockOpen.HasValue && serverState.IsRearCouplerHoseConnected.HasValue)
            {
                if (serverState.IsRearCouplerCockOpen.Value && serverState.IsRearCouplerHoseConnected.Value)
                    train.rearCoupler.TryCouple(false);
                else
                    train.rearCoupler.TryCouple(false, true);
            }
        }
        else
        {
            if (serverState.IsRearCouplerCockOpen.HasValue && serverState.IsRearCouplerCockOpen.Value && !train.rearCoupler.IsCockOpen)
                train.rearCoupler.IsCockOpen = true;

            if (serverState.IsRearCouplerHoseConnected.HasValue && serverState.IsRearCouplerHoseConnected.Value && !train.rearCoupler.GetAirHoseConnectedTo() && train.rearCoupler.GetFirstCouplerInRange())
                train.rearCoupler.ConnectAirHose(train.rearCoupler.GetFirstCouplerInRange(), false);
        }
    }

    private IEnumerator FullResyncCar(TrainCar train, WorldTrain serverState)
    {
        Main.Log($"Train load interior");
        train.LoadInterior();
        train.keepInteriorLoaded = true;
        Main.Log($"Train interior should stay loaded: {train.keepInteriorLoaded}");

        Main.Log($"Train set derailed");
        bool isDerailed = train.derailed;
        Main.Log($"Train is derailed: {isDerailed}");
        if (train.Bogies != null && train.Bogies.Length >= 2 && serverState.Position != Vector3.zero)
        {
            Main.Log($"Train Bogies synching");
            Bogie bogie1 = train.Bogies[0];
            Bogie bogie2 = train.Bogies[train.Bogies.Length - 1];
            Main.Log($"Train bogies are set {bogie1 != null && bogie2 != null}");

            isDerailed = serverState.Bogies[0].Derailed || serverState.Bogies[serverState.Bogies.Length - 1].Derailed;
            Main.Log($"Train is derailed by bogies {isDerailed}");
            if (serverState.Bogies[0].Derailed && !bogie1.HasDerailed)
            {
                bogie1.Derail();
            }

            if (serverState.Bogies[serverState.Bogies.Length - 1].Derailed && !bogie2.HasDerailed)
            {
                bogie2.Derail();
            }
            Main.Log($"Train bogies synced");

            if (bogie1.HasDerailed || bogie2.HasDerailed)
            {
                Main.Log("Teleport train to derailed position");
                train.transform.position = serverState.Position + WorldMover.currentMove;
                train.transform.rotation = serverState.Rotation;
                Main.Log("Stop syncing rest of train since values will be reset at rerail");
                yield break;
            }
        }

        Main.Log($"Train repositioning sync: Pos: {serverState.Position.ToString("G3")}");
        if (serverState.Position != Vector3.zero && !isDerailed && train.derailed)
            yield return RerailDesynced(train, serverState.Position, serverState.Forward);

        SyncDamageWithServerState(train, serverState);
        SyncLocomotiveWithServerState(train, serverState);

        Main.Log($"Train should be synced");
    }

    private IEnumerator SyncCarsFromServerState()
    {
        Main.Log($"Synching trains. Train amount: {serverCarStates.Count}");
        foreach (WorldTrain selectedTrain in serverCarStates)
        {
            IsChangeByNetwork = true;
            Main.Log($"Synching train: {selectedTrain.Guid}.");

            TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);
            if (train == null)
            {
                train = InitializeNewTrainCar(selectedTrain);
                yield return new WaitUntil(() => train.AreBogiesFullyInitialized() && train.frontCoupler && train.rearCoupler);
                yield return RerailDesynced(train, selectedTrain.Position, selectedTrain.Forward);
            }

            if (train != null)
            {
                try
                {
                    if (train.frontCoupler.IsCoupled())
                        train.frontCoupler.Uncouple(false);
                    if (train.rearCoupler.IsCoupled())
                        train.rearCoupler.Uncouple(false);
                }
                catch (Exception) { }
                yield return FullResyncCar(train, selectedTrain);
            }

            train.rb.isKinematic = true;
            IsChangeByNetwork = false;
        }

        foreach (WorldTrain selectedTrain in serverCarStates.Where(t => (t.IsFrontCouplerCoupled.HasValue && t.IsFrontCouplerCoupled.Value) || (t.IsRearCouplerCoupled.HasValue && t.IsRearCouplerCoupled.Value)))
        {
            IsChangeByNetwork = true;
            Main.Log($"Synching train: {selectedTrain.Guid}.");

            TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);

            if (train)
                try
                {
                    ResyncCoupling(train, selectedTrain);
                }
                catch (Exception) { }
            IsChangeByNetwork = false;
        }
        IsSynced = true;
    }

    internal void RunBuffer()
    {
        buffer.RunBuffer();
    }

    private void SyncLocomotiveWithServerState(TrainCar train, WorldTrain serverState)
    {
        if (!train.IsLoco)
            return;

        Main.Log($"Train Loco generic sync");
        LocoControllerBase controller = train.GetComponent<LocoControllerBase>();
        Main.Log($"Train Loco controller found {controller != null}");
        if (controller != null)
        {
            controller.SetBrake(serverState.Brake);
            controller.SetIndependentBrake(serverState.IndepBrake);
            controller.SetSanders(serverState.Sander);
            controller.SetReverser(serverState.Reverser);
            controller.SetThrottle(serverState.Throttle);
        }

        Main.Log($"Train Loco specific sync");
        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                Main.Log($"Train Loco is shunter");
                LocoControllerShunter controllerShunter = train.GetComponent<LocoControllerShunter>();
                Main.Log($"Train controller found {controllerShunter != null}");
                Shunter shunter = serverState.Shunter;
                Main.Log($"Train Loco Server data found {shunter != null}");
                if (shunter != null)
                {
                    ShunterDashboardControls shunterDashboard = train.interior.GetComponentInChildren<ShunterDashboardControls>();
                    Main.Log($"Shunter dashboard found {shunterDashboard != null}");
                    Main.Log($"Sync engine state");
                    if (!shunter.IsEngineOn)
                    {
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().SetValue(shunter.IsSideFuse1On ? 1 : 0);
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().SetValue(shunter.IsSideFuse2On ? 1 : 0);
                        shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().SetValue(shunter.IsMainFuseOn ? 1 : 0);
                    }
                    controllerShunter.SetEngineRunning(shunter.IsEngineOn);
                }
                else
                {
                    serverState.Shunter = new Shunter();
                }
                break;
        }
    }

    private TrainCar InitializeNewTrainCar(WorldTrain serverState)
    {
        GameObject carPrefab = CarTypes.GetCarPrefab(serverState.CarType);
        TrainCar newTrain;
        TrainBogie bogie1 = serverState.Bogies[0];
        TrainBogie bogie2 = serverState.Bogies[serverState.Bogies.Length - 1];
        newTrain = CarSpawner.SpawnLoadedCar(carPrefab, serverState.Id, serverState.Guid, serverState.IsPlayerSpawned, serverState.Position + WorldMover.currentMove, serverState.Rotation,
        bogie1.Derailed, RailTrackRegistry.GetTrackWithName(bogie1.TrackName), bogie1.PositionAlongTrack,
        bogie2.Derailed, RailTrackRegistry.GetTrackWithName(bogie2.TrackName), bogie2.PositionAlongTrack,
        false, false);
        

        newTrain.LoadInterior();
        newTrain.keepInteriorLoaded = true;

        NetworkTrainPosSync posSyncer = newTrain.gameObject.AddComponent<NetworkTrainPosSync>();
        if (newTrain.IsLoco)
            newTrain.gameObject.AddComponent<NetworkTrainSync>();

        newTrain.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        newTrain.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

        if(newTrain.logicCar != null && !newTrain.IsLoco && serverState.CargoType != CargoType.None)
            newTrain.logicCar.LoadCargo(serverState.CargoAmount, serverState.CargoType);
        else if(newTrain.logicCar != null)
            posSyncer.OnTrainCarInitialized += NetworkTrainManager_OnTrainCarInitialized;

        SingletonBehaviour<CoroutineManager>.Instance.Run(RerailDesynced(newTrain, serverState, true));
        localCars.Add(newTrain);

        return newTrain;
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, WorldTrain train, bool resyncCoupling)
    {
        yield return RerailDesynced(trainCar, train.Position, train.Forward);
        if (resyncCoupling)
            try
            {
                ResyncCoupling(trainCar, train);
            }
            catch (Exception) { }
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        IsChangeByNetwork = true;
        trainCar.rb.isKinematic = false;
        RailTrack track = null;
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
        if (serverState != null && serverState.AuthorityPlayerId != SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id)
            track = RailTrackRegistry.GetTrackWithName(serverState.Bogies[0].TrackName);
        else
            track = RailTrack.GetClosest(pos + WorldMover.currentMove).track;

        if (track)
        {
            trainCar.Rerail(track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
            yield return new WaitUntil(() => !trainCar.derailed);
            if (serverState != null)
            {
                SyncDamageWithServerState(trainCar, serverState);
                SyncLocomotiveWithServerState(trainCar, serverState);
            }
        }
        if(serverState != null && serverState.AuthorityPlayerId != SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id)
            trainCar.rb.isKinematic = true;
        IsChangeByNetwork = false;
    }

    private void SyncDamageWithServerState(TrainCar trainCar, WorldTrain serverState)
    {
        trainCar.CarDamage.LoadCarDamageState(serverState.CarHealth);
        if (!trainCar.IsLoco)
        {
            trainCar.CargoDamage.LoadCargoDamageState(serverState.CargoHealth);
        }
    }

    internal WorldTrain[] GenerateServerCarsData(IEnumerable<TrainCar> cars)
    {
        List<WorldTrain> data = new List<WorldTrain>();
        foreach(TrainCar car in cars)
        {
            Main.Log($"Get train bogies");
            Bogie bogie1 = car.Bogies[0];
            Bogie bogie2 = car.Bogies[car.Bogies.Length - 1];
            Main.Log($"Train bogies found: {bogie1 != null && bogie2 != null}");

            Main.Log($"Set train defaults");
            List<TrainBogie> bogies = new List<TrainBogie>();
            foreach (Bogie bogie in car.Bogies)
            {
                bogies.Add(new TrainBogie()
                {
                    TrackName = bogie.track.name,
                    Derailed = bogie.HasDerailed,
                    PositionAlongTrack = bogie.HasDerailed ? 0 : bogie.traveller.pointRelativeSpan + bogie.traveller.curPoint.span
                });
            }

            WorldTrain train = new WorldTrain()
            {
                Guid = car.CarGUID,
                Id = car.ID,
                CarType = car.carType,
                IsLoco = car.IsLoco,
                Position = car.transform.position - WorldMover.currentMove,
                Rotation = car.transform.rotation,
                Forward = car.transform.forward,
                Bogies = bogies.ToArray(),
                IsFrontCouplerCoupled = car.frontCoupler.coupledTo,
                IsFrontCouplerCockOpen = car.frontCoupler.IsCockOpen,
                IsFrontCouplerHoseConnected = car.frontCoupler.GetAirHoseConnectedTo() != null,
                IsRearCouplerCoupled = car.rearCoupler.coupledTo,
                IsRearCouplerCockOpen = car.rearCoupler.IsCockOpen,
                IsRearCouplerHoseConnected = car.rearCoupler.GetAirHoseConnectedTo() != null,
                IsPlayerSpawned = car.playerSpawnedCar,
                IsRemoved = false,
                IsStationary = true,
                CarHealth = car.CarDamage.currentHealth
            };

            if (car.IsLoco && car.carType != TrainCarType.HandCar)
            {
                Main.Log($"Set locomotive defaults");
                LocoControllerBase loco = car.GetComponent<LocoControllerBase>();
                Main.Log($"Loco controller found: {loco != null}");
                train.Throttle = loco.throttle;
                Main.Log($"Throttle set: {train.Throttle}");
                train.Brake = loco.brake;
                Main.Log($"Brake set: {train.Brake}");
                train.IndepBrake = loco.independentBrake;
                Main.Log($"IndepBrake set: {train.IndepBrake}");
                train.Reverser = loco.reverser;
                Main.Log($"Reverser set: {train.Reverser}");
                train.Sander = loco.IsSandOn() ? 1 : 0;
                Main.Log($"Sander set: {train.Sander}");
            }
            else if(car.carType != TrainCarType.HandCar)
            {
                train.CargoType = car.LoadedCargo;
                train.CargoAmount = car.LoadedCargoAmount;
                train.CargoHealth = car.CargoDamage.currentHealth;
            }

            switch (car.carType)
            {
                case TrainCarType.LocoShunter:
                    Main.Log($"Set shunter defaults");
                    LocoControllerShunter loco = car.GetComponent<LocoControllerShunter>();
                    Main.Log($"Shunter controller found: {loco != null}");
                    ShunterDashboardControls dashboard = car.interior.GetComponentInChildren<ShunterDashboardControls>();
                    Main.Log($"Shunter dashboard found: {dashboard != null}");
                    train.Shunter = new Shunter()
                    {
                        IsEngineOn = loco.GetEngineRunning(),
                        IsMainFuseOn = dashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1,
                        IsSideFuse1On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1,
                        IsSideFuse2On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1
                    };
                    Main.Log($"Shunter set: IsEngineOn: {train.Shunter.IsEngineOn}, IsMainFuseOn: {train.Shunter.IsMainFuseOn}, IsSideFuse1On: {train.Shunter.IsSideFuse1On}, IsSideFuse2On: {train.Shunter.IsSideFuse2On}");
                    break;
            }

            data.Add(train);
        }
        return data.ToArray();
    }

    internal WorldTrain GetServerStateById(string guid)
    {
        return serverCarStates.FirstOrDefault(t => t.Guid == guid);
    }

    private IEnumerator SpawnSendedTrains(WorldTrain[] trains)
    {
        AppUtil.Instance.PauseGame();
        CustomUI.OpenPopup("Streaming", "New Area being loaded");
        yield return new WaitUntil(() => SingletonBehaviour<CanvasSpawner>.Instance.IsOpen);
        yield return new WaitForFixedUpdate();
        foreach (WorldTrain train in trains)
        {
            IsSpawningTrains = true;
            Main.Log($"Initializing: {train.Guid} in area");
            serverCarStates.Add(train);
            TrainCar car = InitializeNewTrainCar(train);
            yield return RerailDesynced(car, train, true);
            Main.Log($"Initializing: {train.Guid} in area [DONE]");
        }
        yield return new WaitUntil(() =>
        {
            foreach (WorldTrain train in trains)
            {
                if (localCars.Any(t => t.logicCar == null))
                    return false;

                if (!localCars.Any(t => t.CarGUID == train.Guid && t.AreBogiesFullyInitialized()))
                    return false;
            }
            return true;
        });
        yield return SingletonBehaviour<FpsStabilityMeasurer>.Instance.WaitForStableFps();
        SendNewTrainsInitializationFinished();
    }

    private void UpdateServerStateLeverChange(WorldTrain serverState, Levers lever, float value)
    {
        switch (lever)
        {
            case Levers.Throttle:
                serverState.Throttle = value;
                break;

            case Levers.Brake:
                serverState.Brake = value;
                break;

            case Levers.IndependentBrake:
                serverState.IndepBrake = value;
                break;

            case Levers.Reverser:
                serverState.Reverser = value;
                break;

            case Levers.Sander:
                serverState.Sander = value;
                break;

            case Levers.SideFuse_1:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsSideFuse1On = value == 1;
                    if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                break;

            case Levers.SideFuse_2:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsSideFuse2On = value == 1;
                    if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                break;

            case Levers.MainFuse:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsMainFuseOn = value == 1;
                    if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                break;

            case Levers.FusePowerStarter:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    if (serverState.Shunter.IsSideFuse1On && serverState.Shunter.IsSideFuse2On && serverState.Shunter.IsMainFuseOn && value == 1)
                        serverState.Shunter.IsEngineOn = true;
                    else if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                break;
        }
    }

    private void UpdateServerStateDamage(WorldTrain serverState, DamageType type, float value)
    {
        switch (type)
        {
            case DamageType.Car:
                serverState.CargoHealth = value;
                break;

            case DamageType.Cargo:
                serverState.CargoHealth = value;
                break;
        }

        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                serverState.Shunter.IsEngineOn = false;
                break;
        }
    }

    internal void ResyncCar(TrainCar trainCar)
    {
        IsChangeByNetwork = true;
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
        if (serverState != null)
        {
            SyncDamageWithServerState(trainCar, serverState);
            SyncLocomotiveWithServerState(trainCar, serverState);
        }
        IsChangeByNetwork = false;
    }

    private void AddNetworkingScripts(TrainCar car)
    {
        car.LoadInterior();
        car.keepInteriorLoaded = true;

        if (!car.GetComponent<NetworkTrainSync>() && car.IsLoco)
            car.gameObject.AddComponent<NetworkTrainSync>();

        if (!car.GetComponent<NetworkTrainPosSync>())
            car.gameObject.AddComponent<NetworkTrainPosSync>();

        if (!car.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
            car.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

        if (!car.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
            car.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
    }
    #endregion
}
