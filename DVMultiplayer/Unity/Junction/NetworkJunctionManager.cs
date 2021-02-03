using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.DTO.Junction;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkJunctionManager : SingletonBehaviour<NetworkJunctionManager>
{
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; internal set; }

    private VisualSwitch[] switches;
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
    }

    public void PlayerDisconnect()
    {
        if (switches == null)
            return;

        foreach (VisualSwitch @switch in switches)
        {
            if(@switch.junction.gameObject.GetComponent<NetworkJunctionSync>())
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

                foreach(Switch switchInfo in switchesServer)
                {
                    VisualSwitch junction = switches.FirstOrDefault(j => j.junction.position == switchInfo.Position);
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
        IsSynced = true;
    }

    public void OnJunctionSwitched(Vector3 position, Junction.SwitchMode mode, bool switchedToLeft)
    {
        Main.DebugLog($"Junction received switch");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Switch>(new Switch()
            {
                Position = position,
                Mode = (SwitchMode)mode,
                SwitchToLeft = switchedToLeft
            });

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    public void ReceiveNetworkJunctionSwitch(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < SWITCH_CHANGED");

            while (reader.Position < reader.Length)
            {
                Switch switchInfo = reader.ReadSerializable<Switch>();

                VisualSwitch junction = switches.FirstOrDefault(j => j.junction.position == switchInfo.Position);
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

    internal void SyncHost()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<Switch> serverSwitches = new List<Switch>();
            foreach(VisualSwitch s in switches)
            {
                serverSwitches.Add(new Switch()
                {
                    Position = s.junction.position,
                    Mode = SwitchMode.NO_SOUND,
                    SwitchToLeft = s.junction.selectedBranch == 0
                });
            }
            writer.Write(serverSwitches.ToArray());


            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_HOSTSYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
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