using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Steamer : Locomotive
    {
        public bool IsFireOn { get; set; } = false;
        public float Coal { get; set; } = 0;
        public float FireboxDoor { get; set; } = 0;
        public float DraftPuller { get; set; } = 0;
        public float WaterInjector { get; set; } = 0;
        public float Blower { get; set; } = 0;
        

        public override void Deserialize(DeserializeEvent e)
        {
            IsFireOn = e.Reader.ReadBoolean();
            Coal = e.Reader.ReadSingle();
            FireboxDoor = e.Reader.ReadSingle();
            DraftPuller = e.Reader.ReadSingle();
            WaterInjector = e.Reader.ReadSingle();
            Blower = e.Reader.ReadSingle();
        }

        public override void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsFireOn);
            e.Writer.Write(Coal);
            e.Writer.Write(FireboxDoor);
            e.Writer.Write(DraftPuller);
            e.Writer.Write(WaterInjector);
            e.Writer.Write(Blower);
        }
    }
}
