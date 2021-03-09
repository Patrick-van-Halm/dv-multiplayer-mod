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
    public bool isOutOfSync = false;
    private Vector3 prevPos;
    //private bool hostStationary;
    private bool isStationary;
    private Vector3 newPos = Vector3.zero;
    private Quaternion newRot = Quaternion.identity;
    //internal bool isLocationApplied;
    public bool isDerailed;
    internal Vector3 velocity = Vector3.zero;
    public event Action<TrainCar> OnTrainCarInitialized;
    public bool hasLocalPlayerAuthority = false;
    internal bool resetAuthority = false;
    internal NetworkTurntableSync turntable = null;
    internal bool overrideDamageDisabled = false;
    internal Coroutine positionCoro;
    internal Coroutine authorityCoro = null;
    private float drag;
    private Coroutine damageEnablerCoro;
    public bool IsCarDamageEnabled { get; internal set; }
    NetworkPlayerSync localPlayer;
    ShunterLocoSimulation shunterLocoSimulation = null;

    //private TrainAudio trainAudio;
    //private BogieAudioController[] bogieAudios;

#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainPosSync.Awake()");
        trainCar = GetComponent<TrainCar>();

        //bogieAudios = new BogieAudioController[trainCar.Bogies.Length];

        Main.Log($"[{trainCar.ID}] NetworkTrainPosSync Awake called");

        Main.Log($"Listening to derailment/rerail events");
        trainCar.OnDerailed += TrainDerail;
        trainCar.OnRerailed += TrainRerail;
        Main.Log($"Listening to LogicCar loaded event");
        trainCar.LogicCarInitialized += TrainCar_LogicCarInitialized;

        trainCar.CarDamage.IgnoreDamage(true);
        trainCar.stress.enabled = false;
        trainCar.TrainCarCollisions.enabled = false;

        Main.Log($"Set kinematic");
        trainCar.rb.isKinematic = true;
        IsCarDamageEnabled = false;
        localPlayer = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();

        Main.Log($"Listening to movement changed event");
        trainCar.MovementStateChanged += TrainCar_MovementStateChanged;
        trainCar.CarDamage.CarEffectiveHealthStateUpdate += OnBodyDamageTaken;
        if (!trainCar.IsLoco)
            trainCar.CargoDamage.CargoDamaged += OnCargoDamageTaken;

        shunterLocoSimulation = GetComponent<ShunterLocoSimulation>();

        //for(int i = 0; i < trainCar.Bogies.Length; i++)
        //{
        //    bogieAudios[i] = trainCar.Bogies[i].GetComponent<BogieAudioController>();
        //}

        if (NetworkManager.IsHost())
        {
            authorityCoro = SingletonBehaviour<CoroutineManager>.Instance.Run(CheckAuthorityChange());
        }
    }

    private IEnumerator CheckAuthorityChange()
    {
        while (NetworkManager.IsHost())
        {
            yield return new WaitForSeconds(.1f);
            if (serverState != null)
            {
                if (turntable == null)
                {
                    bool authNeedsChange = true;
                    if (!resetAuthority)
                    {
                        foreach (TrainCar car in trainCar.trainset.cars)
                        {
                            if (!car)
                                continue;

                            if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(car).Select(p => p.GetComponent<NetworkPlayerSync>().Id).Contains(serverState.AuthorityPlayerId))
                            {
                                authNeedsChange = false;
                                break;
                            }
                        }
                    }

                    if (authNeedsChange)
                    {
                        GameObject player = null;
                        foreach (TrainCar car in trainCar.trainset.cars)
                        {
                            if (!car)
                                continue;

                            if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(car).Length > 0)
                                player = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(car)[0];
                        }

                        if (!player)
                            player = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();

                        if (player.GetComponent<NetworkPlayerSync>().Id != serverState.AuthorityPlayerId || resetAuthority)
                            SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, player.GetComponent<NetworkPlayerSync>().Id);

                        resetAuthority = false;
                    }
                }
                else
                {
                    if (serverState.AuthorityPlayerId != turntable.playerAuthId)
                        SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(trainCar.trainset, turntable.playerAuthId);
                }
            }
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        if(authorityCoro != null)
            SingletonBehaviour<CoroutineManager>.Instance.Stop(authorityCoro);
        Main.Log($"NetworkTrainPosSync.OnDestroy()");
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

        //if(trainAudio == null)
        //{
        //    trainAudio = trainCar.GetComponentInChildren<TrainAudio>();
        //    return;
        //}

        bool willLocalPlayerGetAuthority = localPlayer.Id == serverState.AuthorityPlayerId;

        if (overrideDamageDisabled && IsCarDamageEnabled)
        {
            Main.Log($"Ignoring damage on train {trainCar.CarGUID}");
            trainCar.CarDamage.IgnoreDamage(true);
            trainCar.stress.enabled = false;
            trainCar.TrainCarCollisions.enabled = false;
        }
        else if(!overrideDamageDisabled && !IsCarDamageEnabled && damageEnablerCoro == null && (hasLocalPlayerAuthority || (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)))
        {
            Main.Log($"Accepting damage on train {trainCar.CarGUID}");
            trainCar.stress.enabled = true;
            trainCar.stress.DisableStressCheckForTwoSeconds();
            trainCar.TrainCarCollisions.enabled = true;
            damageEnablerCoro = StartCoroutine(ToggleDamageAfterSeconds(1));
        }

        //if (!(hasLocalPlayerAuthority || (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)))
        //{
        //    trainAudio.frictionAudio?.Stop();
        //    foreach (BogieAudioController bogieAudio in bogieAudios)
        //    {
        //        bogieAudio.SetLOD(AudioLOD.NONE);
        //    }
        //}


        try
        {
            if (!hasLocalPlayerAuthority)
            {
                float increment = (velocity.magnitude * 3.6f);
                float step = (increment + 5) * Time.deltaTime; // calculate distance to move
                if (newPos != Vector3.zero && Vector3.Distance(transform.position, newPos + WorldMover.currentMove) >= .05)
                {
                    //Main.Log($"Moving train");
                    trainCar.rb.MovePosition(Vector3.MoveTowards(transform.position, newPos + WorldMover.currentMove, step));
                }

                if (newRot != Quaternion.identity && Quaternion.Angle(transform.rotation, newRot) >= .1)
                {
                    //Main.Log($"Rotating train");
                    if (!turntable)
                    {
                        trainCar.rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, newRot, step));
                    }
                    else
                    {
                        trainCar.rb.MoveRotation(newRot);
                    }
                }
            }

            if (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)
            {
                Main.Log($"Changing authority [GAINED]");
                SetAuthority(true);
            }
            else if (!willLocalPlayerGetAuthority && hasLocalPlayerAuthority)
            {
                Main.Log($"Changing authority [RELEASED]");
                SetAuthority(false);
                newPos = transform.position - WorldMover.currentMove;
                newRot = transform.rotation;
            }
        }
        catch (Exception ex)
        {
            Main.Log(ex.Message);
        }


        //if (!hasLocalPlayerAuthority)
        //{
        //    trainCar.rb.MovePosition(newPos);
        //    trainCar.rb.MoveRotation(newRot);
        //}

        if (trainCar.rb.isKinematic && isDerailed)
        {
            Main.Log($"Apply physics on derailed train");
            trainCar.rb.isKinematic = false;
        }
    }

    private void SetAuthority(bool gain)
    {
        Main.Log($"Setting authority");
        hasLocalPlayerAuthority = gain;
        //Main.Log($"Set bogies non kinematic");
        //foreach (Bogie bogie in trainCar.Bogies)
        //{
        //    if (bogie.rb == null)
        //    {
        //        bogie.RefreshBogiePoints();
        //    }
        //    bogie.rb.isKinematic = false;
        //}
        Main.Log($"Set kinematic state {!gain}");
        trainCar.rb.isKinematic = !gain;
        Main.Log($"Set velocity and drag");
        trainCar.rb.velocity = velocity;
        trainCar.rb.drag = drag;

        Main.Log($"Resync train");
        SingletonBehaviour<NetworkTrainManager>.Instance.ResyncCar(trainCar);
        Main.Log($"Toggle damage for 2 seconds");
        damageEnablerCoro = StartCoroutine(ToggleDamageAfterSeconds(2));
        StartCoroutine(UpdateLocation());
    }
