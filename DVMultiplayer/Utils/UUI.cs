using DV;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

        public static Texture2D LoadTextureFromFile(string filename)
        {
            if (!File.Exists($"./Mods/DVMultiplayer/Resources/Textures/{filename}"))
                return null;

            Texture2D texture = new Texture2D(512, 512);
            if (texture.LoadImage(File.ReadAllBytes($"./Mods/DVMultiplayer/Resources/Textures/{filename}")))
                return texture;
            else
                return null;
        }

        public static void ChangeAnchors(this RectTransform rt, RectTransformAnchoring anchor)
        {
            switch (anchor)
            {
                case RectTransformAnchoring.TopLeft:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    break;

                case RectTransformAnchoring.TopCenter:
                    rt.anchorMin = new Vector2(.5f, 1);
                    rt.anchorMax = new Vector2(.5f, 1);
                    break;
            }
        }

        public static void ChangeRect(this RectTransform rt, Rect rect)
        {
            if (rt.anchorMax.x != 1 || rt.anchorMax.y != 1)
            {
                rt.anchoredPosition = new Vector2(rect.x, rect.y);
                rt.sizeDelta = new Vector2(rect.width, rect.height);
            }
        }
    }

    public enum RectTransformAnchoring
    {
        NotSet,
        TopLeft,
        TopCenter,
        TopRight
    }
}
