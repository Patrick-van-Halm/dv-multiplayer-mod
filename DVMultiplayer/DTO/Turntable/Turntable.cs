using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Turntable
{
    public class Turntable : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public float? Rotation { get; set; }
        public float? LeverAngle { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadNullableSingle();
            LeverAngle = e.Reader.ReadNullableSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(LeverAngle);
        }
    }
}
