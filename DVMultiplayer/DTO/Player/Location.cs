using DarkRift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class Location : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Id = e.Reader.ReadUInt16();
            this.Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.Rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.Id);
            e.Writer.Write(this.Position.x);
            e.Writer.Write(this.Position.y);
            e.Writer.Write(this.Position.z);
            e.Writer.Write(this.Rotation.x);
            e.Writer.Write(this.Rotation.y);
            e.Writer.Write(this.Rotation.z);
            e.Writer.Write(this.Rotation.w);
        }
    }
}