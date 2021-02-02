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

class NetworkTrainManager : SingletonBehaviour<NetworkTrainManager>
{
    public TrainCar[] trainCars;
    public bool IsChangeByNetwork { get; internal set; }

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
        trainCars = GameObject.FindObjectsOfType<TrainCar>();
        Main.DebugLog($"{trainCars.Length} traincars found, {trainCars.Where(car => car.IsLoco).Count()} are locomotives");

        foreach (TrainCar trainCar in trainCars)
        {
            trainCar.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
            trainCar.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

            if (trainCar.IsLoco)
            {
                trainCar.gameObject.AddComponent<NetworkTrainPosSync>();
                trainCar.gameObject.AddComponent<NetworkTrainSync>();
            }
        }

        PlayerManager.CarChanged += OnPlayerSwitchTrainCarEvent;
    }

    internal void SendRerailTrainUpdate(TrainCar trainCar)
    {
        throw new NotImplementedException();
    }

    public void PlayerDisconnect()
    {
        foreach (TrainCar trainCar in trainCars.Where(car => car.IsLoco))
        {
            Destroy(trainCar.GetComponent<NetworkTrainPosSync>());
            Destroy(trainCar.GetComponent<NetworkTrainSync>());
        }
    }

    private void OnPlayerSwitchTrainCarEvent(TrainCar trainCar)
    {
        if (trainCar)
        {
            if (!trainCar.GetComponent<NetworkTrainSync>())
                trainCar.gameObject.AddComponent<NetworkTrainSync>();

            if (!trainCar.GetComponent<NetworkTrainPosSync>())
                trainCar.gameObject.AddComponent<NetworkTrainPosSync>();
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
            }
        }
    }

    #region Sending
    internal void SendDerailTrainUpdate(TrainCar trainCar)
    {
        if (!NetworkManager.IsHost())
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
        if (!NetworkManager.IsHost())
            return;

        Main.DebugLog($"[CLIENT] > TRAIN_LOCATION_UPDATE: TrainID: {trainCar.ID}");
        Vector3[] carsPositions = new Vector3[trainCar.trainset.cars.Count];
        for(int i = 0; i < carsPositions.Length; i++)
        {
            carsPositions[i] = trainCar.trainset.cars[i].transform.position - WorldMover.currentMove;
        }
        Quaternion[] carsRotation = new Quaternion[trainCar.trainset.cars.Count];
        for (int i = 0; i < carsRotation.Length; i++)
        {
            carsRotation[i] = trainCar.trainset.cars[i].transform.rotation;
        }

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainLocation>(new TrainLocation()
            {
                TrainId = trainCar.CarGUID,
                Forward = trainCar.transform.forward,
                Velocity = trainCar.rb.velocity,
                AngularVelocity = trainCar.rb.angularVelocity,
                IsDerailed = trainCar.derailed,
                AmountCars = (uint)trainCar.trainset.cars.Count,
                LocoInTrainSetIndex = (uint)trainCar.indexInTrainset,
                CarsPositions = carsPositions,
                CarsRotation = carsRotation
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    internal void SendNewLeverValue(NetworkTrainSync trainSync, Levers lever, float value)
    {
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
        if(train)
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

    internal void SendCouplerCockChanged(Coupler coupler, bool isCockOpen)
    {
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
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

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
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

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
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                TrainLocation location = reader.ReadSerializable<TrainLocation>();
                TrainCar train = trainCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == location.TrainId);
                Main.DebugLog($"[CLIENT] < TRAIN_LOCATION_UPDATE: Packet size: {reader.Length}, TrainID: {train.ID}");

                if (train)
                {
                    train.GetComponent<NetworkTrainPosSync>().UpdateLocation(location);
                }
            }
        }
    }

    private void OnTrainLeverMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                TrainLever lever = reader.ReadSerializable<TrainLever>();

                TrainCar train = trainCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == lever.TrainId);
                if (train)
                {
                    Main.DebugLog($"[CLIENT] < TRAIN_LEVER: Packet size: {reader.Length}, TrainID: {train.ID}, Lever: {lever.Lever}, Value: {lever.Value}");
                    IsChangeByNetwork = true;
                    switch (lever.Lever)
                    {
                        case Levers.Throttle:
                            train.GetComponent<LocoControllerBase>().SetThrottle(lever.Value);
                            break;

                        case Levers.Brake:
                            train.GetComponent<LocoControllerBase>().SetBrake(lever.Value);
                            break;

                        case Levers.IndependentBrake:
                            train.GetComponent<LocoControllerBase>().SetIndependentBrake(lever.Value);
                            break;

                        case Levers.Reverser:
                            train.GetComponent<LocoControllerBase>().SetReverser(lever.Value);
                            break;

                        case Levers.Sander:
                            train.GetComponent<LocoControllerBase>().SetSanders(lever.Value);
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
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                float val = lever.Value;
                                if (val < 0.5)
                                    val = val - 0.5f * -1;
                                else
                                    val = val - 0.5f;
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().gameObject.GetComponent<Horn>().SetInput(val);
                            }
                            break;
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnTrainCoupleChange(Message message, bool isCoupled)
    {
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
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

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
        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

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
