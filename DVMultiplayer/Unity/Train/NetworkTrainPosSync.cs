using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkTrainPosSync : MonoBehaviour
{
    private const float SYNC_CHECKTIME = .05f;
    private TrainCar trainCar;
    private Vector3? newVelocity = null;
    private bool isOutOfSync = false;
    private Coroutine movingCoroutine;
    private Vector3 hostPos;
    private float prevIndepBrakePos;
    private float prevBrakePos;
    public bool hostDerailed;

    private void Awake()
    {
        Main.DebugLog($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();
        Main.DebugLog($"[{trainCar.ID}] NetworkTrainPosSync Awake called");
        Main.DebugLog($"Starting coroutine for location updating");

        Main.DebugLog($"Listen to derailment events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
        trainCar.MovementStateChanged += TrainCar_MovementStateChanged;
    }

    private void TrainCar_MovementStateChanged(bool isMoving)
    {
        if (NetworkManager.IsHost())
        {
            if (isMoving)
                movingCoroutine = SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());
            else
                SingletonBehaviour<CoroutineManager>.Instance.Stop(movingCoroutine);
        }
    }

    private void TrainRerail()
    {
        SingletonBehaviour<NetworkTrainManager>.Instance.SendRerailTrainUpdate(trainCar);
    }

    private void TrainDerail(TrainCar derailedCar)
    {
        if (!NetworkManager.IsHost())
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendDerailTrainUpdate(trainCar);
    }

    private void Update()
    {
        if(!NetworkManager.IsHost() && newVelocity.HasValue)
        {
            if(hostPos != null && newVelocity.Value.z == 0 && isOutOfSync)
            {
                if((trainCar.brakeSystem.hasIndependentBrake && trainCar.brakeSystem.independentBrakePosition > 0) || trainCar.brakeSystem.trainBrakePosition > 0)
                {
                    prevIndepBrakePos = trainCar.brakeSystem.hasIndependentBrake ? trainCar.brakeSystem.independentBrakePosition : 0;
                    prevBrakePos = trainCar.brakeSystem.trainBrakePosition;
                    if (trainCar.brakeSystem.hasIndependentBrake)
                        trainCar.brakeSystem.independentBrakePosition = 0;
                    trainCar.brakeSystem.trainBrakePosition = 0;
                }
                float distance = Distance(trainCar.transform, hostPos);
                if (distance < .1f && distance > -.1f)
                {
                    isOutOfSync = false;
                    newVelocity = new Vector3(0, 0, 0);

                    if (trainCar.brakeSystem.hasIndependentBrake)
                        trainCar.brakeSystem.independentBrakePosition = prevIndepBrakePos;
                    trainCar.brakeSystem.trainBrakePosition = prevBrakePos;
                }
                else if(distance < 0)
                {
                    newVelocity = new Vector3(0, 0, -.56f);
                }
                else
                {
                    newVelocity = new Vector3(0, 0, .56f);
                }
            }
            trainCar.rb.velocity =  new Vector3(trainCar.rb.velocity.x, trainCar.rb.velocity.y, newVelocity.Value.z);
        }
    }

    IEnumerator UpdateLocation()
    {
        yield return new WaitForSeconds(SYNC_CHECKTIME);
        yield return new WaitUntil(() => !trainCar.frontCoupler.IsCoupled());
        if (NetworkManager.IsHost() && !trainCar.isStationary)
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.SendTrainLocationUpdate(trainCar);
        }
        yield return UpdateLocation();
    }

    internal IEnumerator UpdateLocation(TrainLocation location)
    {
        location.Position = location.Position + WorldMover.currentMove;

        if (trainCar.derailed && !hostDerailed)
        {
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, location.Position, location.Forward);
        }
        else if (trainCar.derailed && hostDerailed)
        {
            transform.position = location.Position;
            transform.rotation = location.Rotation;
            trainCar.rb.angularVelocity = location.AngularVelocity;
            trainCar.transform.forward = location.Forward;
            yield break;
        }

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
        if (location.Velocity.z == 0)
        {
            
        }
        if (distance > 3f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z + .83f);
            isOutOfSync = true;
        }
        else if (distance <= 1f && distance > 0.1f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z + .28f);
            isOutOfSync = true;
        }
        else if (distance <= .1f && distance > 0.01f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z + .14f);
            isOutOfSync = true;
        }
        else if (distance < .01f && distance > -.01f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z);
            isOutOfSync = false;
        }
        else if (distance <= -.01f && distance > -0.1f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z - .14f);
            isOutOfSync = true;
        }
        else if (distance <= -.1f && distance > -1f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z - .28f);
            isOutOfSync = true;
        }
        else if (distance <= -1f && distance > -3f)
        {
            newVelocity = new Vector3(0, 0, location.Velocity.z - .83f);
            isOutOfSync = true;
        }
        if (isOutOfSync)
            Main.mod.Logger.Log($"{trainCar.ID} Is out of sync difference is {distance}m");
        trainCar.rb.angularVelocity = location.AngularVelocity;
        trainCar.transform.forward = location.Forward;
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
        if(car.frontCoupler.coupledTo != null)
        {
            return GetMostFrontCar(car.frontCoupler.coupledTo.train);
        }
        else
        {
            return car;
        }
    }
}
