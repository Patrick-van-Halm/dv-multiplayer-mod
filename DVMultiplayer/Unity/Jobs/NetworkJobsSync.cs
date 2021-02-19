using DV.Logic.Job;
using DVMultiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkJobsSync : MonoBehaviour
{
    internal event Action<StationController, JobChainController[]> OnJobsGenerated;
    StationController station;
    List<JobChainController> currentJobs = new List<JobChainController>();
    private void Awake()
    {
        station = GetComponent<StationController>();
        station.ProceduralJobsController.JobGenerationAttempt += OnJobGeneratedAttempt;
        currentJobs.AddRange(station.ProceduralJobsController.GetCurrentJobChains());
    }

    private void OnDestroy()
    {
        station.ProceduralJobsController.JobGenerationAttempt -= OnJobGeneratedAttempt;
    }

    private void OnJobGeneratedAttempt()
    {
        JobChainController[] newJobs = currentJobs.Except(station.ProceduralJobsController.GetCurrentJobChains()).ToArray();
        OnJobsGenerated?.Invoke(station, newJobs);
        currentJobs.AddRange(newJobs);
    }
}
