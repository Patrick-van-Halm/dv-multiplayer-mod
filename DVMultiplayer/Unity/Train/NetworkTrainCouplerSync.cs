using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class NetworkTrainCouplerSync : MonoBehaviour
{
    private Coupler coupler;

    private void Awake()
    {
        Main.DebugLog($"NetworkTrainCouplerSync.Awake()");
        coupler = GetComponent<Coupler>();
        Main.DebugLog($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] NetworkTrainCouplerSync Awake called");
        Main.DebugLog($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to coupled event");
        coupler.Coupled += CouplerCoupled;
        Main.DebugLog($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to uncoupled event");
        coupler.Uncoupled += CouplerUncoupled;
        Main.DebugLog($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to hose connection changed event");
        coupler.HoseConnectionChanged += CouplerHoseConChanged;
        Main.DebugLog($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to cock changed event");
        coupler.CockChanged += CouplerCockChanged;
    }

    private void CouplerUncoupled(object sender, UncoupleEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarCoupledChange(e.thisCoupler, e.otherCoupler, e.viaChainInteraction, false);
    }

    private void CouplerCockChanged(bool isCockOpen)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarCouplerCockChanged(coupler, isCockOpen);
    }

    private void CouplerHoseConChanged(bool isConnected, bool audioPlayed)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarCouplerHoseConChanged(coupler, isConnected);
    }

    private void CouplerCoupled(object sender, CoupleEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCarCoupledChange(e.thisCoupler, e.otherCoupler, e.viaChainInteraction, true);
    }
}