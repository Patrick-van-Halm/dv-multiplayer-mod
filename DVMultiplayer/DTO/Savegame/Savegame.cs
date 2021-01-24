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
        public string SaveDataString { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            SaveDataString = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(SaveDataString);
        }
    }
}
