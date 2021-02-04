using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Networking;
using DVMultiplayer.DTO.Train;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainPlugin
{
    public class TrainPlugin : Plugin
    {
        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.5.8");

        private List<WorldTrain> worldTrains;

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

                Logger.Trace($"[SERVER] < {tag.ToString()}");

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
                    case NetworkTags.TRAIN_COUPLE:
                    case NetworkTags.TRAIN_UNCOUPLE:
                    case NetworkTags.TRAIN_COUPLE_HOSE:
                    case NetworkTags.TRAIN_COUPLE_COCK:
                        ReliableSendToOthers(message, e.Client);
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
                }
            }
        }

        private void SyncTrainDataFromHost(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                worldTrains.Clear();
                worldTrains.AddRange(reader.ReadSerializables<WorldTrain>());
            }
        }

        private void UpdateTrainDerailed(Message message, IClient sender)
        {
            if(worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainDerail derailed = reader.ReadSerializable<TrainDerail>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == derailed.TrainId);
                    if (train == null)
                    {
                        Logger.Error($"[{derailed.TrainId}] Train not found");
                    }
                    else
                    {
                        train.IsBogie1Derailed = derailed.IsBogie1Derailed;
                        train.IsBogie2Derailed = derailed.IsBogie2Derailed;
                        train.Bogie1RailTrackName = derailed.Bogie1TrackName;
                        train.Bogie2RailTrackName = derailed.Bogie2TrackName;
                        train.Bogie1PositionAlongTrack = derailed.Bogie1PositionAlongTrack;
                        train.Bogie2PositionAlongTrack = derailed.Bogie2PositionAlongTrack;
                    }
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
                        Logger.Error($"[{rerailed.TrainId}] Train not found");
                    }
                    else
                    {
                        train.IsBogie1Derailed = false;
                        train.IsBogie2Derailed = false;
                        train.Position = rerailed.Position;
                        train.Forward = rerailed.Forward;
                        train.Rotation = rerailed.Rotation;
                    }
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
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == lever.TrainId);
                    if (train == null)
                    {
                        Logger.Error($"[{lever.TrainId}] Train not found");
                    }
                    else
                    {
                        switch (lever.Lever)
                        {
                            case Levers.Throttle:
                                train.Throttle = lever.Value;
                                break;

                            case Levers.Brake:
                                train.Brake = lever.Value;
                                break;

                            case Levers.IndependentBrake:
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
                                        if(shunter.IsSideFuse1On && shunter.IsSideFuse2On && shunter.IsMainFuseOn && lever.Value == 1)
                                            shunter.IsEngineOn = true;
                                        else if (lever.Value == 0)
                                            shunter.IsEngineOn = false;
                                        break;
                                }
                                break;
                        }
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
                        Logger.Error($"[{newLocation.TrainId}] Train not found");
                    }
                    else
                    {
                        train.Position = newLocation.Position;
                        train.Rotation = newLocation.Rotation;
                        train.Velocity = newLocation.Velocity;
                        train.AngularVelocity = newLocation.AngularVelocity;
                        train.Forward = newLocation.Forward;
                        train.IsBogie1Derailed = newLocation.IsBogie1Derailed;
                        train.Bogie1PositionAlongTrack = newLocation.Bogie1PositionAlongTrack;
                        train.Bogie1RailTrackName = newLocation.Bogie1TrackName;
                        train.IsBogie2Derailed = newLocation.IsBogie2Derailed;
                        train.Bogie2PositionAlongTrack = newLocation.Bogie2PositionAlongTrack;
                        train.Bogie2RailTrackName = newLocation.Bogie2TrackName;
                    }
                }
            }
            Logger.Trace("[SERVER] > TRAIN_LOCATION_UPDATE");
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
