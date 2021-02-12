using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainLocation : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public string Bogie1TrackName { get; set; }
        public double Bogie1PositionAlongTrack { get; set; }
        public string Bogie2TrackName { get; set; }
        public double Bogie2PositionAlongTrack { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            Forward = e.Reader.ReadVector3();
            Velocity = e.Reader.ReadVector3();
            AngularVelocity = e.Reader.ReadVector3();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            Bogie1PositionAlongTrack = e.Reader.ReadDouble();
            Bogie1TrackName = e.Reader.ReadString();
            Bogie2PositionAlongTrack = e.Reader.ReadDouble();
            Bogie2TrackName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(Forward);
            e.Writer.Write(Velocity);
            e.Writer.Write(AngularVelocity);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(Bogie1PositionAlongTrack);
            e.Writer.Write(Bogie1TrackName);
            e.Writer.Write(Bogie2PositionAlongTrack);
            e.Writer.Write(Bogie2TrackName);
        }
    }
}
