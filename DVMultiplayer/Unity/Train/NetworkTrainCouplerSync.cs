using DVMultiplayer;
using UnityEngine;

internal class NetworkTrainCouplerSync : MonoBehaviour
{
    private Coupler coupler;
#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainCouplerSync.Awake()");
        coupler = GetComponent<Coupler>();
        Main.Log($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] NetworkTrainCouplerSync Awake called");
        Main.Log($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to coupled event");
        coupler.Coupled += CouplerCoupled;
        Main.Log($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to uncoupled event");
        coupler.Uncoupled += CouplerUncoupled;
        Main.Log($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to hose connection changed event");
        coupler.HoseConnectionChanged += CouplerHoseConChanged;
        Main.Log($"[{coupler.train.ID}-{(coupler.isFrontCoupler ? "Front" : "Back")}] Listening to cock changed event");
        coupler.CockChanged += CouplerCockChanged;
    }
#pragma warning restore IDE0051 // Remove unused private members

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