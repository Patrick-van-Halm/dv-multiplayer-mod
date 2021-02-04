using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Turntable;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkTurntableManager : SingletonBehaviour<NetworkTurntableManager>
{
    private TurntableController[] turntables;

    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; set; }

    protected override void Awake()
    {
        base.Awake();

        turntables = GameObject.FindObjectsOfType<TurntableController>();
        foreach(TurntableController turntable in turntables)
        {
            turntable.gameObject.AddComponent<NetworkTurntableSync>();
        }

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    internal void SyncTurntables()
    {
        IsSynced = false;
        Main.DebugLog($"[CLIENT] > TURNTABLE_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.TURNTABLE_ANGLE_CHANGED:
                    ReceiveNetworkTurntableChange(message);
                    break;

                case NetworkTags.TURNTABLE_SYNC:
                    ReceiveTurntableSync(message);
                    break;
            }
        }
    }

    private void ReceiveTurntableSync(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < TURNTABLE_SYNC");

            while (reader.Position < reader.Length)
            {
                Turntable[] turntableInfos = reader.ReadSerializables<Turntable>();

                foreach(Turntable turntable in turntableInfos)
                {
                    TurntableController turntableController = turntables.FirstOrDefault(j => j.transform.position == turntable.Position);
                    if (turntableController)
                    {
                        SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntableController, turntable));
                    }
                }
            }
        }
        IsSynced = true;
    }

    private IEnumerator RotateTurntableTowardsByNetwork(TurntableController turntableController, Turntable turntable)
    {
        IsChangeByNetwork = true;
        turntableController.turntable.targetYRotation = turntable.Rotation.Value;
        turntableController.GetComponent<NetworkTurntableSync>().yRot = turntableController.turntable.targetYRotation;
        turntableController.turntable.RotateToTargetRotation();
        yield return new WaitUntil(() => turntableController.turntable.currentYRotation == turntable.Rotation.Value);
        IsChangeByNetwork = false;
    }

    internal void OnTurntableRotationChanged(TurntableController turntable, float value, bool isLever)
    {
        Main.DebugLog($"[CLIENT] > TURNTABLE_ANGLE_CHANGED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            if (!isLever)
            {
                writer.Write(new Turntable()
                {
                    Position = turntable.transform.position,
                    Rotation = value,
                    LeverAngle = null
                });
            }
            else
            {
                writer.Write(new Turntable()
                {
                    Position = turntable.transform.position,
                    Rotation = null,
                    LeverAngle = value
                });
            }

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_ANGLE_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void PlayerDisconnect()
    {
        if (turntables == null)
            return;

        foreach (TurntableController turntable in turntables)
        {
            if(turntable.gameObject.GetComponent<NetworkTurntableSync>())
                Destroy(turntable.gameObject.GetComponent<NetworkTurntableSync>());
        }
    }

    public void ReceiveNetworkTurntableChange(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < TURNTABLE_ANGLE_CHANGED");

            while (reader.Position < reader.Length)
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == turntableInfo.Position);
                if (turntable && turntableInfo.Rotation.HasValue)
                {
                    turntable.leverGO.GetComponent<LeverBase>().SetValue(.5f);
                    if(Mathf.Abs(turntable.turntable.currentYRotation - turntableInfo.Rotation.Value) > .1f)
                    {
                        SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntable, turntableInfo));
                    }
                }
                else if (turntable && turntableInfo.LeverAngle.HasValue)
                {
                    turntable.leverGO.GetComponent<LeverBase>().SetValue(turntableInfo.LeverAngle.Value);
                }
            }
        }
    }

    
}