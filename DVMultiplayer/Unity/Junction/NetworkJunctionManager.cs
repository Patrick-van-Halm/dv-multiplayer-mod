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
    private VisualSwitch[] switches;
    protected override void Awake()
    {
        base.Awake();
        switches = GameObject.FindObjectsOfType<VisualSwitch>();
        foreach(VisualSwitch @switch in switches)
        {
            @switch.junction.gameObject.AddComponent<NetworkJunctionSync>();
        }

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void OnDisconnect()
    {
        foreach (VisualSwitch @switch in switches)
        {
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
            }
        }
    }

    public void OnJunctionSwitched(Vector3 position, Junction.SwitchMode mode, bool switchedToLeft)
    {
        if (IsChangeByNetwork)
            return;

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
            Main.DebugLog($"[CLIENT] SWITCH_CHANGED received | Packet size: {reader.Length}");
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed location update packet.");
            //    return;
            //}

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
}