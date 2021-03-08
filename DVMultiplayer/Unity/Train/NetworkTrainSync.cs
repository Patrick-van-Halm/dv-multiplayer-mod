using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using UnityEngine;

internal class NetworkTrainSync : MonoBehaviour
{
    public TrainCar loco;
    public bool listenToLocalPlayerInputs = false;
    private LocoControllerBase baseController;

    public void ListenToTrainInputEvents()
    {
        if (!loco.IsLoco)
            return;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to base loco controller");
        baseController = loco.GetComponent<LocoControllerBase>();
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen throttle change on base loco controller");
        baseController.ThrottleUpdated += OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen brake change on base loco controller");
        baseController.BrakeUpdated += OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated += OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen reverser change on base loco controller");
        baseController.ReverserUpdated += OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen sander change on base loco controller");
        baseController.SandersUpdated += OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to specific train events");
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

    public void StopListeningToTrainInputEvents()
    {
        if (!loco || !loco.IsLoco)
            return;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening throttle change on base loco controller");
        baseController.ThrottleUpdated -= OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening brake change on base loco controller");
        baseController.BrakeUpdated -= OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated -= OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening reverser change on base loco controller");
        baseController.ReverserUpdated -= OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening sander change on base loco controller");
        baseController.SandersUpdated -= OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening to train specific events");
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
        Main.Log($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] NetworkTrainSync Awake called");
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Load interior");
        if (!loco.IsInteriorLoaded)
            loco.LoadInterior();
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Keep interior loaded");
        loco.keepInteriorLoaded = true;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to inputEvents");
        ListenToTrainInputEvents();
    }

    public void OnDestroy()
    {
        if (loco && NetworkManager.IsHost())
        {
            if (loco.logicCar != null)
                Main.Log($"[{loco.ID}] NetworkTrainSync.OnDestroy()");
            if (loco.logicCar != null)
                Main.Log($"[{loco.ID}] Stop listening to input");
            //StopListeningToTrainInputEvents();
            if (loco.logicCar != null)
                Main.Log($"[{loco.ID}] Stop keeping interior loaded");
            loco.keepInteriorLoaded = false;

            if (loco.IsInteriorLoaded)
                loco.UnloadInterior();
        }
    }

    private IEnumerator RotaryAmplitudeCheckerStartListen(FuseBoxPowerController fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }

    private void ShunterHornUsed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = e.newValue;
        if (val < .7f && val > .3f)
            val = 0;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.Horn, val);
    }

    private void OnTrainFusePowerStarterStateChanged(int state)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        if (state == -1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.FusePowerStarter, 0);
        else if (state == 0)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.FusePowerStarter, 0.5f);
        else if (state == 1)
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.FusePowerStarter, 1);
    }

    private void OnTrainSideFuse_2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.SideFuse_2, e.newValue);
    }

    private void OnTrainSideFuse_1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.SideFuse_1, e.newValue);
    }

    private void OnTrainMainFuseChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.MainFuse, e.newValue);
    }

    private void OnTrainSanderChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.Sander, value);
    }

    private void OnTrainReverserStateChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.Reverser, value);
    }

    private void OnTrainIndependentBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.IndependentBrake, value);
    }

    private void OnTrainBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.Brake, value);
    }

    private void OnTrainThrottleChanged(float newThrottle)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(Levers.Throttle, newThrottle);
    }
}