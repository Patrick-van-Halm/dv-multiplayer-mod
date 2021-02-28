﻿using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Turntable;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTurntableSync : MonoBehaviour
{
    private TurntableController turntable;
    private TurntableControlKeyboardInput keyboardInput;
    private float prevRotation;
    private List<TrainCar> carsOnTurntable = new List<TrainCar>();
    private bool hasLocalPlayerAuthority = false;
    internal Turntable serverState = null;
    internal ushort playerAuthId = 0;

    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        turntable.Snapped += Turntable_Snapped;
        prevRotation = turntable.turntable.currentYRotation;
        SingletonBehaviour<CoroutineManager>.Instance.Run(DisableKeyboardInput());
        if (NetworkManager.IsHost())
        {
            SingletonBehaviour<CoroutineManager>.Instance.Run(CheckAuthorityChange());
        }
    }

    private IEnumerator CheckAuthorityChange()
    {
        yield return new WaitForSeconds(.1f);
        GameObject newAuthorityPlayer = null;
        if (keyboardInput)
        {
            Vector3 position = PlayerManager.PlayerTransform.position;
            if (keyboardInput.interactionAreaTrigger.ClosestPoint(position) == position)
            {
                newAuthorityPlayer = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayer();
            }

            if (!newAuthorityPlayer)
            {
                foreach (GameObject player in SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers())
                {
                    position = player.transform.position;
                    if (keyboardInput.interactionAreaTrigger.ClosestPoint(position) == position)
                    {
                        newAuthorityPlayer = player;
                        break;
                    }
                }
            }

            if (newAuthorityPlayer)
            {
                if (playerAuthId != newAuthorityPlayer.GetComponent<NetworkPlayerSync>().Id)
                {
                    playerAuthId = newAuthorityPlayer.GetComponent<NetworkPlayerSync>().Id;
                    SingletonBehaviour<NetworkTurntableManager>.Instance.SendRequestAuthority(turntable, playerAuthId);
                }
            }
            else
            {
                if(playerAuthId != 0)
                {
                    SingletonBehaviour<NetworkTurntableManager>.Instance.SendReleaseAuthority(turntable);
                    playerAuthId = 0;
                }
            }
        }
        yield return CheckAuthorityChange();
    }

    private void Update()
    {
        if (!SingletonBehaviour<NetworkTurntableManager>.Instance)
            return;

        List<TrainCar> currentCarsOnTurntable = new List<TrainCar>();
        foreach(Bogie bogie in turntable.turntable.Track.onTrackBogies)
        {
            if (!currentCarsOnTurntable.Contains(bogie.Car))
            {
                bogie.Car.GetComponent<NetworkTrainPosSync>().overrideDamageDisabled = true;
                if(turntable.turntable.Track.onTrackBogies.Contains(bogie.Car.Bogies[0]) && turntable.turntable.Track.onTrackBogies.Contains(bogie.Car.Bogies[bogie.Car.Bogies.Length - 1]))
                    currentCarsOnTurntable.Add(bogie.Car);
            }
        }

        foreach (TrainCar car in carsOnTurntable.ToList())
        {
            if(currentCarsOnTurntable.Count == 0 || !carsOnTurntable.Contains(car))
            {
                carsOnTurntable.Remove(car);
                if(car.logicCar != null)
                {
                    car.GetComponent<NetworkTrainPosSync>().turntable = null;
                    Main.Log($"Train: {car.CarGUID} left turntable");
                    SingletonBehaviour<CoroutineManager>.Instance.Run(EnableDamageAfterSeconds(car, .3f));
                }
            }
        }

        hasLocalPlayerAuthority = playerAuthId == SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id;

        foreach (TrainCar car in currentCarsOnTurntable)
        {
            if (currentCarsOnTurntable.Count == 0 || !carsOnTurntable.Contains(car))
            {
                if (car.logicCar != null)
                {
                    car.GetComponent<NetworkTrainPosSync>().overrideDamageDisabled = true;
                    car.GetComponent<NetworkTrainPosSync>().turntable = this;
                    Main.Log($"Train: {car.CarGUID} entered turntable");
                    carsOnTurntable.Add(car);
                    //car.CarDamage.IgnoreDamage(true);
                }
            }

            //if (!hasLocalPlayerAuthority)
            //{
            //    if (!car.rb.isKinematic)
            //    {
            //        Main.Log("You're not controlling the turntable so setting trains physics off");
            //        car.rb.isKinematic = true;
            //    }
            //}
            //else if(hasLocalPlayerAuthority)
            //{
            //    if (car.rb.isKinematic)
            //    {
            //        Main.Log("You're controlling the turntable so setting trains physics on");
            //        car.rb.isKinematic = false;
            //    }
            //}
        }

        if (SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork || !hasLocalPlayerAuthority)
        {
            prevRotation = turntable.turntable.currentYRotation;
            return;
        }

        if (turntable.turntable.currentYRotation != prevRotation)
        {
            if (!SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork && SingletonBehaviour<NetworkTurntableManager>.Instance.IsSynced)
                SendRotationChange();
            prevRotation = turntable.turntable.currentYRotation;
        }
    }

    private IEnumerator EnableDamageAfterSeconds(TrainCar car, float time)
    {
        yield return new WaitForSeconds(time);
        car.GetComponent<NetworkTrainPosSync>().overrideDamageDisabled = false;
    }

    private void SendRotationChange()
    {
        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, turntable.turntable.currentYRotation);
    }

    private IEnumerator DisableKeyboardInput()
    {
        yield return new WaitUntil(() =>
        {
            keyboardInput = turntable.GetComponentInChildren<TurntableControlKeyboardInput>();
            return keyboardInput;
        });
        keyboardInput.enabled = false;
    }

    private void OnDestroy()
    {
        //SingletonBehaviour<CoroutineManager>.Instance.Stop(coroutineInputLever);
        turntable.Snapped -= Turntable_Snapped;
        if (keyboardInput)
            keyboardInput.enabled = true;
    }

    private void Turntable_Snapped()
    {
        if (!SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork && hasLocalPlayerAuthority)
            SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableSnap(turntable, turntable.turntable.currentYRotation);
    }

    //private IEnumerator CheckInputLever()
    //{
    //    yield return new WaitUntil(() =>
    //    {
    //        return (lever.Value < .45f || lever.Value > .55f) && lever.IsGrabbedOrHoverScrolled();
    //    });
    //    yield return new WaitUntil(() =>
    //    {
    //        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
    //        return lever.Value > .45f && lever.Value < .55f && !lever.IsGrabbedOrHoverScrolled();
    //    });
    //    yield return CheckInputLever();
    //}

    //private void OnTurntableLeverAngleChanged(float value)
    //{
    //    if (prevLeverAngle != value)
    //    {
    //        prevLeverAngle = value;
    //        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, value, true);
    //    }
    //}

    //private void OnTurntableRotationChanged(float targetYRotation)
    //{
    //    SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, targetYRotation, false);
    //}
}