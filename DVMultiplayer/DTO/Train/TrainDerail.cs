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
        public bool IsBogie1Derailed { get; internal set; }
        public bool IsBogie2Derailed { get; internal set; }
        public string Bogie1TrackName { get; internal set; }
        public string Bogie2TrackName { get; internal set; }
        public double Bogie1PositionAlongTrack { get; internal set; }
        public double Bogie2PositionAlongTrack { get; internal set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
        }
    }
}
