using DVMultiplayer.Utils;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DVMultiplayer
{
    internal class CustomUI
    {
        internal static MenuScreen NetworkUI;
        internal static bool readyForCSUpdate = false;
        internal static bool CSUpdateFinished = false;

        internal static void Initialize()
        {
            try
            {
                GenerateNetworkUI();
            }
            catch (Exception ex)
            {
                Main.DebugLog($"{ex}");
            }
            readyForCSUpdate = true;
        }

        private static void GenerateNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            float xPosOffset = -557;

            GameObject refMenu = canvas.transform.Find("Main Menu").gameObject;
            GameObject mpMenuBuilder = Object.Instantiate(refMenu, canvas.transform);
            RectTransform screenTransform = mpMenuBuilder.GetComponent<RectTransform>();
            screenTransform.position = new Vector3(xPosOffset, screenTransform.position.y, screenTransform.position.z);

            TextMeshProUGUI menuTitle = mpMenuBuilder.transform.Find("title").GetComponent<TextMeshProUGUI>();
            menuTitle.text = "DV Multiplayer";

            DestroyChildren(mpMenuBuilder, "Button");
            DestroyChildren(mpMenuBuilder, "Section ");
            DestroyChildWithName(mpMenuBuilder, "Headset Info");
            DestroyChildWithName(mpMenuBuilder, "build version");

            ButtonBuilder closeButtonBuilder = new ButtonBuilder("Close", "UI_Close.png", mpMenuBuilder.transform, new Rect(65, -64.5f, 76, 76), RectTransformAnchoring.TopLeft, new Vector2(.5f, .5f), "Close menu");
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect", mpMenuBuilder.transform, new Rect(0f, -177.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");
            ButtonBuilder hostButtonBuilder = new ButtonBuilder("Host", "Host server", mpMenuBuilder.transform, new Rect(0f, -277.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");

            GameObject closeBtn = CreateSpriteButton(closeButtonBuilder);
            GameObject connectBtn = CreateTextButton(connectButtonBuilder);
            GameObject hostSection = CreateSection(new Rect(0f, -277, 458, 91.14999f), RectTransformAnchoring.TopCenter, mpMenuBuilder.transform);
            GameObject hostBtn = CreateTextButton(hostButtonBuilder);


            GameObject menu = Object.Instantiate(mpMenuBuilder, canvas.transform);
            Object.DestroyImmediate(mpMenuBuilder);
            NetworkUI = menu.GetComponent<MenuScreen>();
        }

        private static void DestroyChildren(GameObject gameObject, string prefixToDelete = "")
        {
            if (prefixToDelete != "")
            {
                for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
                {
                    if(gameObject.transform.GetChild(i).gameObject.name.StartsWith(prefixToDelete))
                        Object.DestroyImmediate(gameObject.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(gameObject.transform.GetChild(i).gameObject);
                }
            }
        }

        private static void DestroyChildWithName(GameObject gameObject, string name)
        {
            Object.DestroyImmediate(gameObject.transform.Find(name).gameObject);
        }

        private static GameObject CreateMenu(ButtonBuilder buttonBuilder)
        {
            if (buttonBuilder.type != ButtonType.Icon)
                return null;

            Texture2D texture = UUI.LoadTextureFromFile(buttonBuilder.icon);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

            GameObject newButton = buttonBuilder.btn;
            if (!newButton)
            {
                GameObject refSpriteButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Back to Game").gameObject;
                newButton = Object.Instantiate(refSpriteButton, buttonBuilder.parent);
            }
            UpdateButtonTransform(newButton, buttonBuilder);

            newButton.name = $"Button {buttonBuilder.name}";
            newButton.transform.Find("image").GetComponent<Image>().sprite = icon;

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            tooltip.tooltipEnabledText = buttonBuilder.tooltipText;
            tooltip.tooltipDisabledText = "";

            return newButton;
        }

        private static GameObject CreateSpriteButton(ButtonBuilder buttonBuilder)
        {
            if (buttonBuilder.type != ButtonType.Icon)
                return null;

            Texture2D texture = UUI.LoadTextureFromFile(buttonBuilder.icon);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

            GameObject newButton = buttonBuilder.btn;
            if (!newButton)
            {
                GameObject refSpriteButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Back to Game").gameObject;
                newButton = Object.Instantiate(refSpriteButton, buttonBuilder.parent);
            }
            UpdateButtonTransform(newButton, buttonBuilder);

            newButton.name = $"Button {buttonBuilder.name}";
            newButton.transform.Find("image").GetComponent<Image>().sprite = icon;

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            tooltip.tooltipEnabledText = buttonBuilder.tooltipText;
            tooltip.tooltipDisabledText = "";

            return newButton;
        }

        private static GameObject CreateTextButton(ButtonBuilder buttonBuilder)
        {
            if (buttonBuilder.type != ButtonType.Text)
                return null;

            GameObject newButton = buttonBuilder.btn;
            if (!newButton)
            {
                GameObject refSpriteButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Fast Travel Menu").Find("Button teleport with loco").gameObject;
                newButton = Object.Instantiate(refSpriteButton, buttonBuilder.parent);
            }

            UpdateButtonTransform(newButton, buttonBuilder);

            newButton.name = $"Button {buttonBuilder.name}";
            TextMeshProUGUI text = newButton.transform.Find("label").GetComponent<TextMeshProUGUI>();
            text.text = buttonBuilder.label;
            text.alignment = buttonBuilder.textAlignment;

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            tooltip.tooltipEnabledText = buttonBuilder.tooltipText;
            tooltip.tooltipDisabledText = "";

            return newButton;
        }

        private static GameObject CreateSection(Rect rect, RectTransformAnchoring anchor, Transform parent)
        {

            GameObject refSection = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Section").gameObject;
            GameObject newSection = Object.Instantiate(refSection, parent);
            RectTransform transform = newSection.GetComponent<RectTransform>();

            transform.ChangeAnchors(anchor);
            transform.ChangeRect(rect);

            return newSection;
        }

        private static void UpdateButtonTransform(GameObject btn, ButtonBuilder buttonBuilder)
        {
            RectTransform transform = btn.GetComponent<RectTransform>();

            if (buttonBuilder.anchor != RectTransformAnchoring.NotSet)
            {
                transform.ChangeAnchors(buttonBuilder.anchor);
            }

            if (buttonBuilder.pivot.HasValue)
            {
                transform.pivot = buttonBuilder.pivot.Value;
            }

            if (buttonBuilder.pos.HasValue)
            {
                transform.ChangeRect(buttonBuilder.pos.Value);
            }
        }

        struct MenuBuilder
        {
            public readonly string name;
            public readonly string title;
            public readonly Vector2 widthAndHeight;

        }

        struct ButtonBuilder
        {
            public readonly string name;
            public readonly ButtonType type;
            public readonly string icon;
            public readonly string label;
            public readonly Transform parent;
            public readonly GameObject btn;
            public readonly Rect? pos;
            public readonly TextAlignmentOptions textAlignment;
            public readonly RectTransformAnchoring anchor;
            public readonly Vector2? pivot;
            public readonly string tooltipText;

            public ButtonBuilder(string name, string icon, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, string tooltipText = "")
            {
                this.name = name;
                this.type = ButtonType.Icon;
                this.icon = icon;
                this.label = "";
                this.parent = parent;
                this.btn = null;
                this.pos = pos;
                this.textAlignment = TextAlignmentOptions.Left;
                this.anchor = anchor;
                this.pivot = pivot;
                this.tooltipText = tooltipText;
            }

            public ButtonBuilder(string name, string label, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, TextAlignmentOptions textAlignment = TextAlignmentOptions.Left, string tooltipText = "")
            {
                this.name = name;
                this.type = ButtonType.Text;
                this.icon = "";
                this.label = label;
                this.parent = parent;
                this.btn = null;
                this.pos = pos;
                this.textAlignment = textAlignment;
                this.anchor = anchor;
                this.pivot = pivot;
                this.tooltipText = tooltipText;
            }

            public ButtonBuilder(string name, string icon, Transform parent, GameObject btn = null, string tooltipText = "")
            {
                this.name = name;
                this.type = ButtonType.Icon;
                this.icon = icon;
                this.label = "";
                this.parent = parent;
                this.btn = btn;
                this.pos = null;
                this.textAlignment = TextAlignmentOptions.Left;
                this.anchor = RectTransformAnchoring.NotSet;
                this.pivot = null;
                this.tooltipText = tooltipText;
            }

            public ButtonBuilder(string name, string label, Transform parent, TextAlignmentOptions textAlignment = TextAlignmentOptions.Left, GameObject btn = null, string tooltipText = "")
            {
                this.name = name;
                this.type = ButtonType.Text;
                this.icon = "";
                this.label = label;
                this.parent = parent;
                this.btn = btn;
                this.pos = null;
                this.textAlignment = textAlignment;
                this.anchor = RectTransformAnchoring.NotSet;
                this.pivot = null;
                this.tooltipText = tooltipText;
            }
        }

        enum ButtonType
        {
            Icon,
            Text
        }
    }
}