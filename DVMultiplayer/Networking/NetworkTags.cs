using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Networking
{
    public enum NetworkTags:ushort
    {
        TEST_TAG,
        PLAYER_SPAWN,
        PLAYER_LOCATION_UPDATE,
        PLAYER_DISCONNECT,
        PLAYER_WORLDMOVED,
        TRAIN_LEVER,
        TRAIN_SWITCH,
        TRAIN_LOCATION_UPDATE,
        TRAIN_DERAIL,
        TRAIN_COUPLE,
        TRAIN_COUPLE_HOSE,
        TRAIN_COUPLE_COCK,
        TRAIN_UNCOUPLE,
        SWITCH_CHANGED,
        SAVEGAME_UPDATE,
        SAVEGAME_GET,
    }
}
