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
        if (trainCar.derailed && !hostDerailed)
        {
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, location.Position, location.Forward);
        }

        if (Vector3.Distance(prevPosition, location.Position) > 5f && trainCar.frontCoupler.coupledTo == null && trainCar.rearCoupler.coupledTo == null)
        {
            transform.position = location.Position;
            transform.rotation = location.Rotation;
        }

        if (trainCar.frontCoupler.coupledTo != null || trainCar.rearCoupler.coupledTo != null)
        {
            if (Vector3.Distance(prevPosition, location.Position) > 3f)
            {
                trainCar.rb.velocity = location.Velocity * 1.5f;
            }
            else if (Vector3.Distance(prevPosition, location.Position) < 3f && Vector3.Distance(prevPosition, location.Position) > 0.1f)
            {
                trainCar.rb.velocity = location.Velocity * 1.2f;
            }
            else if (Vector3.Distance(prevPosition, location.Position) < .1f && Vector3.Distance(prevPosition, location.Position) > -0.1f)
            {
                trainCar.rb.velocity = location.Velocity;
            }
            else if (Vector3.Distance(prevPosition, location.Position) < -.1f && Vector3.Distance(prevPosition, location.Position) > -1f)
            {
                trainCar.rb.velocity = location.Velocity * .8f;
            }
            else if (Vector3.Distance(prevPosition, location.Position) < -1f && Vector3.Distance(prevPosition, location.Position) > -3f)
            {
                trainCar.rb.velocity = location.Velocity * .5f;
            }
        }
        prevPosition = location.Position;
        trainCar.rb.angularVelocity = location.AngularVelocity;
        trainCar.transform.forward = location.Forward;
        
        //for(int i = 0; i < location.AmountCars; i++)
        //{
        //    trainCar.trainset.cars[i].transform.position = location.CarsPositions[i] + WorldMover.currentMove;
        //    trainCar.trainset.cars[i].transform.rotation = location.CarsRotation[i];
        //}
    }
}
