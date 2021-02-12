using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Junction
{
    public class Switch : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public SwitchMode Mode { get; set; }
        public bool SwitchToLeft { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Position = e.Reader.ReadVector3();
            Mode = (SwitchMode)e.Reader.ReadInt32();
            SwitchToLeft = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
            e.Writer.Write((int)Mode);
            e.Writer.Write(SwitchToLeft);
        }
    }

    public enum SwitchMode
    {
        REGULAR,
        FORCED,
        NO_SOUND,
    }
}
