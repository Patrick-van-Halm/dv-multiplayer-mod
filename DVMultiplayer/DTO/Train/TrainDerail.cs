using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainDerail : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public bool IsBogie1Derailed { get; set; }
        public bool IsBogie2Derailed { get; set; }
        public string Bogie1TrackName { get; set; }
        public string Bogie2TrackName { get; set; }
        public double Bogie1PositionAlongTrack { get; set; }
        public double Bogie2PositionAlongTrack { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            IsBogie1Derailed = e.Reader.ReadBoolean();
            IsBogie2Derailed = e.Reader.ReadBoolean();
            Bogie1TrackName = e.Reader.ReadString();
            Bogie2TrackName = e.Reader.ReadString();
            Bogie1PositionAlongTrack = e.Reader.ReadDouble();
            Bogie2PositionAlongTrack = e.Reader.ReadDouble();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(IsBogie1Derailed);
            e.Writer.Write(IsBogie2Derailed);
            e.Writer.Write(Bogie1TrackName);
            e.Writer.Write(Bogie2TrackName);
            e.Writer.Write(Bogie1PositionAlongTrack);
            e.Writer.Write(Bogie2PositionAlongTrack);
        }
    }
}
