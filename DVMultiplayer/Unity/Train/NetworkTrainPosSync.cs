using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using UnityEngine;

internal class NetworkTrainPosSync : MonoBehaviour
{
    private TrainCar trainCar;
    private Vector3? newExtraForce = null;
    private Vector3 prevExtraForce;
    private bool isOutOfSync = false;
    private Vector3 hostPos;
    private bool hostStationary;
    private float prevIndepBrakePos;
    private float prevBrakePos;
    private Vector3 prevPos;
    public bool hostDerailed;
    private bool velocityShouldUpdate = false;
    public event Action<TrainCar> OnTrainCarInitialized;

#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.DebugLog($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();
        Main.DebugLog($"[{trainCar.ID}] NetworkTrainPosSync Awake called");
        Main.DebugLog($"Starting coroutine for location updating");

        Main.DebugLog($"Listen to derailment events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
        trainCar.LogicCarInitialized += TrainCar_LogicCarInitialized;

        if (NetworkManager.IsHost())
        {
            SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());
        }
    }

    private void TrainCar_LogicCarInitialized()
    {
        OnTrainCarInitialized?.Invoke(trainCar);
    }

    private void FixedUpdate()
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced)
            return;

        if (!NetworkManager.IsHost() && newExtraForce.HasValue && !trainCar.derailed)
        {
            if(isOutOfSync && hostStationary && ((trainCar.brakeSystem.hasIndependentBrake && trainCar.brakeSystem.independentBrakePosition > 0) || trainCar.brakeSystem.trainBrakePosition > 0))
            {
                prevIndepBrakePos = trainCar.brakeSystem.hasIndependentBrake ? trainCar.brakeSystem.independentBrakePosition : 0;
                prevBrakePos = trainCar.brakeSystem.trainBrakePosition;
                if (trainCar.brakeSystem.hasIndependentBrake)
                    trainCar.brakeSystem.independentBrakePosition = 0;
                trainCar.brakeSystem.trainBrakePosition = 0;
                velocityShouldUpdate = true;
            }
            else if (!isOutOfSync && hostStationary && prevBrakePos != 0 && prevIndepBrakePos != 0)
            {
                prevIndepBrakePos = 0;
                prevBrakePos = 0;
                if (trainCar.brakeSystem.hasIndependentBrake)
                    trainCar.brakeSystem.independentBrakePosition = prevIndepBrakePos;
                trainCar.brakeSystem.trainBrakePosition = prevBrakePos;
            }

            if(velocityShouldUpdate)
            {
                trainCar.rb.velocity = newExtraForce.Value;
                velocityShouldUpdate = false;
            }
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
        

    private void TrainRerail()
    {
        SingletonBehaviour<NetworkTrainManager>.Instance.SendRerailCarUpdate(trainCar);
    }

    private void TrainDerail(TrainCar derailedCar)
    {
        if (!NetworkManager.IsHost() || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendDerailCarUpdate(trainCar);
    }

    private IEnumerator UpdateLocation()
    {
        yield return new WaitUntil(() => Vector3.Distance(trainCar.transform.position, prevPos) > .01f);
        if (NetworkManager.IsHost())
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar);
            prevPos = trainCar.transform.position;
        }
        yield return UpdateLocation();
    }

    internal IEnumerator UpdateLocation(TrainLocation location)
    {
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
            trainCar.rb.velocity = location.Velocity;
            trainCar.rb.angularVelocity = location.AngularVelocity;
            trainCar.transform.forward = location.Forward;
            yield break;
        }

        location.Position += WorldMover.currentMove;
        hostStationary = location.IsStationary;
        SyncVelocityAndSpeedUpIfDesyncedOnFrontCar(location);
    }

    private void SyncVelocityAndSpeedUpIfDesyncedOnFrontCar(TrainLocation location)
    {
        if (trainCar.frontCoupler.IsCoupled())
        {
            return;
        }

        SyncVelocityAndSpeedUpIfDesynced(location);
    }

    private void SyncVelocityAndSpeedUpIfDesynced(TrainLocation location)
    {
        float distance = Distance(trainCar.transform, location.Position);
        hostPos = location.Position;
        Vector3 newVelocity = Vector3.zero;
        if (distance > 10f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + 1.5f);
            if(newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }
        else if (distance > 3f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + .86f);
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }
        else if (distance <= 3f && distance > .1f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z + .25f);
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }
        else if (distance < .1f && distance > -.1f)
        {
            newVelocity = location.Velocity;
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = false;
        }
        else if (distance <= -.1f && distance > -3f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - .25f);
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }
        else if (distance <= -3f && distance >= -10f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - .86f);
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }
        else if (distance > -10f)
        {
            newVelocity = new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, trainCar.rb.velocity.z - 1.5f);
            if (newVelocity != newExtraForce)
            {
                velocityShouldUpdate = true;
                newExtraForce = newVelocity;
            }
            isOutOfSync = true;
        }

        if (isOutOfSync)
            Main.mod.Logger.Log($"{trainCar.ID} Is out of sync difference is {distance}m");
    }

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
