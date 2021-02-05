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
    private Vector3 prevPosition;
    private const float SYNC_CHECKTIME = .5f;
    private TrainCar trainCar;
    private Vector3 prevVelocity = Vector3.zero;
    private Vector3 prevAngularVelocity = Vector3.zero;
    public bool hostDerailed;

    private void Awake()
    {
        Main.DebugLog($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();
        Main.DebugLog($"[{trainCar.ID}] NetworkTrainPosSync Awake called");
        Main.DebugLog($"Starting coroutine for location updating");
        SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());

        Main.DebugLog($"Listen to derailment events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
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

    IEnumerator UpdateLocation()
    {
        yield return new WaitForSeconds(SYNC_CHECKTIME);
        if (NetworkManager.IsHost())
        {
            yield return new WaitUntil(() => Vector3.Distance(prevPosition, transform.position) > 5f);
            prevAngularVelocity = trainCar.rb.angularVelocity;
            prevVelocity = trainCar.rb.velocity;
            prevPosition = transform.position;
            SingletonBehaviour<NetworkTrainManager>.Instance.SendTrainLocationUpdate(trainCar);
        }
        yield return UpdateLocation();
    }

    internal IEnumerator UpdateLocation(TrainLocation location)
    {
        location.Position = location.Position + WorldMover.currentMove;
        if (trainCar.frontCoupler.IsCoupled())
        {
            prevPosition = location.Position;
            yield break;
        }

        if (trainCar.derailed && !hostDerailed)
        {
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, location.Position, location.Forward);
        }
        else if(trainCar.derailed && hostDerailed)
        {
            transform.position = location.Position;
            transform.rotation = location.Rotation;
            prevPosition = location.Position;
            trainCar.rb.angularVelocity = location.AngularVelocity;
            trainCar.transform.forward = location.Forward;
            yield break;
        }

        if (Vector3.Distance(prevPosition, location.Position) > 5f && trainCar.rearCoupler.coupledTo == null)
        {
            transform.position = location.Position;
            transform.rotation = location.Rotation;
            transform.forward = location.Forward;
        }
        else if (trainCar.rearCoupler.coupledTo != null)
        {
            if (Distance(trainCar.transform, location.Position) > 3f)
            {
                trainCar.rb.velocity = location.Velocity * 1.5f;
                trainCar.rb.angularVelocity = location.AngularVelocity * 1.5f;
            }
            else if (Distance(trainCar.transform, location.Position) < 3f && Distance(trainCar.transform, location.Position) > 0.1f)
            {
                trainCar.rb.velocity = location.Velocity * 1.2f;
                trainCar.rb.angularVelocity = location.AngularVelocity * 1.2f;
            }
            else if (Distance(trainCar.transform, location.Position) < .1f && Distance(trainCar.transform, location.Position) > -0.1f)
            {
                trainCar.rb.velocity = location.Velocity;
                trainCar.rb.angularVelocity = location.AngularVelocity;
            }
            else if (Distance(trainCar.transform, location.Position) < -.1f && Distance(trainCar.transform, location.Position) > -1f)
            {
                trainCar.rb.velocity = location.Velocity * .8f;
                trainCar.rb.angularVelocity = location.AngularVelocity * .8f;
            }
            else if (Distance(trainCar.transform, location.Position) < -1f && Distance(trainCar.transform, location.Position) > -3f)
            {
                trainCar.rb.velocity = location.Velocity * .5f;
                trainCar.rb.angularVelocity = location.AngularVelocity * .5f;
            }
            trainCar.transform.forward = location.Forward;
        }
        prevPosition = location.Position;
    }

    private float Distance(Transform a, Vector3 b)
    {
        Vector3 relativePos = a.InverseTransformPoint(b);
        return relativePos.z;
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
