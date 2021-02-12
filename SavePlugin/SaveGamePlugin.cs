using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using System;
using System.Linq;

namespace SaveGamePlugin
{
    public class SaveGamePlugin : Plugin
    {
        private SaveGame save;

        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.0.8");

        public SaveGamePlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                if (!tag.ToString().StartsWith("SAVEGAME_"))
                    return;

                Logger.Trace($"[SERVER] < {tag.ToString()}");

                switch (tag)
                {
                    case NetworkTags.SAVEGAME_UPDATE:
                        UpdateSaveGame(message);
                        break;

                    case NetworkTags.SAVEGAME_GET:
                        SendSaveGame(e.Client);
                        break;
                }
            }
        }

        private void SendSaveGame(IClient sender)
        {
            if (save != null && sender.ID != 0)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write<SaveGame>(save);

                    using (Message outMessage = Message.Create((ushort)NetworkTags.SAVEGAME_GET, writer))
                        sender.SendMessage(outMessage, SendMode.Reliable);
                }
            }
        }

        private void UpdateSaveGame(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                save = reader.ReadSerializable<SaveGame>();
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
