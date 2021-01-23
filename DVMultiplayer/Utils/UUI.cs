using DV;
using UnityEngine;

namespace DVMultiplayer.Utils
{
    public static class UUI
    {
        public static GUIStyle GenerateStyle(Color? textColor = null, int fontSize = 12, TextAnchor allignment = TextAnchor.MiddleLeft)
        {
            GUIStyle style = new GUIStyle();

            style.normal.textColor = textColor == null ? Color.white : (Color)textColor;
            style.fontSize = fontSize;
            style.alignment = allignment;

            return style;
        }

        public static void UnlockMouse(bool value)
        {
            if (value)
                SingletonBehaviour<AppUtil>.Instance.RequireCursor();
            else
                SingletonBehaviour<AppUtil>.Instance.ReleaseCursor();
        }
    }
}
