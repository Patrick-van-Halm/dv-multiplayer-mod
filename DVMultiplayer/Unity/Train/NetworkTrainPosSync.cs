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

    internal void UpdateLocation(TrainLocation location)
    {
        trainCar.rb.angularVelocity = location.AngularVelocity;
        trainCar.rb.velocity = location.Velocity;
        trainCar.transform.forward = location.Forward;

        for(int i = 0; i < location.AmountCars; i++)
        {
            trainCar.trainset.cars[i].transform.position = location.CarsPositions[i] + WorldMover.currentMove;
            trainCar.trainset.cars[i].transform.rotation = location.CarsRotation[i];
        }

        if(trainCar.derailed && !hostDerailed)
        {
            trainCar.derailed = false;
            trainCar.SetTrack(RailTrack.GetClosest(trainCar.transform.position).track, SingletonBehaviour<NetworkTrainManager>.Instance.CalculateWorldPosition(trainCar.transform.position + WorldMover.currentMove, trainCar.transform.forward, trainCar.Bounds.center.z), trainCar.transform.forward);
        }
    }
}
