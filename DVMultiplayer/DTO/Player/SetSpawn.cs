using DarkRift;
using DVMultiplayer.Darkrift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class SetSpawn : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Position = e.Reader.ReadVector3();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
        }
    }
}
