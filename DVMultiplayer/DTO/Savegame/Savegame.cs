using DarkRift;

namespace DVMultiplayer.DTO.Savegame
{
    public class SaveGame : IDarkRiftSerializable
    {
        public string SaveDataDestroyedLocoDebt { get; internal set; } = "";
        public string SaveDataStagedJobDebt { get; internal set; } = "";
        public string SaveDataDeletedJoblessCarsDept { get; internal set; } = "";
        public string SaveDataInsuranceDept { get; internal set; } = "";

        public void Deserialize(DeserializeEvent e)
        {
            SaveDataDestroyedLocoDebt = e.Reader.ReadString();
            SaveDataStagedJobDebt = e.Reader.ReadString();
            SaveDataDeletedJoblessCarsDept = e.Reader.ReadString();
            SaveDataInsuranceDept = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(SaveDataDestroyedLocoDebt);
            e.Writer.Write(SaveDataStagedJobDebt);
            e.Writer.Write(SaveDataDeletedJoblessCarsDept);
            e.Writer.Write(SaveDataInsuranceDept);
        }
    }
}
