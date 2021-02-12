using DarkRift.Client;
using DVMultiplayer;
using DVMultiplayer.Networking;
using System;
using UnityEngine;

internal class NetworkJobsManager : SingletonBehaviour<NetworkJobsManager>
{
    private StationController[] allStations;
    private bool jobGenerationTurnedOff = false;

    protected override void Awake()
    {
        Main.DebugLog($"NetworkJobsManager initialized");
        base.Awake();
        allStations = GameObject.FindObjectsOfType<StationController>();

        //SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void PlayerConnect()
    {
        Main.DebugLog($"Disable jobs generation if client");
        if (NetworkManager.IsHost() || jobGenerationTurnedOff)
            return;
        jobGenerationTurnedOff = true;

        foreach (StationController station in allStations)
        {
            station.ExpireAllAvailableJobsInStation();
        }
    }

    public void PlayerDisconnect()
    {
        Main.DebugLog($"Enable jobs generation if client");
        if (NetworkManager.IsHost() || !jobGenerationTurnedOff)
            return;

        jobGenerationTurnedOff = false;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        throw new NotImplementedException();
    }
}