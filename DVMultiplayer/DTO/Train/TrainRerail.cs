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
    public class TrainRerail : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }
        public string TrackName { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.TrainId = e.Reader.ReadString();
            this.Forward = e.Reader.ReadVector3();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            TrackName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.TrainId);
            e.Writer.Write(Forward);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(TrackName);
        }
    }
}
