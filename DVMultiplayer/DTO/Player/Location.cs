using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class Location : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion? Rotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadNullableQuaternion();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
        }
    }
}