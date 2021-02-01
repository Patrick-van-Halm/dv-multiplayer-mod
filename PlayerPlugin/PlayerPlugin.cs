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

        public override Version Version => new Version("2.4.0");

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
            e.Client.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                switch ((NetworkTags)message.Tag)
                {
                    case NetworkTags.PLAYER_LOCATION_UPDATE:
                        LocationUpdateMessage(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_SPAWN:
                        SpawnLocationMessage(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_WORLDMOVED:
                        UpdateWorldMoveData(message, e.Client);
                        break;
                }
            }
        }

        private void UpdateWorldMoveData(Message message, IClient sender)
        {
            if (players.TryGetValue(sender, out NPlayer player))
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    WorldMove move = reader.ReadSerializable<WorldMove>();
                    player.WorldMoverPos = move.WorldPosition;
                }

                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                    client.SendMessage(message, SendMode.Reliable);
            }
        }

        private void LocationUpdateMessage(Message message, IClient sender)
        {
            if (players.TryGetValue(sender, out NPlayer player))
            {
                Location newLocation;
                using (DarkRiftReader reader = message.GetReader())
                {
                    newLocation = reader.ReadSerializable<Location>();
                    player.Position = newLocation.AbsPosition;
                    player.Rotation = newLocation.NewRotation;
                }

                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write<Location>(newLocation);

                    using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                        foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                            client.SendMessage(outMessage, SendMode.Unreliable);
                }
            }
        }

        private void SpawnLocationMessage(Message message, IClient sender)
        {
            NPlayer player;
            using (DarkRiftReader reader = message.GetReader())
            {
                player = reader.ReadSerializable<NPlayer>();
                if(players.Count > 0)
                {
                    NPlayer host = players.Values.First();
                    List<string> missingMods = GetMissingMods(host.Mods, player.Mods);
                    List<string> extraMods = GetMissingMods(player.Mods, host.Mods);
                    if (missingMods.Count != 0 || extraMods.Count != 0)
                    {
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(missingMods.ToArray());
                            writer.Write(extraMods.ToArray());

                            using (Message msg = Message.Create((ushort)NetworkTags.PLAYER_MODS_MISMATCH, writer))
                                sender.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            }

            if (players.Count > 0)
            {
                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                    client.SendMessage(message, SendMode.Reliable);

                foreach (NPlayer p in players.Values)
                {
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(p);

                        using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                            sender.SendMessage(outMessage, SendMode.Reliable);
                    }
                }
            }

            players.Add(sender, player);
        }

        private List<string> GetMissingMods(string[] modList1, string[] modList2)
        {
            List<string> missingMods = new List<string>();
            foreach(string mod in modList1)
            {
                if (!modList2.Contains(mod))
                    missingMods.Add(mod);
            }
            return missingMods;
        }
    }
}
