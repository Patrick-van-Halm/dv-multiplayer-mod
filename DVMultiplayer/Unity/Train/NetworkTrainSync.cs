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
    LocoControllerBase baseController;
    private bool sanderCoroutineActive;

    public void ListenToTrainInputEvents()
    {
        if (!loco.IsLoco)
            return;

        Main.DebugLog($"[{loco.ID}] Listen to base loco controller");
        baseController = loco.GetComponent<LocoControllerBase>();
        Main.DebugLog($"[{loco.ID}] Listen throttle change on base loco controller");
        baseController.ThrottleUpdated += OnTrainThrottleChanged;
        Main.DebugLog($"[{loco.ID}] Listen brake change on base loco controller");
        baseController.BrakeUpdated += OnTrainBrakeChanged;
        Main.DebugLog($"[{loco.ID}] Listen indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated += OnTrainIndependentBrakeChanged;
        Main.DebugLog($"[{loco.ID}] Listen reverser change on base loco controller");
        baseController.ReverserUpdated += OnTrainReverserStateChanged;
        Main.DebugLog($"[{loco.ID}] Listen sander change on base loco controller");
        baseController.SandersUpdated += OnTrainSanderChanged;
        //loco.TrainCarCollisions.CarDamaged += OnTrainDamaged;

        Main.DebugLog($"[{loco.ID}] Listen to specific train events");
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

        Main.DebugLog($"[{loco.ID}] Stop listening throttle change on base loco controller");
        baseController.ThrottleUpdated -= OnTrainThrottleChanged;
        Main.DebugLog($"[{loco.ID}] Stop listening brake change on base loco controller");
        baseController.BrakeUpdated -= OnTrainBrakeChanged;
        Main.DebugLog($"[{loco.ID}] Stop listening indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated -= OnTrainIndependentBrakeChanged;
        Main.DebugLog($"[{loco.ID}] Stop listening reverser change on base loco controller");
        baseController.ReverserUpdated -= OnTrainReverserStateChanged;
        Main.DebugLog($"[{loco.ID}] Stop listening sander change on base loco controller");
        baseController.SandersUpdated -= OnTrainSanderChanged;

        Main.DebugLog($"[{loco.ID}] Stop listening to train specific events");
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
        Main.DebugLog($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
        Main.DebugLog($"[{loco.ID}] NetworkTrainSync Awake called");

        Main.DebugLog($"[{loco.ID}] Load interior");
        if (!loco.IsInteriorLoaded)
            loco.LoadInterior();
        Main.DebugLog($"[{loco.ID}] Keep interior loaded");
        loco.keepInteriorLoaded = true;

        Main.DebugLog($"[{loco.ID}] Listen to inputEvents");
        ListenToTrainInputEvents();
    }

    public void OnDestroy()
    {
        Main.DebugLog($"[{loco.ID}] NetworkTrainSync.OnDestroy()");
        Main.DebugLog($"[{loco.ID}] Stop listening to input");
        StopListeningToTrainInputEvents();
        Main.DebugLog($"[{loco.ID}] Stop keeping interior loaded");
        loco.keepInteriorLoaded = false;

        Main.DebugLog($"[{loco.ID}] Unload interior if player is not in car");
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

        float val = e.newValue;
        if (val < .7f && val > .3f)
            val = 0;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Horn, val);
    }

    private void OnTrainFusePowerStarterStateChanged(int state)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        if(state == -1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.FusePowerStarter, 0);
        else if (state == 0)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.FusePowerStarter, 0.5f);
        else if (state == 1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.FusePowerStarter, 1);
    }

    private void OnTrainFusePowerStarterChanged(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.FusePowerStarter, e.newValue);
    }

    private void OnTrainSideFuse_2Changed(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.SideFuse_2, e.newValue);
    }

    private void OnTrainSideFuse_1Changed(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.SideFuse_1, e.newValue);
    }

    private void OnTrainMainFuseChanged(ValueChangedEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.MainFuse, e.newValue);
    }

    private void OnTrainSanderChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs || sanderCoroutineActive)
            return;
        SingletonBehaviour<CoroutineManager>.Instance.Run(SanderUpdate());
    }

    private IEnumerator SanderUpdate()
    {
        sanderCoroutineActive = true;
        if (baseController.IsSandOn())
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Sander, 1);
            yield return new WaitUntil(() => !baseController.IsSandOn());
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Sander, 0);
        }
        sanderCoroutineActive = false;
    }

    private void OnTrainReverserStateChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Reverser, value);
    }

    private void OnTrainIndependentBrakeChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.IndependentBrake, value);
    }

    private void OnTrainBrakeChanged(float value)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Brake, value);
    }

    private void OnTrainThrottleChanged(float newThrottle)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(this, Levers.Throttle, newThrottle);
    }
}