using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

internal class NetworkTrainPosSync : MonoBehaviour
{
    private TrainCar trainCar;
    private WorldTrain serverState;
    private Vector3? newExtraForce = null;
    public bool isOutOfSync = false;
    private bool hostStationary;
    private float prevIndepBrakePos;
    private float prevBrakePos;
    private Vector3 prevPos;
    public bool hostDerailed;
    private bool velocityShouldUpdate = false;
    private Coroutine updatePositionCoroutine;
    public event Action<TrainCar> OnTrainCarInitialized;
    public bool hasLocalPlayerAuthority = false;
    internal bool resetAuthority = false;
    internal NetworkTurntableSync turntable = null;

#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();
        
        Main.Log($"[{trainCar.ID}] NetworkTrainPosSync Awake called");

        Main.Log($"Listening to derailment/rerail events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
        Main.Log($"Listening to LogicCar loaded event");
        trainCar.LogicCarInitialized += TrainCar_LogicCarInitialized;

        trainCar.CarDamage.IgnoreDamage(true);
        trainCar.rb.isKinematic = true;

        if (NetworkManager.IsHost())
        {
            SingletonBehaviour<CoroutineManager>.Instance.Run(CheckAuthorityChange());
        }
    }

    private IEnumerator CheckAuthorityChange()
    {
        yield return new WaitForSeconds(.1f);
        if (turntable == null)
        {
            bool authNeedsChange = true;
            if (!resetAuthority) 
            {
                foreach (TrainCar car in trainCar.trainset.cars)
                {
                    if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(car).Select(p => p.GetComponent<NetworkPlayerSync>().Id).Contains(serverState.AuthorityPlayerId))
                    {
                        authNeedsChange = false;
                        break;
                    }
                }
            }

            if (authNeedsChange)
            {
                resetAuthority = false;
                GameObject player = null;
                foreach (TrainCar car in trainCar.trainset.cars)
                {
                    if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(trainCar).Length > 0)
                        player = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(trainCar)[0];
                }

                if (!player)
                    player = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();

                if (player.GetComponent<NetworkPlayerSync>().Id != serverState.AuthorityPlayerId)
                    SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, player.GetComponent<NetworkPlayerSync>().Id);
            }
        }
        else
        {
            if(serverState.AuthorityPlayerId != turntable.playerAuthId)
                SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, turntable.playerAuthId);
        }
        yield return CheckAuthorityChange();
    }

    private void OnDestroy()
    {
        Main.Log($"NetworkTrainPosSync.OnDestroy()");
        if (!trainCar)
            return;

        if(trainCar.logicCar != null)
            Main.Log($"[{trainCar.ID}] NetworkTrainPosSync OnDestroy called");
        Main.Log($"Stop listening to derailment/rerail events");
        trainCar.OnDerailed -= TrainDerail;
        trainCar.OnRerailed -= TrainRerail;
        Main.Log($"Stop listening to LogicCar loaded event");
        trainCar.LogicCarInitialized -= TrainCar_LogicCarInitialized;
        Main.Log($"Stop listening to movement changed event");
        trainCar.MovementStateChanged -= TrainCar_MovementStateChanged;
    }

    private void Update()
    {
        if (!SingletonBehaviour<NetworkPlayerManager>.Exists)
            return;

        if(serverState == null)
        {
            serverState = SingletonBehaviour<NetworkTrainManager>.Instance.GetServerStateById(trainCar.CarGUID);
            return;
        }

        bool willLocalPlayerGetAuthority = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id == serverState.AuthorityPlayerId;

        if (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)
        {
            trainCar.rb.isKinematic = false;
            hasLocalPlayerAuthority = true;
            trainCar.stress.enabled = true;
            trainCar.stress.DisableStressCheckForTwoSeconds();
            if(!turntable)
                trainCar.CarDamage.IgnoreDamage(false);
            Main.Log($"Listening to movement changed event");
            if (!trainCar.isStationary && updatePositionCoroutine == null)
            {
                updatePositionCoroutine = SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());
            }
            trainCar.MovementStateChanged += TrainCar_MovementStateChanged;
        }
        else if (!willLocalPlayerGetAuthority && hasLocalPlayerAuthority)
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.ResyncCar(trainCar);
            trainCar.rb.isKinematic = true;
            trainCar.stress.enabled = false;
            trainCar.CarDamage.IgnoreDamage(true);
            hasLocalPlayerAuthority = false;
            Main.Log($"Stop listening to movement changed event");
            trainCar.MovementStateChanged -= TrainCar_MovementStateChanged;
            if (updatePositionCoroutine != null)
            {
                SingletonBehaviour<CoroutineManager>.Instance.Stop(updatePositionCoroutine);
                updatePositionCoroutine = null;
            }
        }
    }
