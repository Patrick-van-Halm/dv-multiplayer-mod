using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JunctionPlugin
{
    public class JunctionPlugin : Plugin
    {
        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.0.0");

        public JunctionPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                switch ((NetworkTags)message.Tag)
                {
                    case NetworkTags.SWITCH_CHANGED:
                        ReliableSendToOthers(message, e.Client);
                        break;
                }
            }
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}
