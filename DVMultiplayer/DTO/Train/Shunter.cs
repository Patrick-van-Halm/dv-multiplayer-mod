using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Shunter : Locomotive
    {
        public bool IsEngineOn { get; set; } = false;
        public bool IsSideFuse1On { get; set; } = false;
        public bool IsSideFuse2On { get; set; } = false;
        public bool IsMainFuseOn { get; set; } = false;
        public MultipleUnit MultipleUnit { get; set; } = new MultipleUnit();

        public override void Deserialize(DeserializeEvent e)
        {
            IsEngineOn = e.Reader.ReadBoolean();
            IsSideFuse1On = e.Reader.ReadBoolean();
            IsSideFuse2On = e.Reader.ReadBoolean();
            IsMainFuseOn = e.Reader.ReadBoolean();
            MultipleUnit = e.Reader.ReadSerializable<MultipleUnit>();
        }

        public override void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsEngineOn);
            e.Writer.Write(IsSideFuse1On);
            e.Writer.Write(IsSideFuse2On);
            e.Writer.Write(IsMainFuseOn);
            e.Writer.Write(MultipleUnit);
        }
    }
}
