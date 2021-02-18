using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.CabControls;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Turntable;
using DVMultiplayer.Networking;
using System.Collections;
using System.Linq;
using UnityEngine;

internal class NetworkTurntableManager : SingletonBehaviour<NetworkTurntableManager>
{
    private TurntableController[] turntables;

    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; set; }
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        base.Awake();

        turntables = GameObject.FindObjectsOfType<TurntableController>();
        foreach (TurntableController turntable in turntables)
        {
            turntable.gameObject.AddComponent<NetworkTurntableSync>();
        }

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;

        if (NetworkManager.IsHost())
            IsSynced = true;
    }

    internal void SyncTurntables()
    {
        IsSynced = false;
        Main.Log($"[CLIENT] > TURNTABLE_SYNC");

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

                case NetworkTags.TURNTABLE_SNAP:
                    ReceiveTurntableOnSnap(message);
                    break;
            }
        }
    }

    private void ReceiveTurntableSync(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_SYNC");

            while (reader.Position < reader.Length)
            {
                Turntable[] turntableInfos = reader.ReadSerializables<Turntable>();

                foreach (Turntable turntable in turntableInfos)
                {
                    TurntableController turntableController = turntables.FirstOrDefault(j => j.transform.position == turntable.Position + WorldMover.currentMove);
                    if (turntableController)
                    {
                        SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntableController, turntable.Rotation.Value));
                    }
                }
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    private IEnumerator RotateTurntableTowardsByNetwork(TurntableController turntableController, float angle, bool moveSlow = false)
    {
        IsChangeByNetwork = true;
        if (moveSlow)
        {
            bool addToAngle = angle > turntableController.turntable.currentYRotation;
            turntableController.turntable.targetYRotation += addToAngle ? .5f : -.5f;
            turntableController.turntable.RotateToTargetRotation();
            yield return new WaitUntil(() => turntableController.turntable.targetYRotation == turntableController.turntable.currentYRotation);
            if (!(Mathf.Abs(turntableController.turntable.currentYRotation - angle) < .01f))
            {
                yield return RotateTurntableTowardsByNetwork(turntableController, angle, moveSlow);
            }
        }
        else
        {
            turntableController.turntable.targetYRotation = angle;
            turntableController.turntable.RotateToTargetRotation();
            yield return new WaitUntil(() => Mathf.Abs(turntableController.turntable.currentYRotation - angle) < .1f);
        }
        IsChangeByNetwork = false;
    }

    internal void OnTurntableRotationChanged(TurntableController turntable, float value, bool isLever)
    {
        if (!IsSynced)
            return;
        Main.Log($"[CLIENT] > TURNTABLE_ANGLE_CHANGED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            if (!isLever)
            {
                writer.Write(new Turntable()
                {
                    Position = turntable.transform.position - WorldMover.currentMove,
                    Rotation = value,
                    LeverAngle = null
                });
            }
            else
            {
                writer.Write(new Turntable()
                {
                    Position = turntable.transform.position - WorldMover.currentMove,
                    Rotation = null,
                    LeverAngle = value
                });
            }

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_ANGLE_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void OnTurntableSnap(TurntableController turntable, float value)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TURNTABLE_SNAP");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new Turntable()
            {
                Position = turntable.transform.position - WorldMover.currentMove,
                Rotation = value,
                LeverAngle = null
            });

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_SNAP, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void PlayerDisconnect()
    {
        if (turntables == null)
            return;

        foreach (TurntableController turntable in turntables)
        {
            if (turntable.gameObject.GetComponent<NetworkTurntableSync>())
                DestroyImmediate(turntable.gameObject.GetComponent<NetworkTurntableSync>());
        }
    }

    public void ReceiveNetworkTurntableChange(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveNetworkTurntableChange, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_ANGLE_CHANGED");

            while (reader.Position < reader.Length)
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == turntableInfo.Position + WorldMover.currentMove);
                if (turntable && turntableInfo.Rotation.HasValue)
                {
                    turntable.leverGO.GetComponent<LeverBase>().MoveLeverAndReset(.5f);
                }
                else if (turntable && turntableInfo.LeverAngle.HasValue)
                {
                    turntable.leverGO.GetComponent<LeverBase>().SetValue(turntableInfo.LeverAngle.Value);
                }
            }
        }
    }

    public void ReceiveTurntableOnSnap(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveTurntableOnSnap, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_SNAP");

            while (reader.Position < reader.Length)
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == turntableInfo.Position + WorldMover.currentMove);
                if (turntable && turntableInfo.Rotation.HasValue)
                {
                    turntable.leverGO.GetComponent<LeverBase>().SetValue(.5f);
                    SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntable, turntableInfo.Rotation.Value, !SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(turntable.transform.position)));
                }
            }
        }
    }


}