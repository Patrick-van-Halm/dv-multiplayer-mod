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
    public class WorldTrain : IDarkRiftSerializable
    {
        // Positioning and Physics
        public string Guid { get; set; }
        public string Id { get; set; }
        public TrainCarType CarType { get; set; } = TrainCarType.NotSet;
        public bool IsLoco { get; set; }

        // Position and physics
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }

        // Bogies
        public bool IsBogie1Derailed { get; set; }
        public double Bogie1PositionAlongTrack { get; set; }
        public string Bogie1RailTrackName { get; set; } = "";
        public bool IsBogie2Derailed { get; set; }
        public double Bogie2PositionAlongTrack { get; set; }
        public string Bogie2RailTrackName { get; set; } = "";

        // Couplers
        public bool IsFrontCouplerCoupled { get; set; }
        public bool IsRearCouplerCoupled { get; set; }

        // Player
        public bool IsPlayerSpawned { get; set; }

        // Locomotive (Only set if item is locomotive)
        public float? Throttle { get; set; } = null;
        public float? Brake { get; set; } = null;
        public float? IndepBrake { get; set; } = null;
        public float? Sander { get; set; } = null;
        public float? Reverser { get; set; } = null;

        // Specific Train states
        public Shunter Shunter { get; set; } = new Shunter();

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            Id = e.Reader.ReadString();
            CarType = (TrainCarType)e.Reader.ReadUInt32();
            IsLoco = e.Reader.ReadBoolean();

            Position = e.Reader.ReadVector3();
            Forward = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            Velocity = e.Reader.ReadVector3();
            AngularVelocity = e.Reader.ReadVector3();

            IsBogie1Derailed = e.Reader.ReadBoolean();
            Bogie1RailTrackName = e.Reader.ReadString();
            Bogie1PositionAlongTrack = e.Reader.ReadDouble();
            IsBogie2Derailed = e.Reader.ReadBoolean();
            Bogie2RailTrackName = e.Reader.ReadString();
            Bogie2PositionAlongTrack = e.Reader.ReadDouble();

            IsFrontCouplerCoupled = e.Reader.ReadBoolean();
            IsRearCouplerCoupled = e.Reader.ReadBoolean();

            IsPlayerSpawned = e.Reader.ReadBoolean();

            if (IsLoco)
            {
                Throttle = e.Reader.ReadNullableSingle();
                Brake = e.Reader.ReadNullableSingle();
                IndepBrake = e.Reader.ReadNullableSingle();
                Sander = e.Reader.ReadNullableSingle();
                Reverser = e.Reader.ReadNullableSingle();
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                    Shunter = e.Reader.ReadSerializable<Shunter>();
                    break;
            }
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write(Id);
            e.Writer.Write((uint)CarType);
            e.Writer.Write(IsLoco);

            e.Writer.Write(Position);
            e.Writer.Write(Forward);
            e.Writer.Write(Rotation);
            e.Writer.Write(Velocity);
            e.Writer.Write(AngularVelocity);

            e.Writer.Write(IsBogie1Derailed);
            e.Writer.Write(Bogie1RailTrackName);
            e.Writer.Write(Bogie1PositionAlongTrack);
            e.Writer.Write(IsBogie2Derailed);
            e.Writer.Write(Bogie2RailTrackName);
            e.Writer.Write(Bogie2PositionAlongTrack);

            e.Writer.Write(IsFrontCouplerCoupled);
            e.Writer.Write(IsRearCouplerCoupled);

            e.Writer.Write(IsPlayerSpawned);

            if (IsLoco)
            {
                e.Writer.Write(Throttle);
                e.Writer.Write(Brake);
                e.Writer.Write(IndepBrake);
                e.Writer.Write(Sander);
                e.Writer.Write(Reverser);
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                    e.Writer.Write(Shunter);
                    break;
            }
        }
    }
}
