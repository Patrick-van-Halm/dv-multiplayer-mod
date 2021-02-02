using DarkRift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class NPlayer : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Username { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 WorldMoverPos { get; set; }
        public string[] Mods { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Id = e.Reader.ReadUInt16();
            this.Username = e.Reader.ReadString();
            this.Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.Rotation = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.WorldMoverPos = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.Mods = e.Reader.ReadStrings();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.Id);
            e.Writer.Write(this.Username);
            e.Writer.Write(this.Position.x);
            e.Writer.Write(this.Position.y);
            e.Writer.Write(this.Position.z);
            e.Writer.Write(this.Rotation.x);
            e.Writer.Write(this.Rotation.y);
            e.Writer.Write(this.Rotation.z);
            e.Writer.Write(this.Rotation.w);
            e.Writer.Write(this.WorldMoverPos.x);
            e.Writer.Write(this.WorldMoverPos.y);
            e.Writer.Write(this.WorldMoverPos.z);
            e.Writer.Write(this.Mods);
        }
    }
}