#pragma warning restore IDE0051 // Remove unused private members

    private void TrainCar_MovementStateChanged(bool isMoving)
    {
        if (isMoving)
        {
            if (updatePositionCoroutine == null)
                updatePositionCoroutine = SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());
        }
        else
        {
            if (updatePositionCoroutine != null)
            {
                SingletonBehaviour<CoroutineManager>.Instance.Stop(updatePositionCoroutine);
                updatePositionCoroutine = null;
            }
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar, true);
            prevPos = trainCar.transform.position;
        }
    }

    private void TrainCar_LogicCarInitialized()
    {
        OnTrainCarInitialized?.Invoke(trainCar);
    }

    private void TrainRerail()
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendRerailCarUpdate(trainCar);
    }

    private void TrainDerail(TrainCar derailedCar)
    {
        if (!hasLocalPlayerAuthority || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendDerailCarUpdate(trainCar);
    }

    private IEnumerator UpdateLocation()
    {
        yield return new WaitUntil(() => Vector3.Distance(trainCar.transform.position, prevPos) > .001f);
        if (hasLocalPlayerAuthority && !trainCar.frontCoupler.coupledTo)
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar);
            prevPos = trainCar.transform.position;
        }
        yield return UpdateLocation();
    }

    internal IEnumerator UpdateLocation(TrainLocation location)
    {
        if (hasLocalPlayerAuthority)
            yield break;

        if (trainCar.derailed && !hostDerailed)
        {
            trainCar.transform.position = location.Position + WorldMover.currentMove;
            trainCar.transform.rotation = location.Rotation;
            trainCar.transform.forward = location.Forward;
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, location.Position, location.Forward);
        }
        else if (trainCar.derailed && hostDerailed)
        {
            location.Position += WorldMover.currentMove;
            trainCar.transform.position = location.Position;
            trainCar.transform.rotation = location.Rotation;
            trainCar.transform.forward = location.Forward;
            yield break;
        }
        location.Position += WorldMover.currentMove;
        hostStationary = location.IsStationary;
        trainCar.rb.MovePosition(location.Position);
        trainCar.rb.MoveRotation(location.Rotation);
    }

    //private void SyncVelocityAndSpeedUpIfDesyncedOnFrontCar(TrainLocation location)
    //{
    //    if (trainCar.frontCoupler.IsCoupled())
    //    {
    //        return;
    //    }

    //    SyncVelocityAndSpeedUpIfDesynced(location);
    //}

    //private void SyncVelocityAndSpeedUpIfDesynced(TrainLocation location)
    //{
    //    float distance = Distance(trainCar.transform, location.Position);
    //    float curSpeed = trainCar.GetForwardSpeed() * 3.6f;
    //    Vector3 newVelocity;
    //    if (distance > 10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + 1.5f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance > 3f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + .86f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance <= 3f && distance > .1f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + (curSpeed < 25 ? .25f : .19f));
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance < .1f && distance > -.1f)
    //    {
    //        newVelocity = location.Velocity;
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = false;
    //    }
    //    else if (distance <= -.1f && distance > -3f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - (curSpeed < 25 ? .25f : .19f));
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance <= -3f && distance >= -10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - .86f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }
    //    else if (distance > -10f)
    //    {
    //        newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - 1.5f);
    //        if (newVelocity != newExtraForce)
    //        {
    //            velocityShouldUpdate = true;
    //            newExtraForce = newVelocity;
    //        }
    //        isOutOfSync = true;
    //    }

    //    if (isOutOfSync)
    //        Main.mod.Logger.Log($"{trainCar.ID} Is out of sync difference is {distance}m");
    //}

    private float Distance(Transform a, Vector3 b)
    {
        Vector3 forward = a.TransformDirection(a.forward);
        Vector3 toOther = b - a.position;
        if (Vector3.Dot(forward, toOther) < 0)
            return -Vector3.Distance(a.position, b);
        else
            return Vector3.Distance(a.position, b);
    }

    private TrainCar GetMostFrontCar(TrainCar car)
    {
        if (car.frontCoupler.coupledTo != null)
        {
            return GetMostFrontCar(car.frontCoupler.coupledTo.train);
        }
        else
        {
            return car;
        }
    }
}
