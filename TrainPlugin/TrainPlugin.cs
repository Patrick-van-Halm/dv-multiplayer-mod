using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainPlugin
{
    public class TrainPlugin : Plugin
    {
        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.6.0");

        private readonly List<WorldTrain> worldTrains;

        public TrainPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            worldTrains = new List<WorldTrain>();
            ClientManager.ClientConnected += OnClientConnected;
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("TRAIN_"))
                    return;

                if (tag != NetworkTags.TRAIN_LOCATION_UPDATE)
                    Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.TRAIN_LEVER:
                        UpdateTrainLever(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_RERAIL:
                        UpdateTrainRerail(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_DERAIL:
                        UpdateTrainDerailed(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_SWITCH:
                        UpdateTrainSwitch(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_COUPLE:
                        UpdateCouplingState(message, e.Client, true);
                        break;

                    case NetworkTags.TRAIN_UNCOUPLE:
                        UpdateCouplingState(message, e.Client, false);
                        break;

                    case NetworkTags.TRAIN_COUPLE_HOSE:
                        UpdateCoupledHoseState(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_COUPLE_COCK:
                        UpdateCoupleCockState(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_SYNC_ALL:
                        SendWorldTrains(e.Client);
                        break;

                    case NetworkTags.TRAIN_HOSTSYNC:
                        SyncTrainDataFromHost(message);
                        break;

                    case NetworkTags.TRAIN_LOCATION_UPDATE:
                        UpdateTrainPosition(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_INIT:
                        NewTrainInitialized(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_REMOVAL:
                        OnCarRemovalMessage(message, e.Client);
                        break;
                }
            }
        }

        private void OnCarRemovalMessage(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                CarRemoval carRemoval = reader.ReadSerializable<CarRemoval>();
                WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == carRemoval.Guid);
                if (train == null)
                {
                    train = new WorldTrain()
                    {
                        Guid = carRemoval.Guid,
                        IsRemoved = true,
                    };
                    worldTrains.Add(train);
                }
            }

            Logger.Trace("[SERVER] > TRAIN_REMOVAL");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainSwitch(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                TrainCarChange carChange = reader.ReadSerializable<TrainCarChange>();
                if (carChange.TrainId != "")
                {
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == carChange.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = carChange.TrainId
                        };
                        worldTrains.Add(train);
                    }
                }
            }

            Logger.Trace("[SERVER] > TRAIN_SWITCH");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateCoupleCockState(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplerCockChange cockStateChanged = reader.ReadSerializable<TrainCouplerCockChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == cockStateChanged.TrainIdCoupler);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = cockStateChanged.TrainIdCoupler,
                        };
                        worldTrains.Add(train);
                    }

                    if (cockStateChanged.IsCouplerFront)
                        train.IsFrontCouplerHoseConnected = cockStateChanged.IsOpen;
                    else
                        train.IsRearCouplerHoseConnected = cockStateChanged.IsOpen;
                }
            }

            Logger.Trace("[SERVER] > TRAIN_COUPLE_COCK");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateCoupledHoseState(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplerHoseChange hoseStateChanged = reader.ReadSerializable<TrainCouplerHoseChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == hoseStateChanged.TrainIdC1);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = hoseStateChanged.TrainIdC1,
                        };
                        worldTrains.Add(train);
                    }

                    if (hoseStateChanged.IsC1Front)
                        train.IsFrontCouplerHoseConnected = hoseStateChanged.IsConnected;
                    else
                        train.IsRearCouplerHoseConnected = hoseStateChanged.IsConnected;

                    if (hoseStateChanged.IsConnected)
                    {
                        train = worldTrains.FirstOrDefault(t => t.Guid == hoseStateChanged.TrainIdC2);
                        if (train == null)
                        {
                            train = new WorldTrain()
                            {
                                Guid = hoseStateChanged.TrainIdC2,
                            };
                            worldTrains.Add(train);
                        }

                        if (hoseStateChanged.IsC2Front)
                            train.IsFrontCouplerHoseConnected = hoseStateChanged.IsConnected;
                        else
                            train.IsRearCouplerHoseConnected = hoseStateChanged.IsConnected;
                    }
                }
            }

            Logger.Trace("[SERVER] > TRAIN_COUPLE_HOSE");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateCouplingState(Message message, IClient sender, bool isCoupled)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplingChange coupledChanged = reader.ReadSerializable<TrainCouplingChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == coupledChanged.TrainIdC1);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = coupledChanged.TrainIdC1,
                        };
                        worldTrains.Add(train);
                    }

                    if (isCoupled)
                    {
                        if (coupledChanged.IsC1Front)
                            train.IsFrontCouplerCoupled = true;
                        else
                            train.IsRearCouplerCoupled = true;
                    }
                    else
                    {
                        if (coupledChanged.IsC1Front)
                            train.IsFrontCouplerCoupled = false;
                        else
                            train.IsRearCouplerCoupled = false;
                    }

                    train = worldTrains.FirstOrDefault(t => t.Guid == coupledChanged.TrainIdC2);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = coupledChanged.TrainIdC2,
                        };
                        worldTrains.Add(train);
                    }

                    if (isCoupled)
                    {
                        if (coupledChanged.IsC2Front)
                            train.IsFrontCouplerCoupled = true;
                        else
                            train.IsRearCouplerCoupled = true;
                    }
                    else
                    {
                        if (coupledChanged.IsC2Front)
                            train.IsFrontCouplerCoupled = false;
                        else
                            train.IsRearCouplerCoupled = false;
                    }
                }
            }

            Logger.Trace($"[SERVER] > {(isCoupled ? "TRAIN_COUPLE" : "TRAIN_UNCOUPLE")}");
            ReliableSendToOthers(message, sender);
        }

        private void SyncTrainDataFromHost(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                worldTrains.Clear();
                worldTrains.AddRange(reader.ReadSerializables<WorldTrain>());
            }
        }

        private void NewTrainInitialized(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                worldTrains.Add(reader.ReadSerializable<WorldTrain>());
            }

            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainDerailed(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainDerail derailed = reader.ReadSerializable<TrainDerail>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == derailed.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = derailed.TrainId,
                        };
                        worldTrains.Add(train);
                    }

                    train.IsBogie1Derailed = derailed.IsBogie1Derailed;
                    train.IsBogie2Derailed = derailed.IsBogie2Derailed;
                    train.Bogie1RailTrackName = derailed.Bogie1TrackName;
                    train.Bogie2RailTrackName = derailed.Bogie2TrackName;
                    train.Bogie1PositionAlongTrack = derailed.Bogie1PositionAlongTrack;
                    train.Bogie2PositionAlongTrack = derailed.Bogie2PositionAlongTrack;

                    if (train.CarType == TrainCarType.LocoShunter)
                        train.Shunter.IsEngineOn = false;
                }
            }

            Logger.Trace("[SERVER] > TRAIN_DERAIL");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainRerail(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainRerail rerailed = reader.ReadSerializable<TrainRerail>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == rerailed.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = rerailed.TrainId,
                        };
                        worldTrains.Add(train);
                    }

                    train.IsBogie1Derailed = false;
                    train.IsBogie2Derailed = false;
                    train.Bogie1RailTrackName = rerailed.Bogie1TrackName;
                    train.Bogie2RailTrackName = rerailed.Bogie2TrackName;
                    train.Bogie1PositionAlongTrack = rerailed.Bogie1PositionAlongTrack;
                    train.Bogie2PositionAlongTrack = rerailed.Bogie2PositionAlongTrack;
                    train.Position = rerailed.Position;
                    train.Forward = rerailed.Forward;
                    train.Rotation = rerailed.Rotation;
                }
            }
            Logger.Trace("[SERVER] > TRAIN_RERAIL");
            ReliableSendToOthers(message, sender);
        }

        private void SendWorldTrains(IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    Logger.Trace("[SERVER] > TRAIN_SYNC_ALL");

                    writer.Write(worldTrains.ToArray());

                    using (Message msg = Message.Create((ushort)NetworkTags.TRAIN_SYNC_ALL, writer))
                        sender.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        private void UpdateTrainLever(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainLever lever = reader.ReadSerializable<TrainLever>();
                    Logger.Trace($"Setting serverTrainState lever: [{lever.TrainId}] {lever.Lever}: {lever.Value}");
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == lever.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = lever.TrainId,
                            IsLoco = true
                        };
                        worldTrains.Add(train);
                        Logger.Trace($"Train not found adding new one");
                    }

                    switch (lever.Lever)
                    {
                        case Levers.Throttle:
                            train.Throttle = lever.Value;
                            break;

                        case Levers.Brake:
                            train.Brake = lever.Value;
                            break;

                        case Levers.IndependentBrake:
                            Logger.Trace($"Changed independent brake of {train.Guid}");
                            train.IndepBrake = lever.Value;
                            break;

                        case Levers.Sander:
                            train.Sander = lever.Value;
                            break;

                        case Levers.Reverser:
                            train.Reverser = lever.Value;
                            break;
                    }

                    switch (train.CarType)
                    {
                        case TrainCarType.LocoShunter:
                            if (train.Shunter == null)
                                train.Shunter = new Shunter();

                            Shunter shunter = train.Shunter;
                            switch (lever.Lever)
                            {
                                case Levers.MainFuse:
                                    shunter.IsMainFuseOn = lever.Value == 1;
                                    if (lever.Value == 0)
                                        shunter.IsEngineOn = false;
                                    break;

                                case Levers.SideFuse_1:
                                    shunter.IsSideFuse1On = lever.Value == 1;
                                    if (lever.Value == 0)
                                        shunter.IsEngineOn = false;
                                    break;

                                case Levers.SideFuse_2:
                                    shunter.IsSideFuse2On = lever.Value == 1;
                                    if (lever.Value == 0)
                                        shunter.IsEngineOn = false;
                                    break;

                                case Levers.FusePowerStarter:
                                    if (shunter.IsSideFuse1On && shunter.IsSideFuse2On && shunter.IsMainFuseOn && lever.Value == 1)
                                        shunter.IsEngineOn = true;
                                    else if (lever.Value == 0)
                                        shunter.IsEngineOn = false;
                                    break;
                            }
                            break;
                    }
                }
            }
            Logger.Trace("[SERVER] > TRAIN_LEVER");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainPosition(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainLocation newLocation = reader.ReadSerializable<TrainLocation>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == newLocation.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = newLocation.TrainId,
                        };
                        worldTrains.Add(train);
                    }

                    train.Position = newLocation.Position;
                    train.Rotation = newLocation.Rotation;
                    train.Velocity = newLocation.Velocity;
                    train.AngularVelocity = newLocation.AngularVelocity;
                    train.Forward = newLocation.Forward;
                    train.Bogie1PositionAlongTrack = newLocation.Bogie1PositionAlongTrack;
                    train.Bogie1RailTrackName = newLocation.Bogie1TrackName;
                    train.Bogie2PositionAlongTrack = newLocation.Bogie2PositionAlongTrack;
                    train.Bogie2RailTrackName = newLocation.Bogie2TrackName;
                }
            }
            //Logger.Trace("[SERVER] > TRAIN_LOCATION_UPDATE");
            UnreliableSendToOthers(message, sender);
        }

        private void UnreliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Unreliable);
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}
