using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class WorldMove : IDarkRiftSerializable
    {
        public ushort PlayerId { get; set; }
        public Vector3 WorldPosition { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            PlayerId = e.Reader.ReadUInt16();
            WorldPosition = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(PlayerId);
            e.Writer.Write(WorldPosition.x);
            e.Writer.Write(WorldPosition.y);
            e.Writer.Write(WorldPosition.z);
        }
    }
}
