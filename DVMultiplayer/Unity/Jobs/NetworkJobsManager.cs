using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.DTO.Job;
using DVMultiplayer.Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

internal class NetworkJobsManager : SingletonBehaviour<NetworkJobsManager>
{
    private StationController[] allStations;
    public bool IsChangedByNetwork { get; set; }
    public bool IsSynced { get; internal set; }

    protected override void Awake()
    {
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
            }
        }
    }
    #endregion

    #region Receiving Messages
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
                    if (SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData))
                        Main.DebugLog("Job successfully loaded");
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
                if (SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData))
                    Main.DebugLog("Job successfully loaded");
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
            writer.Write(new JobCreated()
            {
                Id = job.currentJobInChain.ID,
                JobData = JsonConvert.SerializeObject(job.GetJobChainSaveData(), JobSaveManager.serializeSettings)
            });

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
                    currentJobs.Add(new Job()
                    {
                        Id = job.currentJobInChain.ID,
                        JobData = JsonConvert.SerializeObject(job.GetJobChainSaveData(), JobSaveManager.serializeSettings),
                        IsTaken = job.currentJobInChain.State == DV.Logic.Job.JobState.InProgress,
                        TakenByClient = job.currentJobInChain.State == DV.Logic.Job.JobState.InProgress ? SingletonBehaviour<UnityClient>.Instance.ID : (ushort)0,
                        IsCompleted = job.currentJobInChain.State == DV.Logic.Job.JobState.Completed
                    });
                }
            }
            writer.Write(currentJobs.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.JOB_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion
}