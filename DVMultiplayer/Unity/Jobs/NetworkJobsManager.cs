﻿using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.DTO.Job;
using DVMultiplayer.Networking;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkJobsManager : SingletonBehaviour<NetworkJobsManager>
{
    private StationController[] allStations;
    private Dictionary<Job, DV.Logic.Job.Job> jobs;
    public bool IsChangedByNetwork { get; set; }
    public bool IsSynced { get; internal set; }

    protected override void Awake()
    {
        jobs = new Dictionary<Job, DV.Logic.Job.Job>();
        Main.DebugLog($"NetworkJobsManager initialized");
        base.Awake();
        allStations = GameObject.FindObjectsOfType<StationController>();
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    public void PlayerConnect()
    {
        Main.DebugLog($"Expiring singleplayer jobs");
        foreach (StationController station in allStations)
        {
            station.ExpireAllAvailableJobsInStation();
        }
    }

    public void OnFinishLoading()
    {
        if (NetworkManager.IsHost())
        {
            SendInitializedJobs();
            IsSynced = true;
        }

        foreach (StationController station in allStations)
        {
            NetworkJobsSync jobSync = station.gameObject.AddComponent<NetworkJobsSync>();
            jobSync.OnJobGenerated += SendJobCreatedMessage;
        }
    }

    #region Events

    private void CurrentJobInChain_JobTaken(DV.Logic.Job.Job job, bool takenViaGame)
    {
        job.JobCompleted += Job_JobCompleted;
        Job sJob = jobs.Keys.FirstOrDefault(j => j.Id == job.ID);
        if (sJob != null && sJob.CanTakeJob)
        {
            SendJobTaken(job.ID);
            sJob.IsTakenByLocalPlayer = true;
            sJob.IsTaken = true;
        }
    }

    private void Job_JobCompleted(DV.Logic.Job.Job job)
    {
        Job sJob = jobs.Keys.FirstOrDefault(j => j.Id == job.ID);
        if (sJob != null && !sJob.IsCompleted)
        {
            SendJobCompleted(job.ID);
            sJob.IsCompleted = true;
        }
        jobs.Remove(sJob);
    }

    #endregion

    #region Messaging
    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.JOB_CREATED:
                    OnJobCreatedMessage(message);
                    break;

                case NetworkTags.JOB_SYNC:
                    OnJobSyncMessage(message);
                    break;

                case NetworkTags.JOB_TAKEN:
                    OnOtherPlayerTakenJobMessage(message);
                    break;

                case NetworkTags.JOB_COMPLETED:
                    OnJobCompletedMesssage(message);
                    break;
            }
        }
    }

    #endregion

    #region Receiving Messages
    private void OnJobCompletedMesssage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < JOB_COMPLETED");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Job job = jobs.Keys.FirstOrDefault(j => j.Id == id);
                if (!job.IsCompleted)
                {
                    job.IsCompleted = true;
                    if(!job.IsTakenByLocalPlayer)
                        jobs[job].CompleteJob();
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnOtherPlayerTakenJobMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < JOB_TAKEN");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Job job = jobs.Keys.FirstOrDefault(j => j.Id == id);
                if (!job.IsTaken)
                {
                    job.IsTaken = true;
                    SingletonBehaviour<CoroutineManager>.Instance.Run(ExpireJobAfterTime(job));
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnJobSyncMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < JOB_SYNC");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                Job[] jobs = reader.ReadSerializables<Job>();
                foreach(Job job in jobs)
                {
                    JobChainSaveData jobSaveData = JsonConvert.DeserializeObject<JobChainSaveData>(job.JobData, JobSaveManager.serializeSettings);
                    GameObject jobGO = SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData);
                    if (jobGO)
                    {
                        StaticJobDefinition jobDef = jobGO.GetComponent<StaticJobDefinition>();
                        jobDef.job.JobTaken += CurrentJobInChain_JobTaken;
                        this.jobs.Add(job, jobDef.job);
                        Main.DebugLog("Job successfully loaded");
                    }
                }
                IsChangedByNetwork = false;
            }
        }
        IsSynced = true;
    }

    private void OnJobCreatedMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] < JOB_CREATED");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                JobCreated job = reader.ReadSerializable<JobCreated>();
                JobChainSaveData jobSaveData = JsonConvert.DeserializeObject<JobChainSaveData>(job.JobData, JobSaveManager.serializeSettings);
                GameObject jobGO = SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData);
                if (jobGO)
                {
                    StaticJobDefinition jobDef = jobGO.GetComponent<StaticJobDefinition>();
                    jobDef.job.JobTaken += CurrentJobInChain_JobTaken;
                    jobs.Add(new Job()
                    {
                        Id = job.Id,
                        JobData = job.JobData,
                        IsTaken = false,
                        IsCompleted = false
                    }, jobDef.job);
                    Main.DebugLog("Job successfully loaded");
                }
                IsChangedByNetwork = false;
            }
        }
    }
    #endregion

    #region Sending Messages
    private void SendJobCreatedMessage(StationController station, JobChainController job)
    {
        if (IsChangedByNetwork || !IsSynced)
            return;

        Main.DebugLog($"[CLIENT] > JOB_CREATED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            JobCreated newJob = new JobCreated()
            {
                Id = job.currentJobInChain.ID,
                JobData = JsonConvert.SerializeObject(job.GetJobChainSaveData(), JobSaveManager.serializeSettings)
            };
            writer.Write(newJob);

            job.currentJobInChain.JobTaken += CurrentJobInChain_JobTaken;
            jobs.Add(new Job()
            {
                Id = newJob.Id,
                JobData = newJob.JobData,
                IsCompleted = false,
                IsTaken = false
            }, job.currentJobInChain);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_CREATED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendJobsRequest()
    {
        Main.DebugLog($"[CLIENT] > JOB_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendJobTaken(string id)
    {
        Main.DebugLog($"[CLIENT] > JOB_TAKEN");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_TAKEN, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendJobCompleted(string id)
    {
        Main.DebugLog($"[CLIENT] > JOB_COMPLETED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_COMPLETED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendInitializedJobs()
    {
        Main.DebugLog($"[CLIENT] > JOB_HOST_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<Job> currentJobs = new List<Job>();
            foreach(StationController station in allStations)
            {
                foreach(JobChainController job in station.ProceduralJobsController.GetCurrentJobChains())
                {
                    Job sJob = new Job()
                    {
                        Id = job.currentJobInChain.ID,
                        JobData = JsonConvert.SerializeObject(job.GetJobChainSaveData(), JobSaveManager.serializeSettings),
                        IsTaken = job.currentJobInChain.State == DV.Logic.Job.JobState.InProgress,
                        IsCompleted = job.currentJobInChain.State == DV.Logic.Job.JobState.Completed
                    };

                    currentJobs.Add(sJob);

                    job.currentJobInChain.JobTaken += CurrentJobInChain_JobTaken;
                    jobs.Add(sJob, job.currentJobInChain);
                }
            }
            writer.Write(currentJobs.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.JOB_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion

    private IEnumerator ExpireJobAfterTime(Job job)
    {
        // Wait 2 minutes so the user can still accept the job
        yield return new WaitForSeconds(2 * 60);
        job.CanTakeJob = false;
    }

    internal bool IsAllowedToTakeJob(string id)
    {
        return jobs.Keys.FirstOrDefault(j => j.Id == id).CanTakeJob;
    }
}