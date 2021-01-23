using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public enum Levers
    {
        Throttle,
        Brake,
        IndependentBrake,
        Reverser,
        Sander,
        SideFuse_1,
        SideFuse_2,
        SideFuse_3,
        MainFuse,
        FusePowerStarter,
        Horn
    }

    public class TrainLever : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Levers Lever { get; set; }
        public float Value { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.TrainId = e.Reader.ReadString();
            this.Lever = (Levers) e.Reader.ReadUInt32();
            this.Value = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.TrainId);
            e.Writer.Write((uint) this.Lever);
            e.Writer.Write(this.Value);
        }
    }
}
