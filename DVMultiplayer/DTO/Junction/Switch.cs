using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DV;

namespace DVMultiplayer.DTO.Junction
{
    class Switch : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public SwitchMode Mode { get; set; }
        public bool SwitchToLeft { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.Mode = (SwitchMode)e.Reader.ReadInt32();
            this.SwitchToLeft = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.Position.x);
            e.Writer.Write(this.Position.y);
            e.Writer.Write(this.Position.z);
            e.Writer.Write((int)this.Mode);
            e.Writer.Write(this.SwitchToLeft);
        }
    }

    public enum SwitchMode
    {
        REGULAR,
        FORCED,
        NO_SOUND,
    }
}
