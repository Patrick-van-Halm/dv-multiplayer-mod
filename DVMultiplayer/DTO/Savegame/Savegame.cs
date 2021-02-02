using DarkRift;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Savegame
{
    public class SaveGame : IDarkRiftSerializable
    {
        public Vector3 PlayerPos { get; set; }
        public string SaveDataCars { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            SaveDataCars = e.Reader.ReadString();
            PlayerPos = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(SaveDataCars);
            e.Writer.Write(this.PlayerPos.x);
            e.Writer.Write(this.PlayerPos.y);
            e.Writer.Write(this.PlayerPos.z);
        }
    }
}
