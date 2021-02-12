using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Job
{
    public class Job : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public string JobData { get; set; }
        public bool IsTaken { get; set; } = false;
        public ushort TakenByClient { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            JobData = e.Reader.ReadString();
            IsTaken = e.Reader.ReadBoolean();
            TakenByClient = e.Reader.ReadUInt16();
            IsCompleted = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(JobData);
            e.Writer.Write(IsTaken);
            e.Writer.Write(TakenByClient);
            e.Writer.Write(IsCompleted);
        }
    }
}
