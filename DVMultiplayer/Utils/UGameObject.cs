using UnityEngine;

namespace DVMultiplayer.Utils
{
    public static class UGameObject
    {
        public static bool Exists(string name)
        {
            return GameObject.Find(name) != null;
        }
    }
}
