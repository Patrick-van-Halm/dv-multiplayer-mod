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
        internal static MenuScreen ConnectMenuUI;
        internal static MenuScreen InputScreenUI;
        internal static MenuScreen HostMenuUI;
        internal static MenuScreen currentScreen;
        internal static bool readyForCSUpdate = false;
        internal static bool CSUpdateFinished = false;
        internal static float menuOffset = -500;

        internal static void Initialize()
        {
            try
            {
                GenerateNetworkUI();
                GenerateConnectUI();
                GenerateInputScreenUI();
                GenerateHostUI();
            }
            catch (Exception ex)
            {
                Main.DebugLog($"{ex}");
            }
            readyForCSUpdate = true;
        }

        internal static void Open(MenuScreen screen)
        {
            currentScreen = screen;
            SingletonBehaviour<CanvasSpawner>.Instance.Open(screen);
        }

        private static void GenerateNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject mpMenuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Main Menu", "DV Multiplayer", 508, 460, true));
            
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect", mpMenuBuilder.transform, new Rect(0f, -177.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");
            ButtonBuilder hostButtonBuilder = new ButtonBuilder("Host", "Host server", mpMenuBuilder.transform, new Rect(0f, -277.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");

            GameObject connectSection = CreateSection(new Rect(0f, -177, 458, 91.14999f), RectTransformAnchoring.TopCenter, mpMenuBuilder.transform);
            GameObject connectBtn = CreateButton(connectButtonBuilder);
            GameObject hostSection = CreateSection(new Rect(0f, -277, 458, 91.14999f), RectTransformAnchoring.TopCenter, mpMenuBuilder.transform);
            GameObject hostBtn = CreateButton(hostButtonBuilder);

            GameObject menu = Object.Instantiate(mpMenuBuilder, canvas.transform);
            Object.DestroyImmediate(mpMenuBuilder);
            NetworkUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateConnectUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject connectMenu = CreateMenu(new MenuBuilder("DVMultiplayer Connect", "Connect", 975, 540f, false, false));

            TextFieldBuilder inputIpField = new TextFieldBuilder("IP", connectMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            TextFieldBuilder inputPortField = new TextFieldBuilder("Port", connectMenu.transform, new Rect(-32f, -315, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f), true);
            TextFieldBuilder inputUsernameField = new TextFieldBuilder("Username", connectMenu.transform, new Rect(-32f, -415, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect", connectMenu.transform, new Rect(0f, -515, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            GameObject ipSection = CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, connectMenu.transform);
            GameObject inputFieldIPLabel = CreateLabel("IP", "IP:", connectMenu.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldIP = CreateTextField(inputIpField);

            GameObject portSection = CreateSection(new Rect(0f, -277, 925, 91.14999f), RectTransformAnchoring.TopCenter, connectMenu.transform);
            GameObject inputFieldPortLabel = CreateLabel("Port", "Port:", connectMenu.transform, new Rect(32, -315, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldPort = CreateTextField(inputPortField);

            GameObject usernameSection = CreateSection(new Rect(0f, -377, 925, 91.14999f), RectTransformAnchoring.TopCenter, connectMenu.transform);
            GameObject inputFieldUsernameLabel = CreateLabel("Username", "Username:", connectMenu.transform, new Rect(32, -415, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldUsername = CreateTextField(inputUsernameField);

            GameObject connectBtn = CreateButton(connectButtonBuilder);

            GameObject menu = Object.Instantiate(connectMenu, canvas.transform);
            Object.DestroyImmediate(connectMenu);
            ConnectMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateHostUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject hostMenu = CreateMenu(new MenuBuilder("DVMultiplayer Host", "Host", 975, 440f, false, false));

            TextFieldBuilder inputPortField = new TextFieldBuilder("Port", hostMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f), true);
            TextFieldBuilder inputUsernameField = new TextFieldBuilder("Username", hostMenu.transform, new Rect(-32f, -315, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Host", "Host", hostMenu.transform, new Rect(0f, -415, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            GameObject portSection = CreateSection(new Rect(0f, -277, 925, 91.14999f), RectTransformAnchoring.TopCenter, hostMenu.transform);
            GameObject inputFieldPortLabel = CreateLabel("Port", "Port:", hostMenu.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldPort = CreateTextField(inputPortField);

            GameObject usernameSection = CreateSection(new Rect(0f, -377, 925, 91.14999f), RectTransformAnchoring.TopCenter, hostMenu.transform);
            GameObject inputFieldUsernameLabel = CreateLabel("Username", "Username:", hostMenu.transform, new Rect(32, -315, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldUsername = CreateTextField(inputUsernameField);

            GameObject hostBtn = CreateButton(connectButtonBuilder);

            GameObject menu = Object.Instantiate(hostMenu, canvas.transform);
            Object.DestroyImmediate(hostMenu);
            HostMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateInputScreenUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject inputMenu = CreateMenu(new MenuBuilder("DVMultiplayer InputMenu", "Input", 975, 672.6f, false, false));
            InputScreen input = inputMenu.AddComponent<InputScreen>();

            GameObject inputSection = CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, inputMenu.transform);
            GameObject currentInput = CreateLabel("Input", "", inputMenu.transform, new Rect(32, -215, 861, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));

            char[] row = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-' };
            for(int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", inputMenu.transform, new Rect(925 - (80 * 11) + (80 * i), -300, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            row = new char[] { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p'};
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", inputMenu.transform, new Rect(925 - (80 * 10.5f) + (80 * i), -380, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }
            
            row = new char[] { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l'};
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", inputMenu.transform, new Rect(925 - (80 * 10) + (80 * i), -460, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            row = new char[] { 'z', 'x', 'c', 'v', 'b', 'n', 'm', '.'};
            for (int i = 0; i < row.Length; i++)
            {
                char key = row[i];
                ButtonBuilder builder = new ButtonBuilder($"Key {key}", $"{key}", inputMenu.transform, new Rect(925 - (80 * 9.5f) + (80 * i), -540, 75, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
                GameObject keyInputBtn = CreateButton(builder);
                InputButton btn = keyInputBtn.AddComponent<InputButton>();
                btn.key = key;
            }

            ButtonBuilder builderBtn = new ButtonBuilder($"Backspace", $"Backspace", inputMenu.transform, new Rect(925 - (80 * 9f) + 44, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject backspaceBtn = CreateButton(builderBtn);
            InputButton inputbtn = backspaceBtn.AddComponent<InputButton>();
            inputbtn.isBackspace = true;

            builderBtn = new ButtonBuilder($"Casing", $"Uppercase", inputMenu.transform, new Rect(925 - (80 * 9f) + 229, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject caseBtn = CreateButton(builderBtn);

            builderBtn = new ButtonBuilder($"Confirm", $"Confirm", inputMenu.transform, new Rect(925 - (80 * 9f) + 414, -620, 150, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject confirmBtn = CreateButton(builderBtn);

            GameObject menu = Object.Instantiate(inputMenu, canvas.transform);
            Object.DestroyImmediate(inputMenu);
            InputScreenUI = menu.GetComponent<MenuScreen>();
        }

        private static GameObject CreateMenu(MenuBuilder menuBuilder)
        {
            menuOffset -= menuBuilder.widthAndHeight.x + 100;

            GameObject refMenu = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").gameObject;
            GameObject newMenu = Object.Instantiate(refMenu, SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform);
            newMenu.name = menuBuilder.name;
            DestroyChildren(newMenu);

            RectTransform transform = newMenu.GetComponent<RectTransform>();
            transform.sizeDelta = menuBuilder.widthAndHeight;
            transform.localPosition = new Vector3(menuOffset, 0);

            GameObject section = CreateTextBackground(new Rect(0, -24.39999f, 458, 80), RectTransformAnchoring.TopCenter, newMenu.transform, new Vector2(.5f, 1));
            transform = section.GetComponent<RectTransform>();
            transform.sizeDelta = new Vector2(menuBuilder.widthAndHeight.x - 50, transform.sizeDelta.y);

            ButtonBuilder closeButtonBuilder = new ButtonBuilder("Close", (menuBuilder.isClose ? "UI_Close.png" : "UI_ArrowLeft.png"), newMenu.transform, new Rect(65, -64.5f, 76, 76), RectTransformAnchoring.TopLeft, new Vector2(.5f, .5f), "Close menu");
            GameObject closeBtn = CreateButton(closeButtonBuilder);

            GameObject refTitle = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("title").gameObject;
            GameObject title = Object.Instantiate(refTitle, newMenu.transform);
            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            title.name = "Title";
            titleText.text = menuBuilder.title;

            if (menuBuilder.withTooltip)
            {
                GameObject tooltipSection = CreateTextBackground(new Rect(0, 70, 458, 76), RectTransformAnchoring.BottomCenter, newMenu.transform, new Vector2(.5f, .5f));
                transform = tooltipSection.GetComponent<RectTransform>();
                transform.sizeDelta = new Vector2(menuBuilder.widthAndHeight.x - 50, transform.sizeDelta.y);

                GameObject refTooltip = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Tooltip").gameObject;
                GameObject tooltip = Object.Instantiate(refTooltip, newMenu.transform);
                transform = tooltip.GetComponent<RectTransform>();
                transform.ChangeRect(new Rect(0, 96.5f, menuBuilder.widthAndHeight.x - 83, transform.sizeDelta.y));
            }

            return newMenu;
        }

        private static GameObject CreateButton(ButtonBuilder buttonBuilder)
        {
            GameObject newButton = buttonBuilder.btn;
            if (!newButton && buttonBuilder.type == ButtonType.Text)
            {
                GameObject refButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Fast Travel Menu").Find("Button teleport with loco").gameObject;
                newButton = Object.Instantiate(refButton, buttonBuilder.parent);
            }
            else if(!newButton && buttonBuilder.type == ButtonType.Icon)
            {
                GameObject refSpriteButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Button Back to Game").gameObject;
                newButton = Object.Instantiate(refSpriteButton, buttonBuilder.parent);
            }

            UpdateButtonTransform(newButton, buttonBuilder);

            newButton.name = $"Button {buttonBuilder.name}";
            if(buttonBuilder.type == ButtonType.Icon)
            {
                Texture2D texture = UUI.LoadTextureFromFile(buttonBuilder.icon);
                Sprite icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                newButton.transform.Find("image").GetComponent<Image>().sprite = icon;
            }
            else
            {
                TextMeshProUGUI text = newButton.transform.Find("label").GetComponent<TextMeshProUGUI>();
                text.text = buttonBuilder.label;
                text.alignment = buttonBuilder.textAlignment;
            }

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            tooltip.tooltipEnabledText = buttonBuilder.tooltipText;
            tooltip.tooltipDisabledText = "";

            return newButton;
        }

        private static GameObject CreateLabel(string name, string labelText, Transform parent, Rect rect, FontStyles fontStyle, TextAlignmentOptions textAlignment, RectTransformAnchoring anchor, Vector2 pivot)
        {
            GameObject refLabel = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("title").gameObject;
            GameObject label = Object.Instantiate(refLabel, parent);
            label.name = $"Label {name}";

            TextMeshProUGUI titleText = label.GetComponent<TextMeshProUGUI>();
            titleText.text = labelText;
            titleText.alignment = textAlignment;
            titleText.fontStyle = fontStyle;

            RectTransform transform = label.GetComponent<RectTransform>();
            transform.pivot = pivot;
            transform.ChangeAnchors(anchor);
            transform.ChangeRect(rect);

            return label;
        }

        private static GameObject CreateTextField(TextFieldBuilder textFieldBuilder)
        {
            GameObject refButton = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Fast Travel Menu").Find("Button teleport with loco").gameObject;
            GameObject newTextField = Object.Instantiate(refButton, textFieldBuilder.parent);
            UpdateTextFieldTransform(newTextField, textFieldBuilder);

            newTextField.name = $"[INPUT] {textFieldBuilder.name}";
            TextMeshProUGUI text = newTextField.transform.Find("label").GetComponent<TextMeshProUGUI>();
            text.alignment = textFieldBuilder.textAlignment;
            text.text = "";

            TextField field = newTextField.AddComponent<TextField>();
            field.title = textFieldBuilder.name;
            field.isDigitOnly = textFieldBuilder.isDigitOnly;
            
            return newTextField;
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

        private static GameObject CreateTextBackground(Rect rect, RectTransformAnchoring anchor, Transform parent, Vector2 pivot)
        {
            GameObject refSection = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Title bg").gameObject;
            GameObject newTextBackground = Object.Instantiate(refSection, parent);
            RectTransform transform = newTextBackground.GetComponent<RectTransform>();

            transform.ChangeAnchors(anchor);
            transform.ChangeRect(rect);
            transform.pivot = pivot;

            return newTextBackground;
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

            if (buttonBuilder.rect.HasValue)
            {
                transform.ChangeRect(buttonBuilder.rect.Value);
                transform.Find("image hover").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
                transform.Find("image click").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
                if (buttonBuilder.type == ButtonType.Icon)
                    transform.Find("image").GetComponent<RectTransform>().ChangeRect(new Rect(0,0, buttonBuilder.rect.Value.width, buttonBuilder.rect.Value.height));
            }
        }

        private static void UpdateTextFieldTransform(GameObject btn, TextFieldBuilder textFieldBuilder)
        {
            RectTransform transform = btn.GetComponent<RectTransform>();

            if (textFieldBuilder.anchor != RectTransformAnchoring.NotSet)
            {
                transform.ChangeAnchors(textFieldBuilder.anchor);
            }

            if (textFieldBuilder.pivot.HasValue)
            {
                transform.pivot = textFieldBuilder.pivot.Value;
            }

            if (textFieldBuilder.rect.HasValue)
            {
                transform.ChangeRect(textFieldBuilder.rect.Value);
                transform.Find("image hover").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, textFieldBuilder.rect.Value.width, textFieldBuilder.rect.Value.height));
                transform.Find("image click").GetComponent<RectTransform>().ChangeRect(new Rect(0, 0, textFieldBuilder.rect.Value.width, textFieldBuilder.rect.Value.height));
            }
        }

        struct MenuBuilder
        {
            public readonly string name;
            public readonly string title;
            public readonly Vector2 widthAndHeight;
            public readonly bool withTooltip;
            public readonly bool isClose;

            public MenuBuilder(string name, string title, float width, float height, bool isClose)
            {
                this.name = name;
                this.title = title;
                this.widthAndHeight = new Vector2(width, height);
                withTooltip = true;
                this.isClose = isClose;
            }

            public MenuBuilder(string name, string title, float width, float height, bool isClose, bool tooltip)
            {
                this.name = name;
                this.title = title;
                this.widthAndHeight = new Vector2(width, height);
                withTooltip = tooltip;
                this.isClose = isClose;
            }
        }

        struct ButtonBuilder
        {
            public readonly string name;
            public readonly ButtonType type;
            public readonly string icon;
            public readonly string label;
            public readonly Transform parent;
            public readonly GameObject btn;
            public readonly Rect? rect;
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
                this.rect = pos;
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
                this.rect = pos;
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
                this.rect = null;
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
                this.rect = null;
                this.textAlignment = textAlignment;
                this.anchor = RectTransformAnchoring.NotSet;
                this.pivot = null;
                this.tooltipText = tooltipText;
            }
        }

        struct TextFieldBuilder
        {
            public readonly string name;
            public readonly Transform parent;
            public readonly Rect? rect;
            public readonly TextAlignmentOptions textAlignment;
            public readonly RectTransformAnchoring anchor;
            public readonly Vector2? pivot;
            public readonly bool isDigitOnly;

            public TextFieldBuilder(string name, Transform parent, Rect pos, TextAlignmentOptions textAlignment, RectTransformAnchoring anchor, Vector2 pivot, bool isDigitOnly = false)
            {
                this.name = name;
                this.parent = parent;
                this.rect = pos;
                this.textAlignment = textAlignment;
                this.anchor = anchor;
                this.pivot = pivot;
                this.isDigitOnly = isDigitOnly;
            }
        }

        enum ButtonType
        {
            Icon,
            Text
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
    }
}