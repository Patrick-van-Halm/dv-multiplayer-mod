using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Networking;
using DVMultiplayer.DTO.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerPlugin
{
    class PlayerPlugin : Plugin
    {
        Dictionary<IClient, NPlayer> players = new Dictionary<IClient, NPlayer>();

        public override bool ThreadSafe => false;

        public override Version Version => new Version("2.3.0");

        public PlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }

        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write<Disconnect>(new Disconnect()
                {
                    PlayerId = e.Client.ID
                });

                using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_DISCONNECT, writer))
                    foreach (IClient client in ClientManager.GetAllClients().Where(client => client != e.Client))
                        client.SendMessage(outMessage, SendMode.Reliable);
            }
        }

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += SpawnLocationMessage;
            e.Client.MessageReceived += LocationUpdateMessage;
        }

        private void LocationUpdateMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == (ushort)NetworkTags.PLAYER_LOCATION_UPDATE)
                {
                    if (players.TryGetValue(e.Client, out NPlayer player))
                    {
                        Location newLocation;
                        using (DarkRiftReader reader = message.GetReader())
                        {
                            newLocation = reader.ReadSerializable<Location>();
                            player.Position = newLocation.Position;
                            player.Rotation = newLocation.Rotation;
                        }

                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write<Location>(newLocation);

                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != e.Client))
                                    client.SendMessage(outMessage, SendMode.Unreliable);
                        }
                    }
                }
            }
        }

        private void SpawnLocationMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == (ushort)NetworkTags.PLAYER_SPAWN)
                {
                    NPlayer player;
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        player = reader.ReadSerializable<NPlayer>();
                    }

                    if (players.Count > 0)
                    {
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write<NPlayer>(player);

                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != e.Client))
                                    client.SendMessage(outMessage, SendMode.Reliable);
                        }

                        foreach (NPlayer p in players.Values)
                        {
                            using (DarkRiftWriter writer = DarkRiftWriter.Create())
                            {
                                writer.Write<NPlayer>(p);

                                using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                                    e.Client.SendMessage(outMessage, SendMode.Reliable);
                            }
                        }
                    }

                    players.Add(e.Client, player);
                }
            }
        }
    }
}
