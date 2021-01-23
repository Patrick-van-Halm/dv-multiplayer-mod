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
        trainCar = GetComponent<TrainCar>();
        SingletonBehaviour<CoroutineManager>.Instance.Run(UpdateLocation());

        trainCar.OnDerailed += TrainDerail;
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
            if (Vector3.Distance(prevPosition, transform.position) > 5f)
            {
                prevAngularVelocity = trainCar.rb.angularVelocity;
                prevVelocity = trainCar.rb.velocity;

                prevPosition = transform.position;
                SingletonBehaviour<NetworkTrainManager>.Instance.SendTrainLocationUpdate(trainCar);
            }
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
            trainCar.trainset.cars[i].transform.position = location.CarsPositions[i];
            trainCar.trainset.cars[i].transform.rotation = location.CarsRotation[i];
        }

        if(trainCar.derailed && !hostDerailed)
        {
            trainCar.derailed = false;
            trainCar.SetTrack(RailTrack.GetClosest(trainCar.transform.position).track, CalculateWorldPosition(trainCar.transform.position, trainCar.transform.forward, trainCar.Bounds.center.z), trainCar.transform.forward);
        }
    }

    private Vector3 CalculateWorldPosition(Vector3 position, Vector3 forward, float zBounds)
    {
        return position + forward * zBounds;
    }
}
