using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCouplerHoseChange : IDarkRiftSerializable
    {
        public string TrainIdC1 { get; set; }
        public bool IsC1Front { get; set; }
        public string TrainIdC2 { get; set; }
        public bool IsC2Front { get; set; }
        public bool IsConnected { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.TrainIdC1 = e.Reader.ReadString();
            this.IsC1Front = e.Reader.ReadBoolean();
            this.TrainIdC2 = e.Reader.ReadString();
            this.IsC2Front = e.Reader.ReadBoolean();
            this.IsConnected = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainIdC1);
            e.Writer.Write(IsC1Front);
            e.Writer.Write(TrainIdC2);
            e.Writer.Write(IsC2Front);
            e.Writer.Write(IsConnected);
        }
    }
}