#pragma warning restore IDE0051 // Remove unused private members

    private IEnumerator ToggleDamageAfterSeconds(float seconds)
    {
        if (!hasLocalPlayerAuthority)
        {
            trainCar.CarDamage.IgnoreDamage(true);
            trainCar.stress.enabled = false;
            trainCar.TrainCarCollisions.enabled = false;
            yield break;
        }
        
        trainCar.CarDamage.IgnoreDamage(true);
        trainCar.stress.DisableStressCheckForTwoSeconds();
        yield return new WaitForSeconds(seconds);
        if (!turntable)
        {
            trainCar.CarDamage.IgnoreDamage(false);
        }

        damageEnablerCoro = null;
    }

    private void TrainCar_MovementStateChanged(bool isMoving)
    {
        if (!hasLocalPlayerAuthority)
            return;

        Main.Log($"Movement state changed is moving: {isMoving}");
        if(!isMoving)
        {
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

    private void OnCargoDamageTaken(float _)
    {
        if (overrideDamageDisabled && hasLocalPlayerAuthority)
            Main.Log($"Cargo took damage but should be ignored");

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || overrideDamageDisabled)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Cargo, trainCar.CargoDamage.currentHealth);
    }

    private void OnBodyDamageTaken(float _)
    {
        if(overrideDamageDisabled && hasLocalPlayerAuthority)
            Main.Log($"Train took damage but should be ignored");

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || overrideDamageDisabled)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Car, trainCar.CarDamage.currentHealth);
    }

    private IEnumerator UpdateLocation()
    {
        while (hasLocalPlayerAuthority && !trainCar.frontCoupler.coupledTo)
        {
            yield return new WaitUntil(() => Vector3.Distance(transform.position - WorldMover.currentMove, prevPos) > 0f);
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar);
            prevPos = transform.position - WorldMover.currentMove;
        }
    }

    internal IEnumerator UpdateLocation(TrainLocation location)
    {
        if (hasLocalPlayerAuthority)
            yield break;

        velocity = location.Velocity;
        drag = location.Drag;

        if (trainCar.derailed && !isDerailed)
        {
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, serverState, true);
        }
        else if (trainCar.derailed && isDerailed)
        {
            location.Position += WorldMover.currentMove;
            trainCar.transform.position = location.Position;
            trainCar.transform.rotation = location.Rotation;
            trainCar.transform.forward = location.Forward;
            yield break;
        }
        //location.Position += WorldMover.currentMove;

        //for (int i = 0; i < location.Bogies.Length; i++)
        //{
        //    TrainBogie bogie = location.Bogies[i];
        //    trainCar.Bogies[i].transform.position = bogie.Position;
        //    trainCar.Bogies[i].transform.rotation = bogie.Rotation;
        //}

        //isLocationApplied = false;
        isStationary = location.IsStationary;
        newPos = location.Position;
        newRot = location.Rotation;
        if (trainCar.IsLoco)
        {
            switch (trainCar.carType)
            {
                case TrainCarType.LocoShunter:
                    shunterLocoSimulation.engineTemp.SetValue(location.Temperature);
                    break;
            }
        }
        
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

    //private float Distance(Transform a, Vector3 b)
    //{
    //    Vector3 forward = a.TransformDirection(a.forward);
    //    Vector3 toOther = b - a.position;
    //    if (Vector3.Dot(forward, toOther) < 0)
    //        return -Vector3.Distance(a.position, b);
    //    else
    //        return Vector3.Distance(a.position, b);
    //}

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
