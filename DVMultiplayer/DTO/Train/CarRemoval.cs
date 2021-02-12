using DarkRift;
using DVMultiplayer.Darkrift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public class CarRemoval : IDarkRiftSerializable
    {
        public string Guid { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
        }
    }
}
