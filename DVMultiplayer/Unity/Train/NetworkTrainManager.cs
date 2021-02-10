using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.PointSet;
using UnityEngine;
using System;
using DV.TerrainSystem;
using DVMultiplayer.Darkrift;

class NetworkTrainManager : SingletonBehaviour<NetworkTrainManager>
{
    public List<TrainCar> trainCars = new List<TrainCar>();
    public List<WorldTrain> serverTrainStates = new List<WorldTrain>();
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; private set; }
    public bool SaveTrainCarsLoaded { get; internal set; }
    private BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        base.Awake();
        IsChangeByNetwork = false;
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += OnMessageReceived;
    }

    internal Vector3 CalculateWorldPosition(Vector3 position, Vector3 forward, float zBounds)
    {
        return position + forward * zBounds;
    }

    public void OnFinishedLoading()
    {
        SaveTrainCarsLoaded = false;
        trainCars = GameObject.FindObjectsOfType<TrainCar>().ToList();
        Main.DebugLog($"{trainCars.Count} traincars found, {trainCars.Where(car => car.IsLoco).Count()} are locomotives");

        foreach (TrainCar trainCar in trainCars)
        {
            Main.DebugLog($"Initializing TrainCar Coupling scripts");
            trainCar.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
            trainCar.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
            Main.DebugLog($"Initializing TrainCar Positioning script");
            trainCar.gameObject.AddComponent<NetworkTrainPosSync>();

            if (trainCar.IsLoco)
            {
                Main.DebugLog($"Initializing TrainCar input script");
                trainCar.gameObject.AddComponent<NetworkTrainSync>();
            }
        }

        Main.DebugLog($"Listening to CarChanged event");
        PlayerManager.CarChanged += OnPlayerSwitchTrainCarEvent;
        CarSpawner.CarSpawned += OnCarSpawned;
        if (NetworkManager.IsHost())
        {
            IsSynced = true;
            //SyncInitializedTrains();
        }
        SaveTrainCarsLoaded = true;
    }

    private void OnCarSpawned(TrainCar car)
    {
        if (IsChangeByNetwork)
            return;
        if(SingletonBehaviour<NetworkPlayerManager>.Instance.IsAnyoneInLocalPlayerRegion())
        {
            CarSpawner.DeleteCar(car);
        }
        else
        {
            SendNewTrainSpawned(car);
        }
    }

    private void SendNewTrainSpawned(TrainCar car)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Main.DebugLog($"Get train bogies");
            Bogie bogie1 = car.Bogies[0];
            Bogie bogie2 = car.Bogies[car.Bogies.Length - 1];
            Main.DebugLog($"Train bogies found: {bogie1 != null && bogie2 != null}");

            Main.DebugLog($"Set train defaults");
            WorldTrain train = new WorldTrain()
            {
                Guid = car.CarGUID,
                Id = car.ID,
                CarType = car.carType,
                IsLoco = car.IsLoco,
                Position = car.transform.position - WorldMover.currentMove,
                Rotation = car.transform.rotation,
                Velocity = car.rb.velocity,
                AngularVelocity = car.rb.angularVelocity,
                Forward = car.transform.forward,
                IsBogie1Derailed = bogie1.HasDerailed,
                IsBogie2Derailed = bogie2.HasDerailed,
                Bogie1PositionAlongTrack = bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                Bogie1RailTrackName = bogie1.track.name,
                Bogie2RailTrackName = bogie2.track.name,
                IsFrontCouplerCoupled = car.frontCoupler.coupledTo,
                IsFrontCouplerCockOpen = car.frontCoupler.IsCockOpen,
                IsFrontCouplerHoseConnected = car.frontCoupler.GetAirHoseConnectedTo(),
                IsRearCouplerCoupled = car.rearCoupler.coupledTo,
                IsRearCouplerCockOpen = car.rearCoupler.IsCockOpen,
                IsRearCouplerHoseConnected = car.rearCoupler.GetAirHoseConnectedTo(),
                IsPlayerSpawned = car.playerSpawnedCar
            };

            if (car.IsLoco)
            {
                Main.DebugLog($"Set locomotive defaults");
                LocoControllerBase loco = car.GetComponent<LocoControllerBase>();
                Main.DebugLog($"Loco controller found: {loco != null}");
                train.Throttle = loco.throttle;
                Main.DebugLog($"Throttle set: {train.Throttle}");
                train.Brake = loco.brake;
                Main.DebugLog($"Brake set: {train.Brake}");
                train.IndepBrake = loco.independentBrake;
                Main.DebugLog($"IndepBrake set: {train.IndepBrake}");
                train.Reverser = loco.reverser;
                Main.DebugLog($"Reverser set: {train.Reverser}");
                train.Sander = loco.IsSandOn() ? 1 : 0;
                Main.DebugLog($"Sander set: {train.Sander}");
            }

            switch (car.carType)
            {
                case TrainCarType.LocoShunter:
                    Main.DebugLog($"Set shunter defaults");
                    LocoControllerShunter loco = car.GetComponent<LocoControllerShunter>();
                    Main.DebugLog($"Shunter controller found: {loco != null}");
                    ShunterDashboardControls dashboard = car.interior.GetComponentInChildren<ShunterDashboardControls>();
                    Main.DebugLog($"Shunter dashboard found: {dashboard != null}");
                    train.Shunter = new Shunter()
                    {
                        IsEngineOn = loco.GetEngineRunning(),
                        IsMainFuseOn = dashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1,
                        IsSideFuse1On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1,
                        IsSideFuse2On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1
                    };
                    Main.DebugLog($"Shunter set: IsEngineOn: {train.Shunter.IsEngineOn}, IsMainFuseOn: {train.Shunter.IsMainFuseOn}, IsSideFuse1On: {train.Shunter.IsSideFuse1On}, IsSideFuse2On: {train.Shunter.IsSideFuse2On}");
                    break;
            }

            writer.Write(train);
            Main.DebugLog($"[CLIENT] > TRAIN_INIT: {car.CarGUID}");

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_INIT, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
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

    private void SyncInitializedTrains()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<WorldTrain> trains = new List<WorldTrain>();
            Main.DebugLog($"Host synching trains with server. Train amount: {trainCars.Count}");
            foreach (TrainCar car in trainCars)
            {
                Main.DebugLog($"Load train interior if not loaded");
                car.LoadInterior();
                Main.DebugLog($"Keep train interior loaded");
                car.keepInteriorLoaded = true;

                Main.DebugLog($"Get train bogies");
                Bogie bogie1 = car.Bogies[0];
                Bogie bogie2 = car.Bogies[car.Bogies.Length - 1];
                Main.DebugLog($"Train bogies found: {bogie1 != null && bogie2 != null}");

                Main.DebugLog($"Set train defaults");
                WorldTrain train = new WorldTrain()
                {
                    Guid = car.CarGUID,
                    Id = car.ID,
                    CarType = car.carType,
                    IsLoco = car.IsLoco,
                    Position = car.transform.position - WorldMover.currentMove,
                    Rotation = car.transform.rotation,
                    Velocity = car.rb.velocity,
                    AngularVelocity = car.rb.angularVelocity,
                    Forward = car.transform.forward,
                    IsBogie1Derailed = bogie1.HasDerailed,
                    IsBogie2Derailed = bogie2.HasDerailed,
                    Bogie1PositionAlongTrack = bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                    Bogie2PositionAlongTrack = bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                    Bogie1RailTrackName = bogie1.track.name,
                    Bogie2RailTrackName = bogie2.track.name,
                    IsFrontCouplerCoupled = car.frontCoupler.coupledTo,
                    IsFrontCouplerCockOpen = car.frontCoupler.IsCockOpen,
                    IsFrontCouplerHoseConnected = car.frontCoupler.GetAirHoseConnectedTo(),
                    IsRearCouplerCoupled = car.rearCoupler.coupledTo,
                    IsRearCouplerCockOpen = car.rearCoupler.IsCockOpen,
                    IsRearCouplerHoseConnected = car.rearCoupler.GetAirHoseConnectedTo(),
                    IsPlayerSpawned = car.playerSpawnedCar
                };

                if (car.IsLoco)
                {
                    Main.DebugLog($"Set locomotive defaults");
                    LocoControllerBase loco = car.GetComponent<LocoControllerBase>();
                    Main.DebugLog($"Loco controller found: {loco != null}");
                    train.Throttle = loco.throttle;
                    Main.DebugLog($"Throttle set: {train.Throttle}");
                    train.Brake = loco.brake;
                    Main.DebugLog($"Brake set: {train.Brake}");
                    train.IndepBrake = loco.independentBrake;
                    Main.DebugLog($"IndepBrake set: {train.IndepBrake}");
                    train.Reverser = loco.reverser;
                    Main.DebugLog($"Reverser set: {train.Reverser}");
                    train.Sander = loco.IsSandOn() ? 1 : 0;
                    Main.DebugLog($"Sander set: {train.Sander}");
                }
                
                switch (car.carType)
                {
                    case TrainCarType.LocoShunter:
                        Main.DebugLog($"Set shunter defaults");
                        LocoControllerShunter loco = car.GetComponent<LocoControllerShunter>();
                        Main.DebugLog($"Shunter controller found: {loco != null}");
                        ShunterDashboardControls dashboard = car.interior.GetComponentInChildren<ShunterDashboardControls>();
                        Main.DebugLog($"Shunter dashboard found: {dashboard != null}");
                        train.Shunter = new Shunter()
                        {
                            IsEngineOn = loco.GetEngineRunning(),
                            IsMainFuseOn = dashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse1On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse2On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1
                        };
                        Main.DebugLog($"Shunter set: IsEngineOn: {train.Shunter.IsEngineOn}, IsMainFuseOn: {train.Shunter.IsMainFuseOn}, IsSideFuse1On: {train.Shunter.IsSideFuse1On}, IsSideFuse2On: {train.Shunter.IsSideFuse2On}");
                        break;
                }
                Main.DebugLog($"Add train to sync pile");
                trains.Add(train);
            }

            Main.mod.Logger.Log($"[CLIENT] > TRAIN_HOSTSYNC: AmountOfTrains: {trains.Count}");
            writer.Write(trains.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_HOSTSYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        IsSynced = true;
    }

    internal void SendRerailTrainUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new TrainRerail()
            {
                TrainId = trainCar.CarGUID,
                Position = trainCar.transform.position - WorldMover.currentMove,
                Forward = trainCar.transform.forward,
                Rotation = trainCar.transform.rotation,
                TrackName = trainCar.Bogies[0].track.name
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_RERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    public void PlayerDisconnect()
    {
        SingletonBehaviour<UnityClient>.Instance.MessageReceived -= OnMessageReceived;
        if (trainCars == null)
            return;

        foreach (TrainCar trainCar in trainCars)
        {
            if(trainCar.GetComponent<NetworkTrainPosSync>())
                DestroyImmediate(trainCar.GetComponent<NetworkTrainPosSync>());
            if(trainCar.IsLoco && trainCar.GetComponent<NetworkTrainSync>())
                DestroyImmediate(trainCar.GetComponent<NetworkTrainSync>());
        }
    }

    private void OnPlayerSwitchTrainCarEvent(TrainCar trainCar)
    {
        if (trainCar)
        {
            if (!trainCar.GetComponent<NetworkTrainSync>() && trainCar.IsLoco)
                trainCar.gameObject.AddComponent<NetworkTrainSync>();

            if (!trainCar.GetComponent<NetworkTrainPosSync>())
                trainCar.gameObject.AddComponent<NetworkTrainPosSync>();

            if(!trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                trainCar.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

            if(!trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                trainCar.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        }

        NetworkPlayerSync playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
        if (playerSync.Train && playerSync.Train.IsLoco)
            playerSync.Train.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = false;

        playerSync.Train = trainCar;
        SendPlayerTrainCarChange(trainCar);

        if(trainCar && trainCar.IsLoco)
            trainCar.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = true;
    }

    #region Messaging

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags) message.Tag)
            {
                case NetworkTags.TRAIN_LEVER:
                    OnTrainLeverMessage(message);
                    break;

                case NetworkTags.TRAIN_LOCATION_UPDATE:
                    OnTrainLocationMessage(message);
                    break;

                case NetworkTags.TRAIN_SWITCH:
                    OnPlayerTrainCarChange(message);
                    break;

                case NetworkTags.TRAIN_DERAIL:
                    OnTrainDerailment(message);
                    break;

                case NetworkTags.TRAIN_COUPLE:
                    OnTrainCoupleChange(message, true);
                    break;

                case NetworkTags.TRAIN_UNCOUPLE:
                    OnTrainCoupleChange(message, false);
                    break;

                case NetworkTags.TRAIN_COUPLE_HOSE:
                    OnTrainCouplerHoseChange(message);
                    break;

                case NetworkTags.TRAIN_COUPLE_COCK:
                    OnTrainCouplerCockChange(message);
                    break;

                case NetworkTags.TRAIN_SYNC_ALL:
                    SingletonBehaviour<CoroutineManager>.Instance.Run(OnTrainSyncAll(message));
                    break;

                case NetworkTags.TRAIN_RERAIL:
                    OnTrainRerail(message);
                    break;

                case NetworkTags.TRAIN_INIT:
                    OnTrainInit(message);
                    break;
            }
        }
    }

    private void OnTrainInit(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainInit, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                WorldTrain train = reader.ReadSerializable<WorldTrain>();
                Main.DebugLog($"[CLIENT] < TRAIN_INIT: {train.Guid}");
                serverTrainStates.Add(train);
                InitializeNewTrainCar(train);
                IsChangeByNetwork = false;
            }
        }
    }

    private void OnTrainRerail(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainRerail, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainRerail rerail = reader.ReadSerializable<TrainRerail>();
                TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == rerail.TrainId);
                if (train)
                {
                    train.Rerail(RailTrack.GetClosest(rerail.Position + WorldMover.currentMove).track, CalculateWorldPosition(rerail.Position + WorldMover.currentMove, rerail.Forward, train.Bounds.center.z), rerail.Forward);
                }
            }
        }
    }

    private IEnumerator OnTrainSyncAll(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                serverTrainStates = reader.ReadSerializables<WorldTrain>().ToList();
                Main.DebugLog($"Synching trains. Train amount: {serverTrainStates.Count}");
                foreach (WorldTrain selectedTrain in serverTrainStates)
                {
                    Main.DebugLog($"Synching train: {selectedTrain.Guid}.");

                    TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);

                    if(train != null)
                    {
                        if (train.frontCoupler.IsCoupled())
                            train.frontCoupler.Uncouple(false);
                        if (train.rearCoupler.IsCoupled())
                            train.rearCoupler.Uncouple(false);
                        yield return FullResyncCar(train, selectedTrain);
                    }
                }

                foreach (WorldTrain selectedTrain in serverTrainStates.Where(t => t.IsFrontCouplerCoupled || t.IsRearCouplerCoupled))
                {
                    Main.DebugLog($"Synching train: {selectedTrain.Guid}.");

                    TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);

                    ResyncCoupling(train, selectedTrain);
                }
                IsChangeByNetwork = false;
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    private void ResyncCoupling(TrainCar train, WorldTrain serverState)
    {
        if (serverState.IsFrontCouplerCoupled && !train.frontCoupler.IsCoupled())
        {
            if (serverState.IsFrontCouplerCockOpen && serverState.IsFrontCouplerHoseConnected)
                train.frontCoupler.TryCouple(false);
            else
                train.frontCoupler.TryCouple(false, true);
        }
        else
        { 
            if (serverState.IsFrontCouplerCockOpen && !train.frontCoupler.IsCockOpen)
                train.frontCoupler.IsCockOpen = true;

            if (serverState.IsFrontCouplerHoseConnected && !train.frontCoupler.GetAirHoseConnectedTo() && train.frontCoupler.GetFirstCouplerInRange())
                train.frontCoupler.ConnectAirHose(train.frontCoupler.GetFirstCouplerInRange(), false);
        }

        if (serverState.IsRearCouplerCoupled && !train.rearCoupler.IsCoupled())
        {
            if (serverState.IsRearCouplerCockOpen && serverState.IsRearCouplerHoseConnected)
                train.rearCoupler.TryCouple(false);
            else
                train.rearCoupler.TryCouple(false, true);
        }
        else
        {
            if (serverState.IsRearCouplerCockOpen && !train.rearCoupler.IsCockOpen)
                train.rearCoupler.IsCockOpen = true;

            if (serverState.IsRearCouplerHoseConnected && !train.rearCoupler.GetAirHoseConnectedTo() && train.rearCoupler.GetFirstCouplerInRange())
                train.rearCoupler.ConnectAirHose(train.rearCoupler.GetFirstCouplerInRange(), false);
        }
    }

    private IEnumerator FullResyncCar(TrainCar train, WorldTrain serverState)
    {
        Main.DebugLog($"Train found: {train != null}");
        if (!train)
        {
            Main.DebugLog($"Initializing train");
            train = InitializeNewTrainCar(serverState);
            Main.DebugLog($"Train found: {train != null}");
            if (!train)
                yield break;
        }
        Main.DebugLog($"Train load interior");
        train.LoadInterior();
        train.keepInteriorLoaded = true;
        Main.DebugLog($"Train interior should stay loaded: {train.keepInteriorLoaded}");

        Main.DebugLog($"Train set derailed");
        bool isDerailed = train.derailed;
        Main.DebugLog($"Train is derailed: {isDerailed}");
        if (train.Bogies != null && train.Bogies.Length >= 2)
        {
            Main.DebugLog($"Train Bogies synching");
            Bogie bogie1 = train.Bogies[0];
            Bogie bogie2 = train.Bogies[train.Bogies.Length - 1];
            Main.DebugLog($"Train bogies are set {bogie1 != null && bogie2 != null}");

            isDerailed = serverState.IsBogie1Derailed || serverState.IsBogie2Derailed;
            Main.DebugLog($"Train is derailed by bogies {isDerailed}");
            if (serverState.IsBogie1Derailed && !bogie1.HasDerailed)
            {
                bogie1.Derail();
            }

            if (serverState.IsBogie2Derailed && !bogie2.HasDerailed)
            {
                bogie2.Derail();
            }
            Main.DebugLog($"Train bogies synced");

            if (bogie1.HasDerailed || bogie2.HasDerailed)
            {
                Main.DebugLog("Teleport train to derailed position");
                train.transform.position = serverState.Position + WorldMover.currentMove;
                train.transform.rotation = serverState.Rotation;

                Main.DebugLog("Stop syncing rest of train since values will be reset at rerail");
                yield break;
            }
        }

        Main.DebugLog($"Train repositioning sync: Pos: {serverState.Position.ToString("G3")}");
        TeleportTrainToTrack(train, serverState.Position, serverState.Forward);

        if (!isDerailed && train.derailed)
        {
            Main.DebugLog($"Train is not derailed on host so rerail");
            yield return RerailDesynced(train, serverState.Position, serverState.Forward);
        }

        SyncLocomotiveWithServerStates(train, serverState);

        Main.DebugLog($"Train physics sync");
        train.rb.velocity = serverState.Velocity;
        train.rb.angularVelocity = serverState.AngularVelocity;
        Main.DebugLog($"Train physics sync finished");
        Main.DebugLog($"Train should be synced");
    } 

    private void SyncLocomotiveWithServerStates(TrainCar train, WorldTrain serverState)
    {
        if (!train.IsLoco)
            return;

        Main.DebugLog($"Train Loco specific sync");
        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                Main.DebugLog($"Train Loco is shunter");
                LocoControllerShunter controllerShunter = train.GetComponent<LocoControllerShunter>();
                Main.DebugLog($"Train controller found {controllerShunter != null}");
                Shunter shunter = serverState.Shunter;
                Main.DebugLog($"Train Loco Server data found {shunter != null}");

                ShunterDashboardControls shunterDashboard = train.interior.GetComponentInChildren<ShunterDashboardControls>();
                Main.DebugLog($"Shunter dashboard found {shunterDashboard != null}");
                if (shunter.IsEngineOn)
                {
                    Main.DebugLog($"Sync engine on state");
                    if (shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 0)
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();

                    if (shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Value == 0)
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();

                    if (shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 0)
                        shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();

                    controllerShunter.SetEngineRunning(true);
                }
                else
                {
                    Main.DebugLog($"Sync engine off state");
                    if (shunter.IsSideFuse1On && shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 0 || !shunter.IsSideFuse1On && shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1)
                        train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();

                    if (shunter.IsSideFuse2On && shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Value == 0 || !shunter.IsSideFuse2On && shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Value == 1)
                        train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();

                    if (shunter.IsMainFuseOn && shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 0 || !shunter.IsMainFuseOn && shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1)
                        shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();

                    controllerShunter.SetEngineRunning(false);
                }
                break;
        }
        Main.DebugLog($"Train Loco specific sync finished");

        if (train.IsLoco)
        {
            Main.DebugLog($"Train Loco generic sync");
            LocoControllerBase controller = train.GetComponent<LocoControllerBase>();
            Main.DebugLog($"Train Loco controller found {controller != null}");
            if (serverState.Brake != controller.brake)
            {
                Main.DebugLog($"Train Loco sync Brake");
                controller.SetBrake(serverState.Brake);
            }

            if (serverState.IndepBrake != controller.independentBrake)
            {
                Main.DebugLog($"Train Loco sync IndepBrake");
                controller.SetIndependentBrake(serverState.IndepBrake);
            }

            if (serverState.Sander != 0 && !controller.IsSandOn())
            {
                Main.DebugLog($"Train Loco sync Sander");
                controller.SetSanders(serverState.Sander);
            }

            if (serverState.Reverser != controller.reverser)
            {
                Main.DebugLog($"Train Loco sync Reverser");
                controller.SetReverser(serverState.Reverser);
            }

            if (serverState.Throttle != controller.throttle)
            {
                Main.DebugLog($"Train Loco sync Throttle");
                controller.SetThrottle(serverState.Throttle);
            }
        }
    }

    private TrainCar InitializeNewTrainCar(WorldTrain train)
    {
        GameObject carPrefab = CarTypes.GetCarPrefab(train.CarType);
        TrainCar newTrain = CarSpawner.SpawnLoadedCar(carPrefab, train.Id, train.Guid, train.IsPlayerSpawned, train.Position, train.Rotation, 
            train.IsBogie1Derailed, RailTrackRegistry.GetTrackWithName(train.Bogie1RailTrackName), train.Bogie1PositionAlongTrack,
            train.IsBogie2Derailed, RailTrackRegistry.GetTrackWithName(train.Bogie2RailTrackName), train.Bogie2PositionAlongTrack,
            train.IsFrontCouplerCoupled, train.IsRearCouplerCoupled);

        newTrain.LoadInterior();
        newTrain.keepInteriorLoaded = true;

        newTrain.gameObject.AddComponent<NetworkTrainPosSync>();
        if (newTrain.IsLoco)
            newTrain.gameObject.AddComponent<NetworkTrainSync>();

        newTrain.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        newTrain.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

        TeleportTrainToTrack(newTrain, train.Position, train.Forward);

        ResyncCoupling(newTrain, train);
        trainCars.Add(newTrain);
        return newTrain;
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        IsChangeByNetwork = true;
        trainCar.Rerail(trainCar.Bogies[0].track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
        yield return new WaitUntil(() => !trainCar.derailed);
        WorldTrain serverState = serverTrainStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
        if (serverState != null)
            SyncLocomotiveWithServerStates(trainCar, serverState);
        IsChangeByNetwork = false;
    }

    internal void TeleportTrainToTrack(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        IsChangeByNetwork = true;
        if(trainCar.AreBogiesFullyInitialized())
            trainCar.MoveToTrack(RailTrack.GetClosest(pos + WorldMover.currentMove).track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
        trainCar.transform.position = pos + WorldMover.currentMove;
        trainCar.transform.forward = fwd;
        IsChangeByNetwork = false;
    }

    #region Sending
    internal void SendDerailTrainUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_DERAIL: TrainID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            writer.Write<TrainDerail>(new TrainDerail()
            {
                TrainId = trainCar.CarGUID,
                IsBogie1Derailed = bogie1.HasDerailed,
                IsBogie2Derailed = bogie2.HasDerailed,
                Bogie1TrackName = bogie1.HasDerailed ? "" : bogie1.track.name,
                Bogie2TrackName = bogie2.HasDerailed ? "" : bogie2.track.name,
                Bogie1PositionAlongTrack = bogie1.HasDerailed ? 0 : bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.HasDerailed ? 0 : bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_DERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendTrainLocationUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_LOCATION_UPDATE: TrainID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            writer.Write<TrainLocation>(new TrainLocation()
            {
                TrainId = trainCar.CarGUID,
                Forward = trainCar.transform.forward,
                Velocity = trainCar.rb.velocity,
                AngularVelocity = trainCar.rb.angularVelocity,
                Position = trainCar.transform.position - WorldMover.currentMove,
                Rotation = trainCar.transform.rotation,
                Bogie1TrackName = bogie1.track.name,
                Bogie2TrackName = bogie2.track.name,
                Bogie1PositionAlongTrack = bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    internal void SendNewLeverValue(NetworkTrainSync trainSync, Levers lever, float value)
    {
        if (!IsSynced)
            return;

        TrainCar curTrain = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Train;
        Main.DebugLog($"[CLIENT] > TRAIN_LEVER: TrainID: {curTrain.ID}, Lever: {lever}, value: {value}");
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

    private void SendPlayerTrainCarChange(TrainCar train)
    {
        if (!IsSynced)
            return;

        if (train)
            Main.DebugLog($"[CLIENT] > TRAIN_SWITCH: TrainId {train.ID}, GUID: {train.CarGUID}");
        else
            Main.DebugLog($"[CLIENT] > TRAIN_SWITCH: Player left train");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainCarChange>(new TrainCarChange()
            {
                PlayerId = SingletonBehaviour<UnityClient>.Instance.ID,
                TrainId = train ? train.CarGUID : "",
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SWITCH, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendTrainsCoupledChange(Coupler thisCoupler, Coupler otherCoupler, bool viaChainInteraction, bool isCoupled)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_COUPLE: Coupler_1: {thisCoupler.train.ID}, Coupler_2: {otherCoupler.train.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
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

    internal void SyncTrainCars()
    {
        IsSynced = false;
        Main.DebugLog($"[CLIENT] > TRAIN_SYNC_ALL");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SYNC_ALL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCouplerCockChanged(Coupler coupler, bool isCockOpen)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_COUPLE_COCK: Coupler: {coupler.train.ID}, isOpen: {isCockOpen}");

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

    internal void SendCouplerHoseConChanged(Coupler coupler, bool isConnected)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_COUPLE_HOSE: Coupler: {coupler.train.ID}, IsConnected: {isConnected}");

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
                IsC2Front = C2 != null ? C2.isFrontCoupler : false,
                IsConnected = isConnected
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_COUPLE_HOSE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion

    #region Receiving
    private void OnPlayerTrainCarChange(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnPlayerTrainCarChange, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCarChange changedCar = reader.ReadSerializable<TrainCarChange>();
                NetworkPlayerSync targetPlayerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerSyncById(changedCar.PlayerId);
                Main.DebugLog($"[CLIENT] < TRAIN_SWITCH: Packet size: {reader.Length}, TrainData: {(changedCar.TrainId == "" ? "No" : "Yes")}");
                if (changedCar.TrainId == "")
                {
                    targetPlayerSync.Train = null;
                }
                else
                {
                    TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == changedCar.TrainId);
                    if (train)
                    {
                        Main.DebugLog($"[CLIENT] < TRAIN_SWITCH: Train found: {train}, ID: {train.ID}, GUID: {train.CarGUID}");
                        targetPlayerSync.Train = train;
                    }
                    else
                    {
                        Main.DebugLog($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {changedCar.TrainId}");
                    }
                }
            }
        }
    }

    private void OnTrainDerailment(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainDerailment, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainDerail derailed = reader.ReadSerializable<TrainDerail>();
                TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == derailed.TrainId);
                
                if (train)
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_DERAIL: Packet size: {reader.Length}, TrainId: {train.ID}");
                    WorldTrain serverTrainState = serverTrainStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (serverTrainState == null)
                    {
                        serverTrainState = new WorldTrain()
                        {
                            Guid = train.CarGUID,
                        };
                        if (train.carType == TrainCarType.LocoShunter)
                            serverTrainState.Shunter = new Shunter();
                        serverTrainStates.Add(serverTrainState);
                    }
                    serverTrainState.IsBogie1Derailed = derailed.IsBogie1Derailed;
                    serverTrainState.IsBogie2Derailed = derailed.IsBogie2Derailed;
                    serverTrainState.Bogie1RailTrackName = derailed.Bogie1TrackName;
                    serverTrainState.Bogie2RailTrackName = derailed.Bogie2TrackName;
                    serverTrainState.Bogie1PositionAlongTrack = derailed.Bogie1PositionAlongTrack;
                    serverTrainState.Bogie2PositionAlongTrack = derailed.Bogie2PositionAlongTrack;

                    train.GetComponent<NetworkTrainPosSync>().hostDerailed = true;
                    train.Derail();
                }
                else
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {derailed.TrainId}");
                }
            }
        }
    }

    private void OnTrainLocationMessage(Message message)
    {
        if (!IsSynced)
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLocation location = reader.ReadSerializable<TrainLocation>();
                TrainCar train = trainCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == location.TrainId);

                if (train)
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_LOCATION_UPDATE: TrainID: {train.ID}");
                    SingletonBehaviour<CoroutineManager>.Instance.Run(train.GetComponent<NetworkTrainPosSync>().UpdateLocation(location));
                    WorldTrain serverState = serverTrainStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (serverState == null)
                    {
                        serverState = new WorldTrain()
                        {
                            Guid = train.CarGUID,
                        };
                        if (train.carType == TrainCarType.LocoShunter)
                            serverState.Shunter = new Shunter();
                        serverTrainStates.Add(serverState);
                    }

                    serverState.Position = location.Position;
                    serverState.Rotation = location.Rotation;
                    serverState.Velocity = location.Velocity;
                    serverState.AngularVelocity = location.AngularVelocity;
                    serverState.Forward = location.Forward;
                    serverState.Bogie1PositionAlongTrack = location.Bogie1PositionAlongTrack;
                    serverState.Bogie1RailTrackName = location.Bogie1TrackName;
                    serverState.Bogie2PositionAlongTrack = location.Bogie2PositionAlongTrack;
                    serverState.Bogie2RailTrackName = location.Bogie2TrackName;
                }
            }
        }
    }

    private void OnTrainLeverMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainLeverMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLever lever = reader.ReadSerializable<TrainLever>();

                TrainCar train = trainCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == lever.TrainId);
                if (train && train.IsLoco)
                {
                    WorldTrain serverTrainState = serverTrainStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if(serverTrainState == null)
                    {
                        serverTrainState = new WorldTrain()
                        {
                            Guid = train.CarGUID,
                            IsLoco = true,
                        };
                        if (train.carType == TrainCarType.LocoShunter)
                            serverTrainState.Shunter = new Shunter();
                        serverTrainStates.Add(serverTrainState);
                    }
                    Main.DebugLog($"[CLIENT] < TRAIN_LEVER: Packet size: {reader.Length}, TrainID: {train.ID}, Lever: {lever.Lever}, Value: {lever.Value}");
                    IsChangeByNetwork = true;
                    LocoControllerBase baseController = train.GetComponent<LocoControllerBase>();
                    switch (lever.Lever)
                    {
                        case Levers.Throttle:
                            baseController.SetThrottle(lever.Value);
                            serverTrainState.Throttle = lever.Value;
                            break;

                        case Levers.Brake:
                            baseController.SetBrake(lever.Value);
                            serverTrainState.Brake = lever.Value;
                            break;

                        case Levers.IndependentBrake:
                            baseController.SetIndependentBrake(lever.Value);
                            serverTrainState.IndepBrake = lever.Value;
                            break;

                        case Levers.Reverser:
                            baseController.SetReverser(lever.Value);
                            serverTrainState.Reverser = lever.Value;
                            break;

                        case Levers.Sander:
                            baseController.SetSanders(lever.Value);
                            serverTrainState.Sander = lever.Value;
                            break;

                        case Levers.SideFuse_1:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();
                                serverTrainState.Shunter.IsSideFuse1On = lever.Value == 1;
                                if (lever.Value == 0)
                                    serverTrainState.Shunter.IsEngineOn = false;
                            }
                            break;

                        case Levers.SideFuse_2:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();
                                serverTrainState.Shunter.IsSideFuse2On = lever.Value == 1;
                                if (lever.Value == 0)
                                    serverTrainState.Shunter.IsEngineOn = false;
                            }
                            break;

                        case Levers.MainFuse:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                                serverTrainState.Shunter.IsMainFuseOn = lever.Value == 1;
                                if (lever.Value == 0)
                                    serverTrainState.Shunter.IsEngineOn = false;
                            }
                            break;

                        case Levers.FusePowerStarter:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.powerRotaryObj.GetComponent<RotaryBase>().SetValue(lever.Value);
                                if (serverTrainState.Shunter.IsSideFuse1On && serverTrainState.Shunter.IsSideFuse2On && serverTrainState.Shunter.IsMainFuseOn && lever.Value == 1)
                                    serverTrainState.Shunter.IsEngineOn = true;
                                else if (lever.Value == 0)
                                    serverTrainState.Shunter.IsEngineOn = false;
                            }
                            break;

                        case Levers.Horn:
                            float valHorn = lever.Value;
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().hornObj.GetComponent<LeverBase>().SetValue(valHorn);
                                if (valHorn < 0.5)
                                    valHorn = valHorn * 2;
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

    private void OnTrainCoupleChange(Message message, bool isCoupled)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainCoupleChange, message, isCoupled))
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
                TrainCar trainCoupler1 = trainCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC1);
                TrainCar trainCoupler2 = trainCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC2);

                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverTrainStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
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
                    train = serverTrainStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        if (trainCoupler2.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
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

                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {coupled.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {coupled.IsC2Front})");
                    Coupler C1 = coupled.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = coupled.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if(C1.GetFirstCouplerInRange() == C2 && isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.TryCouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if(C1.coupledTo == C2 && !isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.Uncouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if (C1.coupledTo != C2 && !isCoupled)
                    {
                        Main.DebugLog($"[CLIENT] < TRAIN_COUPLE: Couplers were already uncoupled");
                    }
                }
                else
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {coupled.TrainIdC1}, TrainID_C2: {coupled.TrainIdC2}");
                }
            }
        }
    }

    private void OnTrainCouplerCockChange(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainCouplerCockChange, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerCockChange cockChange = reader.ReadSerializable<TrainCouplerCockChange>();
                TrainCar trainCoupler = trainCars.FirstOrDefault(t => t.CarGUID == cockChange.TrainIdCoupler);

                if (trainCoupler)
                {
                    WorldTrain train = serverTrainStates.FirstOrDefault(t => t.Guid == cockChange.TrainIdCoupler);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler.CarGUID,
                        };
                        if (trainCoupler.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
                    }

                    if (cockChange.IsCouplerFront)
                        train.IsFrontCouplerHoseConnected = cockChange.IsOpen;
                    else
                        train.IsRearCouplerHoseConnected = cockChange.IsOpen;
                    IsChangeByNetwork = true;
                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE_COCK: Packet size: {reader.Length}, TrainID: {trainCoupler.ID} (isFront: {cockChange.IsCouplerFront}), isOpen: {cockChange.IsOpen}");
                    Coupler coupler = cockChange.IsCouplerFront ? trainCoupler.frontCoupler : trainCoupler.rearCoupler;
                    coupler.IsCockOpen = cockChange.IsOpen;
                    IsChangeByNetwork = false;
                }
                else
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE_COCK: Trains not found, TrainID: {cockChange.TrainIdCoupler}, isOpen: {cockChange.IsOpen}");
                }
            }
        }
    }

    private void OnTrainCouplerHoseChange(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnTrainCouplerHoseChange, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerHoseChange hoseChange = reader.ReadSerializable<TrainCouplerHoseChange>();
                TrainCar trainCoupler1 = trainCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC1);
                TrainCar trainCoupler2 = null;
                if (hoseChange.IsConnected)
                    trainCoupler2 = trainCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC2);

                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverTrainStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;
                    train = serverTrainStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        if (trainCoupler2.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
                    }
                    if (hoseChange.IsC2Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;
                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE_HOSE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {hoseChange.IsC2Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = hoseChange.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if ((C1.IsCoupled() && C1.coupledTo == C2) || C1.GetFirstCouplerInRange() == C2)
                    {
                        IsChangeByNetwork = true;
                        C1.ConnectAirHose(C2, true);
                        IsChangeByNetwork = false;
                    }
                }
                else if(trainCoupler1 && !hoseChange.IsConnected)
                {
                    WorldTrain train = serverTrainStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        if (trainCoupler1.carType == TrainCarType.LocoShunter)
                            train.Shunter = new Shunter();
                        serverTrainStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.IsFrontCouplerHoseConnected = hoseChange.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseChange.IsConnected;

                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE_HOSE: TrainID: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    C1.DisconnectAirHose(true);
                }
                else
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {hoseChange.TrainIdC1}, TrainID_C2: {hoseChange.TrainIdC2}, IsConnected: {hoseChange.IsConnected}");
                }
            }
        }
    }
    #endregion

    #endregion
}
