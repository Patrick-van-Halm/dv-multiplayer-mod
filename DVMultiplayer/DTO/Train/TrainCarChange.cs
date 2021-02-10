using DarkRift;
using DVMultiplayer.Darkrift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCarChange : IDarkRiftSerializable
    {
        public ushort PlayerId { get; set; }

        public string TrainId { get; set; } = "";
        public string CarId { get; set; } = "";
        public TrainCarType Type { get; set; }
        public bool IsLoco { get; set; }

        public bool IsPlayerSpawned { get; set; }

        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }

        public bool IsBogie1Derailed { get; set; }
        public double Bogie1PositionAlongTrack { get; set; }
        public string Bogie1RailTrackName { get; set; } = "";
        public bool IsBogie2Derailed { get; set; }
        public double Bogie2PositionAlongTrack { get; set; }
        public string Bogie2RailTrackName { get; set; } = "";


        public void Deserialize(DeserializeEvent e)
        {
            PlayerId = e.Reader.ReadUInt16();
            
            TrainId = e.Reader.ReadString();
            CarId = e.Reader.ReadString();
            Type = (TrainCarType)e.Reader.ReadUInt32();
            IsLoco = e.Reader.ReadBoolean();

            IsPlayerSpawned = e.Reader.ReadBoolean();

            Position = e.Reader.ReadVector3();
            Forward = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();

            IsBogie1Derailed = e.Reader.ReadBoolean();  
            Bogie1PositionAlongTrack = e.Reader.ReadDouble();
            Bogie1RailTrackName = e.Reader.ReadString();

            IsBogie2Derailed = e.Reader.ReadBoolean();
            Bogie2PositionAlongTrack = e.Reader.ReadDouble();
            Bogie2RailTrackName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(PlayerId);
            e.Writer.Write(TrainId);
            e.Writer.Write(CarId);
            e.Writer.Write((uint)Type);
            e.Writer.Write(IsLoco);
            e.Writer.Write(IsPlayerSpawned);
            e.Writer.Write(Position);
            e.Writer.Write(Forward);
            e.Writer.Write(Rotation);
            e.Writer.Write(IsBogie1Derailed);
            e.Writer.Write(Bogie1PositionAlongTrack);
            e.Writer.Write(Bogie1RailTrackName);
            e.Writer.Write(IsBogie2Derailed);
            e.Writer.Write(Bogie2PositionAlongTrack);
            e.Writer.Write(Bogie2RailTrackName);
        }
    }
}
