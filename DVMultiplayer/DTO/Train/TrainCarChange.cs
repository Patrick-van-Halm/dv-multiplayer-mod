using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCarChange : IDarkRiftSerializable
    {
        public ushort PlayerId { get; set; }
        public string TrainId { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.PlayerId = e.Reader.ReadUInt16();
            this.TrainId = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.PlayerId);
            e.Writer.Write(this.TrainId);
        }
    }
}
