using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public abstract class Locomotive : IDarkRiftSerializable
    {
        public abstract void Deserialize(DeserializeEvent e);

        public abstract void Serialize(SerializeEvent e);
    }
}
