using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainDerail : IDarkRiftSerializable
    {
        public string TrainId { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            this.TrainId = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.TrainId);
        }
    }
}
