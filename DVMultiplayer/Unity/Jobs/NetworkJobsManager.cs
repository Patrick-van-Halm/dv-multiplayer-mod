using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkJobsManager : SingletonBehaviour<NetworkJobsManager>
{
    StationController[] allStations;
    Dictionary<StationController, StationProceduralJobsRuleset> defaultRulesets;
    bool jobGenerationTurnedOff = false;

    protected override void Awake()
    {
        base.Awake();
        defaultRulesets = new Dictionary<StationController, StationProceduralJobsRuleset>();
        allStations = GameObject.FindObjectsOfType<StationController>();

        //SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void PlayerConnect()
    {
        if (NetworkManager.IsHost() && jobGenerationTurnedOff)
            return;
        jobGenerationTurnedOff = true;

        foreach(StationController station in allStations)
        {
            defaultRulesets.Add(station, station.proceduralJobsRuleset);
            station.ExpireAllAvailableJobsInStation();
            station.proceduralJobsRuleset = new StationProceduralJobsRuleset()
            {
                jobsCapacity = 0,
            };
        }
    }

    public void PlayerDisconnect()
    {
        if (NetworkManager.IsHost() && !jobGenerationTurnedOff)
            return;

        jobGenerationTurnedOff = false;

        foreach (StationController station in allStations)
        {
            station.proceduralJobsRuleset = defaultRulesets[station];
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        throw new NotImplementedException();
    }
}