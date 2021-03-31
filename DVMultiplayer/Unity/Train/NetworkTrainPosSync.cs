using DV.Logic.Job;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTrainPosSync : MonoBehaviour
{
    private TrainCar trainCar;
    internal WorldTrain serverState;
    public bool isOutOfSync = false;
    private Vector3 prevPos;
    private bool isStationary;

    //private bool hostStationary;
    private Vector3 newPos = Vector3.zero;
    private Quaternion newRot = Quaternion.identity;
    //internal bool isLocationApplied;
    public bool isDerailed;
    internal Vector3 velocity = Vector3.zero;
    public event Action<TrainCar> OnTrainCarInitialized;
    public bool hasLocalPlayerAuthority = false;
    internal bool resetAuthority = false;
    internal NetworkTurntableSync turntable = null;
    internal Coroutine authorityCoro = null;
    private float drag;
    private Coroutine damageEnablerCoro;
    public bool IsCarDamageEnabled { get; internal set; }
    NetworkPlayerSync localPlayer;
    ShunterLocoSimulation shunterLocoSimulation = null;
    ParticleSystem.MainModule shunterExhaust;

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
        localPlayer = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();

        isStationary = trainCar.isStationary;

        Main.Log($"Listening to movement changed event");
        trainCar.MovementStateChanged += TrainCar_MovementStateChanged;
        trainCar.CarDamage.CarEffectiveHealthStateUpdate += OnBodyDamageTaken;
        if (!trainCar.IsLoco)
            trainCar.CargoDamage.CargoDamaged += OnCargoDamageTaken;


        if(trainCar.carType == TrainCarType.LocoShunter)
        {
            shunterLocoSimulation = GetComponent<ShunterLocoSimulation>();
            shunterExhaust = trainCar.transform.Find("[particles]").Find("ExhaustEngineSmoke").GetComponent<ParticleSystem>().main;
        }

        if (!trainCar.IsLoco)
        {
            trainCar.CargoLoaded += OnCargoLoaded;
            trainCar.CargoUnloaded += OnCargoUnloaded;
        }

        //for(int i = 0; i < trainCar.Bogies.Length; i++)
        //{
        //    bogieAudios[i] = trainCar.Bogies[i].GetComponent<BogieAudioController>();
        //}

        if (NetworkManager.IsHost())
        {
            if (trainCar.IsLoco)
            {
                authorityCoro = SingletonBehaviour<CoroutineManager>.Instance.Run(CheckAuthorityChange());
            }
            trainCar.TrainsetChanged += TrainCar_TrainsetChanged;
            SetAuthority(true);
        }
        else
        {
            SetAuthority(false);
        }
    }

    private void TrainCar_TrainsetChanged(Trainset set)
    {
        //Issue with trainset being detatched in the middle positioning not updating correctly.
        if (set.locoIndices.Count == 0 && set.firstCar == trainCar)
            StartCoroutine(ResetAuthorityToHostWhenStationary(set));
    }

    private IEnumerator ResetAuthorityToHostWhenStationary(Trainset set)
    {
        yield return new WaitUntil(() => velocity.magnitude * 3.6f < 1);
        SingletonBehaviour<NetworkTrainManager>.Instance.SendAuthorityChange(set, localPlayer.Id);
    }

    private void OnCargoUnloaded()
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.CargoStateChanged(trainCar, CargoType.None, false);
    }

    private void OnCargoLoaded(CargoType type)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.CargoStateChanged(trainCar, type, true);
    }

    private IEnumerator CheckAuthorityChange()
    {
        while (NetworkManager.IsHost())
        {
            yield return new WaitForSeconds(.1f);

            if (serverState != null && SingletonBehaviour<NetworkPlayerManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Exists && trainCar && trainCar.logicCar != null)
            {
                try
                {
                    if (turntable == null || turntable != null && !turntable.IsAnyoneInControlArea)
                    {
                        bool authNeedsChange = false;
                        GameObject player = null;
                        NetworkPlayerManager playerManager = SingletonBehaviour<NetworkPlayerManager>.Instance;

                        if (!resetAuthority)
                        {
                            GameObject ply;
                            if (!hasLocalPlayerAuthority)
                                ply = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerById(serverState.AuthorityPlayerId);
                            else
                                ply = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();

                            if (ply)
                            {
                                TrainCar car = ply.GetComponent<NetworkPlayerSync>().Train;
                                if (!car && velocity.magnitude * 3.6f < 1)
                                {
                                    GameObject[] playersInLoco = playerManager.GetPlayersInTrain(trainCar);
                                    if (playersInLoco.Length > 0)
                                    {
                                        player = playersInLoco[0];
                                        authNeedsChange = true;
                                    }
                                    if (!player)
                                    {
                                        foreach (int locoId in trainCar.trainset.locoIndices)
                                        {
                                            TrainCar loco = trainCar.trainset.cars[locoId];
                                            if (!loco || loco == trainCar) continue;
                                            playersInLoco = playerManager.GetPlayersInTrain(loco);
                                            if (playersInLoco.Length > 0)
                                            {
                                                player = playersInLoco[0];
                                                authNeedsChange = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (car && !trainCar.trainset.cars.Contains(car) && velocity.magnitude * 3.6f < 1)
                                {
                                    authNeedsChange = true;
                                    GameObject[] playersInLoco = playerManager.GetPlayersInTrain(trainCar);
                                    if (playersInLoco.Length > 0)
                                    {
                                        player = playersInLoco[0];
                                        authNeedsChange = true;
                                    }
                                    if (!player)
                                    {
                                        foreach (int locoId in trainCar.trainset.locoIndices)
                                        {
                                            TrainCar loco = trainCar.trainset.cars[locoId];
                                            if (!loco || loco == trainCar) continue;
                                            playersInLoco = playerManager.GetPlayersInTrain(loco);
                                            if (playersInLoco.Length > 0)
                                            {
                                                player = playersInLoco[0];
                                                authNeedsChange = true;
                                                break;
                                            }
                                        }

                                        if (!player)
                                            player = playerManager.GetLocalPlayer();
                                    }
                                }
                            }
                            else
                            {
                                player = playerManager.GetLocalPlayer();
                                authNeedsChange = true;
                            }
                        }
                        else
                        {
                            authNeedsChange = true;
                            GameObject[] playersInLoco = playerManager.GetPlayersInTrain(trainCar);
                            if (playersInLoco.Length > 0)
                            {
                                player = playersInLoco[0];
                            }
                            if (!player)
                            {
                                foreach (int locoId in trainCar.trainset.locoIndices)
                                {
                                    TrainCar loco = trainCar.trainset.cars[locoId];
                                    if (!loco || loco == trainCar) continue;
                                    playersInLoco = playerManager.GetPlayersInTrain(loco);
                                    if (playersInLoco.Length > 0)
                                    {
                                        player = playersInLoco[0];
                                        break;
                                    }
                                }
                                if (!player)
                                    player = playerManager.GetLocalPlayer();
                            }
                        }

                        if (authNeedsChange)
                        {
                            if ((player.GetComponent<NetworkPlayerSync>() && player.GetComponent<NetworkPlayerSync>().Id != serverState.AuthorityPlayerId) || resetAuthority)
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
                catch(Exception ex)
                {
                    Main.Log($"Exception thrown in authority check. {ex.Message}");
                }
            }
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        if(authorityCoro != null)
            SingletonBehaviour<CoroutineManager>.Instance.Stop(authorityCoro);

        trainCar.MovementStateChanged -= TrainCar_MovementStateChanged;
        trainCar.CarDamage.CarEffectiveHealthStateUpdate -= OnBodyDamageTaken;

        if (!trainCar.IsLoco)
        {
            trainCar.CargoDamage.CargoDamaged -= OnCargoDamageTaken;
            trainCar.CargoLoaded -= OnCargoLoaded;
            trainCar.CargoUnloaded -= OnCargoUnloaded;
        }

        Main.Log($"NetworkTrainPosSync.OnDestroy()");
    }

    private void Update()
    {
        if (!SingletonBehaviour<NetworkPlayerManager>.Exists || !SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting)
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
            if (!hasLocalPlayerAuthority && !willLocalPlayerGetAuthority)
            {
                float increment = (velocity.magnitude * 3f);
                if (increment <= 5f && turntable)
                    increment = 5;

                if (increment <= 5f && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > 1 && isDerailed)
                    increment = 5;

                if (increment <= 5f && Vector3.Distance(transform.position - WorldMover.currentMove, newPos) > 10)
                    increment = 5;

                float step = increment * Time.deltaTime; // calculate distance to move
                if (newPos != Vector3.zero && Vector3.Distance(transform.position, newPos + WorldMover.currentMove) > Mathf.Lerp(1e-3f, .25f, velocity.magnitude * 3.6f / 80))
                {
                    foreach (Bogie b in trainCar.Bogies)
                    {
                        b.rb.isKinematic = true;
                    }
                    trainCar.rb.MovePosition(Vector3.MoveTowards(transform.position, newPos + WorldMover.currentMove, step));
                    foreach (Bogie b in trainCar.Bogies)
                    {
                        b.ResetBogiesToStartPosition();
                        b.rb.isKinematic = false;
                    }
                }

                if (newRot != Quaternion.identity && Quaternion.Angle(transform.rotation, newRot) > 1e-3f)
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

            if (hasLocalPlayerAuthority)
            {
                velocity = trainCar.rb.velocity;
                isStationary = trainCar.isStationary;
            }

            if (willLocalPlayerGetAuthority && !hasLocalPlayerAuthority)
            {
                Main.Log($"Car {trainCar.CarGUID}: Changing authority [GAINED]");
                SetAuthority(true);
                if (!trainCar.IsInteriorLoaded)
                {
                    trainCar.LoadInterior();
                }
                trainCar.keepInteriorLoaded = true;
            }
            else if (!willLocalPlayerGetAuthority && hasLocalPlayerAuthority)
            {
                Main.Log($"Car {trainCar.CarGUID}: Changing authority [RELEASED]");
                SetAuthority(false);
                newPos = transform.position - WorldMover.currentMove;
                newRot = transform.rotation;
                trainCar.keepInteriorLoaded = false;
            }
        }
        catch (Exception ex)
        {
            Main.Log($"NetworkTrainPosSync threw an exception: {ex.Message} inner exception: {ex.InnerException}");
        }


        //if (!hasLocalPlayerAuthority)
        //{
        //    trainCar.rb.MovePosition(newPos);
        //    trainCar.rb.MoveRotation(newRot);
        //}
    }

    private void SetAuthority(bool gain)
    {
        Main.Log($"Setting authority");
        hasLocalPlayerAuthority = gain;
        Main.Log($"Set kinematic state {!gain}");
        trainCar.rb.isKinematic = !gain;

        Main.Log($"Start position updater");
        StartCoroutine(UpdateLocation());

        if (trainCar.carType == TrainCarType.LocoShunter)
        {
            shunterExhaust.emitterVelocityMode = gain ? ParticleSystemEmitterVelocityMode.Rigidbody : ParticleSystemEmitterVelocityMode.Transform;
        }

        //if (gain)
        //{
        //    foreach(Bogie b in trainCar.Bogies)
        //    {
        //        b.
        //        b.ForceSleep(true);
        //        b.ResetBogiesToStartPosition();
        //    }
        //}

        Main.Log($"Toggle damage for 2 seconds");
        damageEnablerCoro = StartCoroutine(ToggleDamageAfterSeconds(2));
        Main.Log($"Resync train");
        SingletonBehaviour<NetworkTrainManager>.Instance.ResyncCar(trainCar);
    }
#pragma warning restore IDE0051 // Remove unused private members

    private IEnumerator ToggleDamageAfterSeconds(float seconds)
    {
        IgnoreDamage(true);
        trainCar.stress.EnableStress(false);
        trainCar.TrainCarCollisions.enabled = false;
        if (!hasLocalPlayerAuthority)
        {
            yield break;
        }
        trainCar.CarDamage.IgnoreDamage(true);
        yield return new WaitForSeconds(seconds);
        if (!turntable)
        {
            trainCar.stress.EnableStress(true);
            trainCar.TrainCarCollisions.enabled = true;
            IgnoreDamage(false);
        }

        damageEnablerCoro = null;
    }

    private void IgnoreDamage(bool set)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                trainCar.GetComponent<DamageControllerShunter>().IgnoreDamage(set);
                break;
        }
    }

    private IEnumerator ToggleKinematic(float seconds)
    {
        trainCar.rb.isKinematic = true;
        trainCar.rb.Sleep();
        trainCar.stress.EnableStress(false);
        //foreach (Bogie bogie in trainCar.Bogies)
        //{
        //    bogie.RerailInitialize();
        //    bogie.ResetBogiesToStartPosition();
        //}
        yield return new WaitForSeconds(seconds);
        if (hasLocalPlayerAuthority)
        {
            trainCar.rb.isKinematic = false;
            trainCar.stress.EnableStress(true);
        }
    }

    private void TrainCar_MovementStateChanged(bool isMoving)
    {
        if (!hasLocalPlayerAuthority)
            return;

        Main.Log($"Movement state changed is moving: {isMoving}");
        if(!isMoving && SingletonBehaviour<NetworkTrainManager>.Exists)
        {
            trainCar.stress.EnableStress(false);
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
        if (!hasLocalPlayerAuthority)
        {
            StartCoroutine(ToggleKinematic(2));
            newPos = transform.position - WorldMover.currentMove;
            newRot = transform.rotation;
        }
        else
        {
            prevPos = transform.position - WorldMover.currentMove;
        }

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
        if (serverState is null)
            return;

        if ((!hasLocalPlayerAuthority || !IsCarDamageEnabled && hasLocalPlayerAuthority) && Math.Round(trainCar.CarDamage.currentHealth, 2) != Math.Round(serverState.CarHealth, 2))
            trainCar.CargoDamage.LoadCargoDamageState(serverState.CargoHealth);

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || !IsCarDamageEnabled)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Cargo, trainCar.CargoDamage.currentHealth, "");
    }

    private void OnBodyDamageTaken(float _)
    {
        if (serverState is null)
            return;

        if ((!hasLocalPlayerAuthority || !IsCarDamageEnabled && hasLocalPlayerAuthority) && Math.Round(trainCar.CarDamage.currentHealth, 2) != Math.Round(serverState.CarHealth, 2))
        {
            if (trainCar.IsLoco)
                LoadLocoDamage(serverState.CarHealthData);
            else
                trainCar.CarDamage.LoadCarDamageState(serverState.CarHealth);
        }

        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority || !IsCarDamageEnabled)
            return;


        string data = "";
        if (trainCar.IsLoco)
        {
            switch (trainCar.carType)
            {
                case TrainCarType.LocoShunter:
                    data = trainCar.GetComponent<DamageControllerShunter>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                    break;
            }
        }

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarDamaged(trainCar.CarGUID, DamageType.Car, trainCar.CarDamage.currentHealth, data);
    }

    internal void LoadLocoDamage(string carHealthData)
    {
        switch(trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                trainCar.GetComponent<DamageControllerShunter>().LoadDamagesState(JObject.Parse(carHealthData));
                break;
        }
    }

    private IEnumerator UpdateLocation()
    {
        while (hasLocalPlayerAuthority && !trainCar.frontCoupler.coupledTo)
        {
            yield return new WaitForSeconds(.005f);
            yield return new WaitUntil(() => Vector3.Distance(transform.position - WorldMover.currentMove, prevPos) > Mathf.Lerp(1e-3f, .25f, velocity.magnitude * 3.6f / 80) && !trainCar.isStationary);
            if(!trainCar.stress.enabled)
                trainCar.stress.EnableStress(true);
            //foreach (Bogie b in trainCar.Bogies)
            //{
            //    if (b.rb.IsSleeping())
            //        b.ForceSleep(false);
            //}
            SingletonBehaviour<NetworkTrainManager>.Instance.SendCarLocationUpdate(trainCar);
            prevPos = transform.position - WorldMover.currentMove;

            if (!turntable && !IsCarDamageEnabled)
            {
                trainCar.CarDamage.IgnoreDamage(false);
            }
        }
    }

    internal void UpdateLocation(TrainLocation location)
    {
        StartCoroutine(CoroUpdateLocation(location));
    }

    internal IEnumerator CoroUpdateLocation(TrainLocation location)
    {
        if (hasLocalPlayerAuthority)
            yield break;

        velocity = location.Velocity;
        drag = location.Drag;

        if (trainCar.derailed && !isDerailed)
        {
            yield return SingletonBehaviour<NetworkTrainManager>.Instance.RerailDesynced(trainCar, serverState, true);
        }

        isStationary = location.IsStationary;
        newPos = location.Position;
        newRot = location.Rotation;
        if (trainCar.IsLoco)
        {
            switch (trainCar.carType)
            {
                case TrainCarType.LocoShunter:
                    shunterLocoSimulation.engineTemp.SetValue(location.Temperature);
                    shunterLocoSimulation.engineRPM.SetValue(location.RPM);
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
