using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class NetworkTrainSync : MonoBehaviour
{
    private TrainCar loco;
    public bool listenToLocalPlayerInputs = false;

    public void ListenToTrainInputEvents()
    {
        if (!loco.IsLoco)
            return;

        LocoControllerBase baseLocomotiveController = loco.GetComponent<LocoControllerBase>();
        baseLocomotiveController.ThrottleUpdated += OnTrainThrottleChanged;
        baseLocomotiveController.BrakeUpdated += OnTrainBrakeChanged;
        baseLocomotiveController.IndependentBrakeUpdated += OnTrainIndependentBrakeChanged;
        baseLocomotiveController.ReverserUpdated += OnTrainReverserStateChanged;
        baseLocomotiveController.SandersUpdated += OnTrainSanderChanged;
        //loco.TrainCarCollisions.CarDamaged += OnTrainDamaged;

        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                ShunterDashboardControls shunterDashboard = loco.interior.GetComponentInChildren<ShunterDashboardControls>();
                FuseBoxPowerController fuseBox = shunterDashboard.fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged += OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged += OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged += OnTrainMainFuseChanged;
                shunterDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += ShunterHornUsed;
                SingletonBehaviour<CoroutineManager>.Instance.Run(RotaryAmplitudeCheckerStartListen(fuseBox));
                break;
        }
    }

    private void OnTrainDamaged(float colDamage, Vector3 forceDirection)
    {
        throw new NotImplementedException();
    }

    public void StopListeningToTrainInputEvents()
    {
        if (!loco || !loco.IsLoco)
            return;

        LocoControllerBase baseLocomotiveController = loco.GetComponent<LocoControllerBase>();
        baseLocomotiveController.ThrottleUpdated -= OnTrainThrottleChanged;
        baseLocomotiveController.BrakeUpdated -= OnTrainBrakeChanged;
        baseLocomotiveController.IndependentBrakeUpdated -= OnTrainIndependentBrakeChanged;
        baseLocomotiveController.ReverserUpdated -= OnTrainReverserStateChanged;
        baseLocomotiveController.SandersUpdated -= OnTrainSanderChanged;

        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                FuseBoxPowerController fuseBox = loco.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged -= OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;
        }
    }

    public void Awake()
    {
        loco = GetComponent<TrainCar>();

        if(!loco.IsInteriorLoaded)
            loco.LoadInterior();
        loco.keepInteriorLoaded = true;

        ListenToTrainInputEvents();
    }

    public void OnDestroy()
    {
        StopListeningToTrainInputEvents();
        loco.keepInteriorLoaded = false;

        if (PlayerManager.Car && PlayerManager.Car.CarGUID == loco.CarGUID)
            return;

        if (loco.IsInteriorLoaded)
            loco.UnloadInterior();
    }

    IEnumerator RotaryAmplitudeCheckerStartListen(FuseBoxPowerController fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }

    private void ShunterHornUsed(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.Horn, e.newValue);
    }

    private void OnTrainFusePowerStarterStateChanged(int state)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        if(state == -1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.FusePowerStarter, 0);
        else if (state == 0)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.FusePowerStarter, 0.5f);
        else if (state == 1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.FusePowerStarter, 1);
    }

    private void OnTrainFusePowerStarterChanged(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.FusePowerStarter, e.newValue);
    }

    private void OnTrainSideFuse_2Changed(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.SideFuse_2, e.newValue);
    }

    private void OnTrainSideFuse_1Changed(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.SideFuse_1, e.newValue);
    }

    private void OnTrainMainFuseChanged(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.MainFuse, e.newValue);
    }

    private void OnTrainSanderChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.Brake, value);
    }

    private void OnTrainReverserStateChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.Reverser, value);
    }

    private void OnTrainIndependentBrakeChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.IndependentBrake, value);
    }

    private void OnTrainBrakeChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.Brake, value);
    }

    private void OnTrainThrottleChanged(float newThrottle)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLeverValue(this, Levers.Throttle, newThrottle);
    }
}