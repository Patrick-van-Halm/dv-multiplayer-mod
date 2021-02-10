using DarkRift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class PlayerLoaded : IDarkRiftSerializable
    {
        public ushort Id { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
        }
    }
}