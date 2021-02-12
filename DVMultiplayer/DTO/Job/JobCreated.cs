using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Job
{
    public class JobCreated : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public string JobData { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            JobData = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(JobData);
        }
    }
}
