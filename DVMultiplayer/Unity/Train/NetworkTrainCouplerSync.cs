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
        coupler = GetComponent<Coupler>();
        coupler.Coupled += CouplerCoupled;
        coupler.Uncoupled += CouplerUncoupled;
        coupler.HoseConnectionChanged += CouplerHoseConChanged;
        coupler.CockChanged += CouplerCockChanged;
    }

    private void CouplerUncoupled(object sender, UncoupleEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendTrainsCoupledChange(e.thisCoupler, e.otherCoupler, e.viaChainInteraction, false);
    }

    private void CouplerCockChanged(bool isCockOpen)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCouplerCockChanged(coupler, isCockOpen);
    }

    private void CouplerHoseConChanged(bool isConnected, bool audioPlayed)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendCouplerHoseConChanged(coupler, isConnected);
    }

    private void CouplerCoupled(object sender, CoupleEventArgs e)
    {
        if (SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !coupler)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendTrainsCoupledChange(e.thisCoupler, e.otherCoupler, e.viaChainInteraction, true);
    }
}