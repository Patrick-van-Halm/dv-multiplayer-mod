using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Savegame
{
    class OfflineSaveGame
    {
        public string SaveDataCars { get; set; }
        public JobsSaveGameData SaveDataJobs { get; set; }
        public string SaveDataSwitches { get; set; } = "";
        public string SaveDataTurntables { get; set; } = "";
    }
}
