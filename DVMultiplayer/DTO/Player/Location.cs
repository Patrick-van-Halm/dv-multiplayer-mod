using DarkRift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class Location : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public Vector3 AbsPosition { get; set; }
        public Quaternion NewRotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Id = e.Reader.ReadUInt16();
            this.AbsPosition = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.NewRotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.Id);
            e.Writer.Write(this.AbsPosition.x);
            e.Writer.Write(this.AbsPosition.y);
            e.Writer.Write(this.AbsPosition.z);
            e.Writer.Write(this.NewRotation.x);
            e.Writer.Write(this.NewRotation.y);
            e.Writer.Write(this.NewRotation.z);
            e.Writer.Write(this.NewRotation.w);
        }
    }
}