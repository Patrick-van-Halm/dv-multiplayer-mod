using DV.MultipleUnit;
using DVMultiplayer;
using DVMultiplayer.Networking;
using System;
using UnityEngine;

internal class NetworkTrainMUSync : MonoBehaviour
{
    private MultipleUnitCable mu;
#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkTrainMUSync.Awake()");
        mu = GetComponent<MultipleUnitCable>();
        string trainId = mu.muModule.loco.train.CarGUID;

        Main.Log($"[{trainId}-{(mu.isFront ? "Front" : "Back")}] NetworkTrainMUSync Awake called");
        Main.Log($"[{trainId}-{(mu.isFront ? "Front" : "Back")}] Listening to connection changed event");
        mu.ConnectionChanged += MUConnectionChanged;
    }

    private void OnDestroy()
    {
        Main.Log($"NetworkTrainMUSync.OnDestroy()");
        if (mu is null)
            return;

        if (NetworkManager.IsHost())
        {
            mu.ConnectionChanged -= MUConnectionChanged;
        }
    }
#pragma warning restore IDE0051 // Remove unused private members

    private void MUConnectionChanged(bool isConnected, bool isAudioPlayed)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Exists || SingletonBehaviour<NetworkTrainManager>.Instance.IsDisconnecting || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced || mu is null || SingletonBehaviour<NetworkTrainManager>.Instance.IsSpawningTrains)
            return;

        if (isConnected)
            SingletonBehaviour<NetworkTrainManager>.Instance.OnMUConnectionChanged(mu.muModule.loco.train.CarGUID, mu.isFront, mu.connectedTo.muModule.loco.train.CarGUID, mu.connectedTo.isFront, isConnected, isAudioPlayed);
        else
            SingletonBehaviour<NetworkTrainManager>.Instance.OnMUConnectionChanged(mu.muModule.loco.train.CarGUID, mu.isFront, "", false, isConnected, isAudioPlayed);
    }
}