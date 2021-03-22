using DV.MultipleUnit;
using DVMultiplayer;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using UnityEngine;

internal class NetworkTrainMUSync : MonoBehaviour
{
    private MultipleUnitModule mu;
    private MultipleUnitCable frontConnectedTo;
    private MultipleUnitCable rearConnectedTo;
#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainMUSync.Awake()");
        mu = GetComponent<MultipleUnitModule>();
        if (mu)
        {
            ListenToEvents();
        }
        else
            StartCoroutine(LateMUInit());
    }

    private IEnumerator LateMUInit()
    {
        yield return new WaitUntil(() => GetComponent<MultipleUnitModule>());
        mu = GetComponent<MultipleUnitModule>();
        ListenToEvents();
    }

    private void ListenToEvents()
    {
        Main.Log($"NetworkTrainMUSync Listening to events");

        if (mu.frontCableAdapter)
            mu.frontCableAdapter.muCable.ConnectionChanged += MUFrontConnectionChanged;
        if(mu.rearCableAdapter)
            mu.rearCableAdapter.muCable.ConnectionChanged += MURearConnectionChanged;
    }

    private void OnDestroy()
    {
        Main.Log($"NetworkTrainMUSync.OnDestroy()");
        if (mu is null)
            return;

        if (NetworkManager.IsHost())
        {
            if (mu.frontCableAdapter)
                mu.frontCableAdapter.muCable.ConnectionChanged -= MUFrontConnectionChanged;
            if (mu.rearCableAdapter)
                mu.rearCableAdapter.muCable.ConnectionChanged -= MURearConnectionChanged;
        }
    }

#pragma warning restore IDE0051 // Remove unused private members
    private void MURearConnectionChanged(bool isConnected, bool isAudioPlayed)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || mu is null || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        rearConnectedTo = mu.rearCableAdapter.muCable.connectedTo;
        if (rearConnectedTo.isFront && rearConnectedTo == rearConnectedTo.muModule.GetComponent<NetworkTrainMUSync>().frontConnectedTo)
            return;

        if (!rearConnectedTo.isFront && rearConnectedTo == rearConnectedTo.muModule.GetComponent<NetworkTrainMUSync>().rearConnectedTo)
            return;
        MUConnectionChanged(isConnected, isAudioPlayed, false, rearConnectedTo);
    }

    private void MUFrontConnectionChanged(bool isConnected, bool isAudioPlayed)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || mu is null || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        frontConnectedTo = mu.frontCableAdapter.muCable.connectedTo;
        if (frontConnectedTo.isFront && frontConnectedTo == frontConnectedTo.muModule.GetComponent<NetworkTrainMUSync>().frontConnectedTo)
            return;

        if (!frontConnectedTo.isFront && frontConnectedTo == frontConnectedTo.muModule.GetComponent<NetworkTrainMUSync>().rearConnectedTo)
            return;

        MUConnectionChanged(isConnected, isAudioPlayed, true, frontConnectedTo);
    }

    private void MUConnectionChanged(bool isConnected, bool isAudioPlayed, bool isFront, MultipleUnitCable connectedTo = null)
    {
        

        if (isConnected)
            SingletonBehaviour<NetworkTrainManager>.Instance.OnMUConnectionChanged(mu.loco.train.CarGUID, isFront, connectedTo.muModule.loco.train.CarGUID, connectedTo.isFront, isConnected, isAudioPlayed);
        else
            SingletonBehaviour<NetworkTrainManager>.Instance.OnMUConnectionChanged(mu.loco.train.CarGUID, isFront, "", false, isConnected, isAudioPlayed);
    }
}