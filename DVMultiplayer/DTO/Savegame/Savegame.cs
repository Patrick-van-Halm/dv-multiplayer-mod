using DarkRift;

namespace DVMultiplayer.DTO.Savegame
{
    public class SaveGame : IDarkRiftSerializable
    {
        public string SaveDataSwitches { get; set; } = "";
        public string SaveDataTurntables { get; set; } = "";

        public void Deserialize(DeserializeEvent e)
        {
            SaveDataSwitches = e.Reader.ReadString();
            SaveDataTurntables = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(SaveDataSwitches);
            e.Writer.Write(SaveDataTurntables);
        }
    }
}
