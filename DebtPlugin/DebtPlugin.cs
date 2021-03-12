using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebtPlugin
{
    public class DebtPlugin : Plugin
    {
        public override bool ThreadSafe => false;
        public override Version Version => new Version("1.0.0");

        public DebtPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("DEBT_"))
                    return;

                Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.DEBT_LOCO_PAID:
                        OnLocoDebtPaid(message, e.Client);
                        break;
                }
            }
        }

        private void OnLocoDebtPaid(Message message, IClient client)
        {
            Logger.Trace($"[SERVER] > DEBT_LOCO_PAID");
            ReliableSendToOthers(message, client);
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}
