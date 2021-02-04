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
    public TrainCar[] trainCars;
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
        trainCars = GameObject.FindObjectsOfType<TrainCar>();
        Main.DebugLog($"{trainCars.Length} traincars found, {trainCars.Where(car => car.IsLoco).Count()} are locomotives");

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
        if (NetworkManager.IsHost())
        {
            SyncInitializedTrains();
        }
        SaveTrainCarsLoaded = true;
    }

    private void SyncInitializedTrains()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<WorldTrain> trains = new List<WorldTrain>();
            Main.DebugLog($"Host synching trains with server. Train amount: {trainCars.Length}");
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
                    IsFrontCouplerCoupled = car.frontCoupler.coupledTo != null,
                    IsRearCouplerCoupled = car.rearCoupler.coupledTo != null,
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
                 Rotation = trainCar.transform.rotation
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
        if (playerSync.Train)
            playerSync.Train.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = false;

        playerSync.Train = trainCar;
        SendPlayerTrainCarChange(trainCar);

        if(trainCar)
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
                WorldTrain[] allTrains = reader.ReadSerializables<WorldTrain>();
                Main.DebugLog($"Synching trains. Train amount: {allTrains.Length}");
                foreach (WorldTrain selectedTrain in allTrains)
                {
                    Main.DebugLog($"Synching train: {selectedTrain.Guid}.");

                    TrainCar train = trainCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);
                    Main.DebugLog($"Train found: {train != null}");
                    if (!train)
                    {
                        Main.DebugLog($"Initializing train");
                        train = InitializeNewTrainCar(selectedTrain);
                        Main.DebugLog($"Train found: {train != null}");
                        if (!train)
                            continue;
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

                        isDerailed = selectedTrain.IsBogie1Derailed || selectedTrain.IsBogie2Derailed;
                        Main.DebugLog($"Train is derailed by bogies {isDerailed}");
                        if (selectedTrain.IsBogie1Derailed && !bogie1.HasDerailed)
                        {
                            bogie1.Derail();
                        }

                        if (selectedTrain.IsBogie2Derailed && !bogie2.HasDerailed)
                        {
                            bogie2.Derail();
                        }
                        Main.DebugLog($"Train bogies synced");

                        if(bogie1.HasDerailed || bogie2.HasDerailed)
                        {
                            Main.DebugLog("Teleport train to derailed position");
                            train.transform.position = selectedTrain.Position + WorldMover.currentMove;
                            train.transform.rotation = selectedTrain.Rotation;

                            Main.DebugLog("Stop syncing rest of train since values will be reset at rerail");
                            continue;
                        }
                    }

                    Main.DebugLog($"Train repositioning sync: Pos: {selectedTrain.Position.ToString("G3")}");
                    TeleportTrainToTrack(train, selectedTrain.Position , selectedTrain.Forward);

                    if (!isDerailed && train.derailed)
                    {
                        Main.DebugLog($"Train is not derailed on host so rerail");
                        yield return RerailDesynced(train, selectedTrain.Position, selectedTrain.Forward);
                    }

                    Main.DebugLog($"Train Loco specific sync");
                    switch (selectedTrain.CarType)
                    {
                        case TrainCarType.LocoShunter:
                            Main.DebugLog($"Train Loco is shunter");
                            LocoControllerShunter controllerShunter = train.GetComponent<LocoControllerShunter>();
                            Main.DebugLog($"Train controller found {controllerShunter != null}");
                            Shunter shunter = selectedTrain.Shunter;
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
                        if (selectedTrain.Brake != controller.brake)
                        {
                            Main.DebugLog($"Train Loco sync Brake");
                            controller.SetBrake(selectedTrain.Brake);
                        }

                        if (selectedTrain.IndepBrake != controller.independentBrake)
                        {
                            Main.DebugLog($"Train Loco sync IndepBrake");
                            controller.SetIndependentBrake(selectedTrain.IndepBrake);
                        }

                        if (selectedTrain.Sander != 0 && !controller.IsSandOn())
                        {
                            Main.DebugLog($"Train Loco sync Sander");
                            controller.SetSanders(selectedTrain.Sander);
                        }

                        if (selectedTrain.Reverser != controller.reverser)
                        {
                            Main.DebugLog($"Train Loco sync Reverser");
                            controller.SetReverser(selectedTrain.Reverser);
                        }

                        if (selectedTrain.Throttle != controller.throttle)
                        {
                            Main.DebugLog($"Train Loco sync Throttle");
                            controller.SetThrottle(selectedTrain.Throttle);
                        }
                    }

                    Main.DebugLog($"Train physics sync");
                    train.rb.velocity = selectedTrain.Velocity;
                    train.rb.angularVelocity = selectedTrain.AngularVelocity;
                    Main.DebugLog($"Train physics sync finished");
                    Main.DebugLog($"Train should be synced");
                }
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    private TrainCar InitializeNewTrainCar(WorldTrain train)
    {
        GameObject carPrefab = CarTypes.GetCarPrefab(train.CarType);
        TrainCar newTrain = CarSpawner.SpawnLoadedCar(carPrefab, train.Id, train.Guid, train.IsPlayerSpawned, train.Position, train.Rotation, 
            train.IsBogie1Derailed, RailTrackRegistry.GetTrackWithName(train.Bogie1RailTrackName), train.Bogie1PositionAlongTrack,
            train.IsBogie2Derailed, RailTrackRegistry.GetTrackWithName(train.Bogie2RailTrackName), train.Bogie2PositionAlongTrack,
            train.IsFrontCouplerCoupled, train.IsRearCouplerCoupled);

        newTrain.gameObject.AddComponent<NetworkTrainPosSync>();
        if (newTrain.IsLoco)
            newTrain.gameObject.AddComponent<NetworkTrainSync>();

        newTrain.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        newTrain.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
        return newTrain;
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        IsChangeByNetwork = true;
        trainCar.Rerail(trainCar.Bogies[0].track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
        yield return new WaitUntil(() => !trainCar.derailed);
        IsChangeByNetwork = false;
    }

    internal void TeleportTrainToTrack(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        IsChangeByNetwork = true;
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
            writer.Write<TrainDerail>(new TrainDerail()
            {
                TrainId = trainCar.CarGUID
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
                IsBogie1Derailed = bogie1.HasDerailed,
                IsBogie2Derailed = bogie1.HasDerailed,
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
                TrainCar train = trainCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == derailed.TrainId);
                
                if (train)
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_DERAIL: Packet size: {reader.Length}, TrainId: {train.ID}");
                    if(!train.derailed)
                        train.Derail();
                    train.GetComponent<NetworkTrainPosSync>().hostDerailed = true;
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
                if (train)
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_LEVER: Packet size: {reader.Length}, TrainID: {train.ID}, Lever: {lever.Lever}, Value: {lever.Value}");
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
