using DarkRift;
using DVMultiplayer.Darkrift;
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
        public string SaveDataCars { get; set; }
        public string SaveDataSwitches { get; set; }
        public string SaveDataTurntables { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            SaveDataCars = e.Reader.ReadString();
            SaveDataSwitches = e.Reader.ReadString();
            SaveDataTurntables = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(SaveDataCars);
            e.Writer.Write(SaveDataSwitches);
            e.Writer.Write(SaveDataTurntables);
        }
    }
}
