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
    internal List<JobChainController> currentChains = new List<JobChainController>();
    StationController station;
    Coroutine sendNewJobsAfterGeneration = null;
    List<JobChainController> newChains = new List<JobChainController>();

    private void Awake()
    {
        station = GetComponent<StationController>();
        station.ProceduralJobsController.JobGenerationAttempt += OnChainsGenerated;
        currentChains.AddRange(station.ProceduralJobsController.GetCurrentJobChains());
    }

    private void OnDestroy()
    {
        station.ProceduralJobsController.JobGenerationAttempt -= OnChainsGenerated;
    }

    private void OnChainsGenerated()
    {
        List<JobChainController> newJobs = station.ProceduralJobsController.GetCurrentJobChains();
        foreach (JobChainController chain in currentChains)
        {
            newJobs.RemoveAll(j => j.currentJobInChain.ID == chain.currentJobInChain.ID);
        }

        currentChains.AddRange(newJobs);
        newChains.AddRange(newJobs);
        if (sendNewJobsAfterGeneration == null)
            sendNewJobsAfterGeneration = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitTillGenerationFinished());
    }

    internal void OnSingleChainGeneratedWithExistingCars(JobChainController chain)
    {
        Main.Log("Single Chain with existing cars generated");
        currentChains.Add(chain);
        OnJobsGenerated?.Invoke(station, new JobChainController[] { chain });
    }

    internal void OnSingleChainGenerated(bool trainsNotInitialized = false)
    {
        List<JobChainController> newJobs = station.ProceduralJobsController.GetCurrentJobChains();
        foreach (JobChainController chain in currentChains)
        {
            newJobs.RemoveAll(j => j.currentJobInChain.ID == chain.currentJobInChain.ID);
        }

        currentChains.AddRange(newJobs);
        if (trainsNotInitialized)
        {
            List<TrainCar> newJobTrains = new List<TrainCar>();
            foreach (JobChainController job in newJobs)
            {
                newJobTrains.AddRange(job.trainCarsForJobChain);
            }
            SingletonBehaviour<NetworkTrainManager>.Instance.SendNewJobChainCars(newJobTrains);
        }
        OnJobsGenerated?.Invoke(station, newJobs.ToArray());
    }

    private IEnumerator WaitTillGenerationFinished()
    {
        while (station.ProceduralJobsController.IsJobGenerationActive)
        {
            yield return new WaitUntil(() => !station.ProceduralJobsController.IsJobGenerationActive);
            yield return new WaitForSeconds(.25f);
        }

        Main.Log("Generation is finished Length = " + newChains.Count);
        List<TrainCar> newJobTrains = new List<TrainCar>();
        foreach(JobChainController job in newChains)
        {
            newJobTrains.AddRange(job.trainCarsForJobChain);
        }
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewJobChainCars(newJobTrains);

        OnJobsGenerated?.Invoke(station, newChains.ToArray());
        newChains.Clear();
        sendNewJobsAfterGeneration = null;
    }
}
