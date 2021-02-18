﻿using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class CarDamage : IDarkRiftSerializable
    {
        public string Guid { get; set; }
        public DamageType DamageType { get; set; }
        public float NewHealth { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            DamageType = (DamageType)e.Reader.ReadUInt16();
            NewHealth = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write((ushort)DamageType);
            e.Writer.Write(NewHealth);
        }
    }

    public enum DamageType
    {
        Car,
        Cargo
    }
}
