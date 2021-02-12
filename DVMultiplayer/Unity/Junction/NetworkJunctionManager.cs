using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Junction;
using DVMultiplayer.Networking;
using System.Linq;
using UnityEngine;

internal class NetworkJunctionManager : SingletonBehaviour<NetworkJunctionManager>
{
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; internal set; }

    private VisualSwitch[] switches;
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        Main.DebugLog("NetworkJunctionManager initialized");
        base.Awake();
        switches = GameObject.FindObjectsOfType<VisualSwitch>();
        Main.DebugLog($"NetworkJunctionManager found {switches.Length} switches in world");
        foreach (VisualSwitch @switch in switches)
        {
            @switch.junction.gameObject.AddComponent<NetworkJunctionSync>();
        }

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;

        if (NetworkManager.IsHost())
            IsSynced = true;
    }

    public void PlayerDisconnect()
    {
        if (switches == null)
            return;

        foreach (VisualSwitch @switch in switches)
        {
            if (@switch.junction.gameObject.GetComponent<NetworkJunctionSync>())
                Destroy(@switch.junction.gameObject.GetComponent<NetworkJunctionSync>());
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.SWITCH_CHANGED:
                    ReceiveNetworkJunctionSwitch(message);
                    break;

                case NetworkTags.SWITCH_SYNC:
                    ReceiveServerSwitches(message);
                    break;
            }
        }
    }

    private void ReceiveServerSwitches(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < SWITCH_SYNC");

            while (reader.Position < reader.Length)
            {
                Switch[] switchesServer = reader.ReadSerializables<Switch>();

                foreach (Switch switchInfo in switchesServer)
                {
                    VisualSwitch junction = switches.FirstOrDefault(j => j.junction.position == switchInfo.Position + WorldMover.currentMove);
                    if (junction)
                    {
                        if (switchInfo.SwitchToLeft && junction.junction.selectedBranch == 0 || !switchInfo.SwitchToLeft && junction.junction.selectedBranch == 1)
                            continue;
                        IsChangeByNetwork = true;
                        junction.junction.Switch(Junction.SwitchMode.NO_SOUND);
                        IsChangeByNetwork = false;
                    }
                }
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    public void OnJunctionSwitched(Vector3 position, Junction.SwitchMode mode, bool switchedToLeft)
    {
        if (!IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > SWITCH_CHANGED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Switch>(new Switch()
            {
                Position = position - WorldMover.currentMove,
                Mode = (SwitchMode)mode,
                SwitchToLeft = switchedToLeft
            });

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    public void ReceiveNetworkJunctionSwitch(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveNetworkJunctionSwitch, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < SWITCH_CHANGED");

            while (reader.Position < reader.Length)
            {
                Switch switchInfo = reader.ReadSerializable<Switch>();

                VisualSwitch junction = switches.FirstOrDefault(j => j.junction.position == switchInfo.Position + WorldMover.currentMove);
                if (junction)
                {
                    if (switchInfo.SwitchToLeft && junction.junction.selectedBranch == 0 || !switchInfo.SwitchToLeft && junction.junction.selectedBranch == 1)
                        return;
                    IsChangeByNetwork = true;
                    junction.junction.Switch((Junction.SwitchMode)switchInfo.Mode);
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    internal void SyncJunction()
    {
        IsSynced = false;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
}