using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Job;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobsPlugin
{
    public class JobsPlugin : Plugin
    {
        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.0.0");

        private readonly List<Job> jobs;

        public JobsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            jobs = new List<Job>();
            ClientManager.ClientConnected += OnClientConnected;
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("JOB_"))
                    return;

                Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.JOB_CREATED:
                        OnJobCreated(message, e.Client);
                        break;

                    case NetworkTags.JOB_SYNC:
                        SendAllServerJobs(e.Client);
                        break;

                    case NetworkTags.JOB_HOST_SYNC:
                        UpdateServerJobs(message);
                        break;
                }

            }
        }

        private void UpdateServerJobs(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Job[] jobs = reader.ReadSerializables<Job>();
                this.jobs.AddRange(jobs);
            }
        }

        private void SendAllServerJobs(IClient sender)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(jobs.ToArray());

                using (Message msg = Message.Create((ushort)NetworkTags.JOB_SYNC, writer))
                    sender.SendMessage(msg, SendMode.Reliable);
            }
        }

        private void OnJobCreated(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                JobCreated job = reader.ReadSerializable<JobCreated>();
                jobs.Add(new Job()
                {
                    Id = job.Id,
                    JobData = job.JobData
                });
            }

            ReliableSendToOthers(message, sender);
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}