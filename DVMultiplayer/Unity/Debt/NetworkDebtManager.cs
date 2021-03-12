using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.ServicePenalty;
using DVMultiplayer;
using DVMultiplayer.DTO.Debt;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class NetworkDebtManager : SingletonBehaviour<NetworkDebtManager>
{
    public bool IsChangeByNetwork { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();
        Main.Log($"NetworkDebtManager initialized");
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Instance)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.DEBT_LOCO_PAID:
                    OnLocoDeptPaidMessage(message);
                    break;
            }
        }
    }

    private void OnLocoDeptPaidMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < DEBT_LOCO_PAID");

            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                LocoDebtPaid data = reader.ReadSerializable<LocoDebtPaid>();
                if (data.isDestroyed)
                    SingletonBehaviour<LocoDebtController>.Instance.destroyedLocosDebts.FirstOrDefault(t => t.ID == data.Id).Pay();
                else
                    SingletonBehaviour<LocoDebtController>.Instance.trackedLocosDebts.FirstOrDefault(t => t.ID == data.Id).Pay();
                IsChangeByNetwork = false;
            }
        }
        
    }

    internal void OnLocoDeptPaid(string id, bool isDestroyed)
    {
        Main.Log($"[CLIENT] > DEBT_LOCO_PAID");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new LocoDebtPaid() { Id = id, isDestroyed = isDestroyed });

            using (Message message = Message.Create((ushort)NetworkTags.DEBT_LOCO_PAID, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
}
