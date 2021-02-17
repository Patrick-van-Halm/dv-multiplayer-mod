using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class CarDamage : IDarkRiftSerializable
    {
        public string Guid { get; set; }
        public DamageType DamageType { get; set; }
        public float Damage { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            DamageType = (DamageType)e.Reader.ReadUInt16();
            Damage = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write((ushort)DamageType);
            e.Writer.Write(Damage);
        }
    }

    public enum DamageType
    {
        Car,
        Cargo
    }
}
