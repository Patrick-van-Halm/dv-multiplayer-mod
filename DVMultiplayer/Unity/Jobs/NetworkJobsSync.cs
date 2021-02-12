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
    internal event Action<StationController, JobChainController> OnJobGenerated;
    StationController station;
    List<JobChainController> currentJobs = new List<JobChainController>();
    private void Awake()
    {
        station = GetComponent<StationController>();
        station.ProceduralJobsController.JobGenerationAttempt += OnJobGeneratedAttempt;
        currentJobs.AddRange(station.ProceduralJobsController.GetCurrentJobChains());
    }

    private void OnJobGeneratedAttempt()
    {
        JobChainController[] newJobs = currentJobs.Except(station.ProceduralJobsController.GetCurrentJobChains()).ToArray();
        foreach(JobChainController job in newJobs)
        {
            Main.DebugLog($"Job found with id: {job.currentJobInChain.ID}");
            OnJobGenerated?.Invoke(station, job);
        }
        currentJobs.AddRange(newJobs);
    }
}
