using DV.CabControls;
using DVMultiplayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTurntableSync : MonoBehaviour
{
    private TurntableController turntable;
    private LeverBase lever;
    private float prevLeverAngle;
    private Transform playerCameraTransform;
    private TurntableControlKeyboardInput keyboardInput;
    private Coroutine coroutineInputLever;
    private readonly List<TrainCar> trainsOnTurntable = new List<TrainCar>();
    private float prevRotation;
    private List<TrainCar> carsOnTurntable = new List<TrainCar>();
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        lever = turntable.leverGO.GetComponent<LeverBase>();
        playerCameraTransform = PlayerManager.PlayerCamera.transform;
        //coroutineInputLever = SingletonBehaviour<CoroutineManager>.Instance.Run(CheckInputLever());
        //turntable.Snapped += Turntable_Snapped;
        prevRotation = turntable.turntable.currentYRotation;
        SingletonBehaviour<CoroutineManager>.Instance.Run(DisableKeyboardInput());
    }

    private void Update()
    {
        List<TrainCar> currentCarsOnTurntable = new List<TrainCar>();
        foreach(Bogie bogie in turntable.turntable.Track.onTrackBogies)
        {
            if(!currentCarsOnTurntable.Contains(bogie.Car))
                currentCarsOnTurntable.Add(bogie.Car);
        }

        foreach(TrainCar car in carsOnTurntable.ToList())
        {
            if(!currentCarsOnTurntable.Contains(car))
            {
                Main.Log($"Train: {car.CarGUID} left turntable");
                carsOnTurntable.Remove(car);
                car.stress.enabled = true;
                car.GetComponent<NetworkTrainSync>().CanTakeDamage = true;
            }
        }

        foreach(TrainCar car in currentCarsOnTurntable)
        {
            if (!carsOnTurntable.Contains(car))
            {
                Main.Log($"Train: {car.CarGUID} entered turntable");
                carsOnTurntable.Add(car);
                car.stress.enabled = false;
                car.GetComponent<NetworkTrainSync>().CanTakeDamage = false;
            }
        }

        if (SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork)
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
        //turntable.Snapped -= Turntable_Snapped;
        if (keyboardInput)
            keyboardInput.enabled = true;
    }

    //private void Turntable_Snapped()
    //{
    //    if (!SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork)
    //        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableSnap(turntable, turntable.turntable.currentYRotation);
    //}

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