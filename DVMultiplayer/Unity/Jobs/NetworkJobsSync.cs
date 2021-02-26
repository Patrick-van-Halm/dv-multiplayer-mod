﻿using DV.Logic.Job;
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
        List<JobChainController> newJobs = station.ProceduralJobsController.GetCurrentJobChains();
        foreach (JobChainController chain in currentJobs)
        {
            newJobs.RemoveAll(j => j.currentJobInChain.ID == chain.currentJobInChain.ID);
        }

        currentJobs.AddRange(newJobs);
        newChains.AddRange(newJobs);
        if (sendNewJobsAfterGeneration == null)
            sendNewJobsAfterGeneration = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitTillGenerationFinished());
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
