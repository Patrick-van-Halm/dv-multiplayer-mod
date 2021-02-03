using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DV;
using DVMultiplayer.Darkrift;

namespace DVMultiplayer.DTO.Turntable
{
    public class Turntable : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
        }
    }
}
