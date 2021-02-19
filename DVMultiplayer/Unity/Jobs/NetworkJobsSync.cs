using DV.Logic.Job;
using DVMultiplayer;
using System;
using System.Collections;
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
    Coroutine sendNewJobsAfterGeneration = null;
    List<JobChainController> newChains = new List<JobChainController>();

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
        newChains.AddRange(currentJobs.Except(station.ProceduralJobsController.GetCurrentJobChains()));
        if (sendNewJobsAfterGeneration == null)
            sendNewJobsAfterGeneration = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitTillGenerationFinished());
    }

    private IEnumerator WaitTillGenerationFinished()
    {
        do
        {
            yield return new WaitUntil(() => station.ProceduralJobsController.IsJobGenerationActive);
            yield return new WaitForSeconds(.1f);
        }
        while (station.ProceduralJobsController.IsJobGenerationActive);

        foreach(JobChainController job in newChains)
        {
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewJobChainCars(job.trainCarsForJobChain);
        }

        OnJobsGenerated?.Invoke(station, newChains.ToArray());
        newChains.Clear();
        sendNewJobsAfterGeneration = null;
    }
}
