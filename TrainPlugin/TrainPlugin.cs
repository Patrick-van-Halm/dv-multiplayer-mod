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

        public override Version Version => new Version("1.3.1");

        public TrainPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
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
                switch ((NetworkTags) message.Tag)
                {
                    case NetworkTags.TRAIN_LEVER:
                    case NetworkTags.TRAIN_SWITCH:
                    case NetworkTags.TRAIN_DERAIL:
                    case NetworkTags.TRAIN_COUPLE:
                    case NetworkTags.TRAIN_UNCOUPLE:
                    case NetworkTags.TRAIN_COUPLE_HOSE:
                    case NetworkTags.TRAIN_COUPLE_COCK:
                        ReliableSendToOthers(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_LOCATION_UPDATE:
                        UnreliableSendToOthers(message, e.Client);
                        break;
                }
            }
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
