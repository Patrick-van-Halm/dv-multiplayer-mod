﻿using DVMultiplayer.Utils;
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
        internal static MenuScreen SaveFavoriteMenuUI;
        internal static MenuScreen FavoriteConnectMenuUI;
        internal static MenuScreen UsernameRequestMenuUI;
        internal static MenuScreen HostConnectedMenuUI;
        internal static MenuScreen ClientConnectedMenuUI;
        internal static MenuScreen ModMismatchScreen;
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
                GenerateFavoriteUI();
                GenerateFavoriteListUI();
                GenerateRequestUsernameUI();
                GenerateHostNetworkUI();
                GenerateClientNetworkUI();
                GenerateModMismatchScreenUI();
                Open(ModMismatchScreen);
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

        internal static void Close()
        {
            currentScreen = null;
            SingletonBehaviour<CanvasSpawner>.Instance.Close();
        }

        private static void GenerateModMismatchScreenUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer ModMismatched", "Mod mismatch", 800, 500, true, false));

            CreateSection(new Rect(0f, -245, 750, 255), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            //Max 11 mods or if mod amount > 10 show "and {amount} more"
            CreateLabel("ModsMismatched", "Your mods and the mods of the host mismatched.\n[MISSING] Mod1\n[MISSING] Mod2\n[MISSING] Mod3\n[MISSING] Mod4\n[REMOVE]\n[REMOVE]\n[REMOVE]\n[REMOVE]\n[REMOVE]\n[REMOVE]\n[REMOVE]", menuBuilder.transform, new Rect(32f, -125, 740, 250), FontStyles.Normal, TextAlignmentOptions.TopLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 1f), Color.white, 18);
            CreateSection(new Rect(0f, 115, 750, 91f), RectTransformAnchoring.BottomCenter, menuBuilder.transform, new Vector2(.5f, 1));
            ButtonBuilder buttonBuilder = new ButtonBuilder("Ok", "OK", menuBuilder.transform, new Rect(0f, 107, 608, 76), RectTransformAnchoring.BottomCenter, new Vector2(.5f, 1f), TextAlignmentOptions.Center);
            CreateButton(buttonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            ModMismatchScreen = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject mpMenuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Main Menu", "DV Multiplayer", 508, 560, true));

            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect Manually", mpMenuBuilder.transform, new Rect(0f, -177.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an existing server");
            ButtonBuilder connectFavButtonBuilder = new ButtonBuilder("Connect to Favorite", "Connect to Favorite", mpMenuBuilder.transform, new Rect(0f, -257.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Connect to an favorited server");
            ButtonBuilder hostButtonBuilder = new ButtonBuilder("Host", "Host server", mpMenuBuilder.transform, new Rect(0f, -377.5f, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center, "Host a new server");

            GameObject connectSection = CreateSection(new Rect(0f, -218, 458, 168), RectTransformAnchoring.TopCenter, mpMenuBuilder.transform);
            GameObject connectBtn = CreateButton(connectButtonBuilder);
            GameObject connectFavBtn = CreateButton(connectFavButtonBuilder);
            GameObject hostSection = CreateSection(new Rect(0f, -377, 458, 91f), RectTransformAnchoring.TopCenter, mpMenuBuilder.transform);
            GameObject hostBtn = CreateButton(hostButtonBuilder);

            GameObject menu = Object.Instantiate(mpMenuBuilder, canvas.transform);
            Object.DestroyImmediate(mpMenuBuilder);
            NetworkUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateHostNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Hosting", "Hosting", 668, 418, true, false));

            ButtonBuilder stopServerButtonBuilder = new ButtonBuilder("Stop Server", "Stop Server", menuBuilder.transform, new Rect(0f, -318.5f, 608, 91f), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center);

            GameObject usernameSection = CreateSection(new Rect(0f, -177, 618, 91), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            GameObject usernameLbl = CreateLabel("Username", "Connected as: ", menuBuilder.transform, new Rect(0f, -177.5f, 608, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f));
            GameObject hostSection = CreateSection(new Rect(0f, -318, 618, 91f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            GameObject hostBtn = CreateButton(stopServerButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            HostConnectedMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateClientNetworkUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject menuBuilder = CreateMenu(new MenuBuilder("DVMultiplayer Hosting", "Connected To Server", 668, 418, true, false));

            ButtonBuilder stopServerButtonBuilder = new ButtonBuilder("Disconnect", "Disconnect", menuBuilder.transform, new Rect(0f, -318.5f, 608, 91f), RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f), TextAlignmentOptions.Center);

            GameObject usernameSection = CreateSection(new Rect(0f, -177, 618, 91), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            GameObject usernameLbl = CreateLabel("Username", "Connected as: ", menuBuilder.transform, new Rect(0f, -177.5f, 608, 76), FontStyles.Normal, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopCenter, new Vector2(.5f, .5f));
            GameObject hostSection = CreateSection(new Rect(0f, -318, 618, 91f), RectTransformAnchoring.TopCenter, menuBuilder.transform);
            GameObject hostBtn = CreateButton(stopServerButtonBuilder);

            GameObject menu = Object.Instantiate(menuBuilder, canvas.transform);
            Object.DestroyImmediate(menuBuilder);
            ClientConnectedMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateConnectUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject connectMenu = CreateMenu(new MenuBuilder("DVMultiplayer Connect", "Connect to server", 975, 540f, false, false));

            TextFieldBuilder inputIpField = new TextFieldBuilder("IP", connectMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            TextFieldBuilder inputPortField = new TextFieldBuilder("Port", connectMenu.transform, new Rect(-32f, -315, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f), true);
            TextFieldBuilder inputUsernameField = new TextFieldBuilder("Username", connectMenu.transform, new Rect(-32f, -415, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Connect", "Connect", connectMenu.transform, new Rect(0f, -515, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);
            ButtonBuilder favoriteButtonBuilder = new ButtonBuilder("Save as Favorite", "UI_Unfavorited.png", connectMenu.transform, new Rect(280f, -515, 76, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f));

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
            GameObject favorited = CreateButton(favoriteButtonBuilder);

            GameObject menu = Object.Instantiate(connectMenu, canvas.transform);
            Object.DestroyImmediate(connectMenu);
            ConnectMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateFavoriteListUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject favoriteConnectMenu = CreateMenu(new MenuBuilder("DVMultiplayer Favorite Connector", "Connect to Favorite", 700, 640, false, false));

            for(int i = 0; i < 4; i++)
            {
                CreateSection(new Rect(0f, -177 - (100 * i), 650, 91.14999f), RectTransformAnchoring.TopCenter, favoriteConnectMenu.transform);
                CreateButton(new ButtonBuilder($"Fav{i + 1}", "", favoriteConnectMenu.transform, new Rect(32f, -177.5f - (100 * i), 559, 76), RectTransformAnchoring.TopLeft, new Vector2(0f, .5f), TextAlignmentOptions.MidlineLeft));
                CreateButton(new ButtonBuilder($"Del Fav{i + 1}", "UI_Bin.png", favoriteConnectMenu.transform, new Rect(-32f, -177.5f - (100 * i), 76, 76), RectTransformAnchoring.TopRight, new Vector2(1f, .5f)));
            }
            CreateSection(new Rect(0f, -177 - (100 * 4), 650, 91.14999f), RectTransformAnchoring.TopCenter, favoriteConnectMenu.transform);
            CreateButton(new ButtonBuilder($"NextPage", ">", favoriteConnectMenu.transform, new Rect(-30, -177.5f - (100 * 4), 76, 76), RectTransformAnchoring.TopRight, new Vector2(1f, .5f), TextAlignmentOptions.Center));
            CreateButton(new ButtonBuilder($"PrevPage", "<", favoriteConnectMenu.transform, new Rect(30, -177.5f - (100 * 4), 76, 76), RectTransformAnchoring.TopLeft, new Vector2(0f, .5f), TextAlignmentOptions.Center));

            GameObject menu = Object.Instantiate(favoriteConnectMenu, canvas.transform);
            Object.DestroyImmediate(favoriteConnectMenu);
            FavoriteConnectMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateFavoriteUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject favoriteMenu = CreateMenu(new MenuBuilder("DVMultiplayer Favorite", "Favorite Server", 975, 345f, false, false));

            TextFieldBuilder inputNameField = new TextFieldBuilder("Name", favoriteMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder acceptButtonBuilder = new ButtonBuilder("Accept", "Save as Favorite", favoriteMenu.transform, new Rect(0, -320, 975 / 3, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            GameObject ipSection = CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, favoriteMenu.transform);
            GameObject inputFieldNameLabel = CreateLabel("Name", "Name:", favoriteMenu.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldErrorLabel = CreateLabel("Error", "", favoriteMenu.transform, new Rect(0, -240f, 920, 16), FontStyles.Normal, TextAlignmentOptions.MidlineRight, RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), Color.red, 16);
            GameObject inputFieldIP = CreateTextField(inputNameField);

            GameObject favorited = CreateButton(acceptButtonBuilder);

            GameObject menu = Object.Instantiate(favoriteMenu, canvas.transform);
            Object.DestroyImmediate(favoriteMenu);
            SaveFavoriteMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateRequestUsernameUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject usernameMenu = CreateMenu(new MenuBuilder("DVMultiplayer Username Request", "Enter username please", 975, 345f, false, false));

            TextFieldBuilder inputNameField = new TextFieldBuilder("Username", usernameMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder acceptButtonBuilder = new ButtonBuilder("Accept", "Connect", usernameMenu.transform, new Rect(0, -320, 975 / 3, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            GameObject ipSection = CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, usernameMenu.transform);
            GameObject inputFieldNameLabel = CreateLabel("Username", "Username:", usernameMenu.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldIP = CreateTextField(inputNameField);

            GameObject favorited = CreateButton(acceptButtonBuilder);

            GameObject menu = Object.Instantiate(usernameMenu, canvas.transform);
            Object.DestroyImmediate(usernameMenu);
            UsernameRequestMenuUI = menu.GetComponent<MenuScreen>();
        }

        private static void GenerateHostUI()
        {
            GameObject canvas = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO;
            GameObject hostMenu = CreateMenu(new MenuBuilder("DVMultiplayer Host", "Host", 975, 440f, false, false));

            TextFieldBuilder inputPortField = new TextFieldBuilder("Port", hostMenu.transform, new Rect(-32f, -215, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f), true);
            TextFieldBuilder inputUsernameField = new TextFieldBuilder("Username", hostMenu.transform, new Rect(-32f, -315, 695, 76), TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopRight, new Vector2(1f, 0f));
            ButtonBuilder connectButtonBuilder = new ButtonBuilder("Host", "Host", hostMenu.transform, new Rect(0f, -415, 448, 76), RectTransformAnchoring.TopCenter, new Vector2(.5f, 0f), TextAlignmentOptions.Center);

            GameObject portSection = CreateSection(new Rect(0f, -177, 925, 91.14999f), RectTransformAnchoring.TopCenter, hostMenu.transform);
            GameObject inputFieldPortLabel = CreateLabel("Port", "Port:", hostMenu.transform, new Rect(32, -215, 218, 76), FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft, RectTransformAnchoring.TopLeft, new Vector2(0f, 0f));
            GameObject inputFieldPort = CreateTextField(inputPortField);

            GameObject usernameSection = CreateSection(new Rect(0f, -277, 925, 91.14999f), RectTransformAnchoring.TopCenter, hostMenu.transform);
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

            ButtonBuilder builderBtn = new ButtonBuilder($"Backspace", $"Backspace", inputMenu.transform, new Rect(925 - (80 * 9f) + 20, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject backspaceBtn = CreateButton(builderBtn);
            InputButton inputbtn = backspaceBtn.AddComponent<InputButton>();
            inputbtn.isBackspace = true;

            builderBtn = new ButtonBuilder($"Casing", $"Uppercase", inputMenu.transform, new Rect(925 - (80 * 9f) + 205, -620, 180, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
            GameObject caseBtn = CreateButton(builderBtn);

            builderBtn = new ButtonBuilder($"Confirm", $"Confirm", inputMenu.transform, new Rect(925 - (80 * 9f) + 390, -620, 150, 75), RectTransformAnchoring.TopLeft, new Vector2(0f, 0f), TextAlignmentOptions.Center);
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
                newButton.transform.Find("image").GetComponent<Image>().SetSprite(buttonBuilder.icon);
            }
            else
            {
                TextMeshProUGUI text = newButton.transform.Find("label").GetComponent<TextMeshProUGUI>();
                text.text = buttonBuilder.label;
                text.alignment = buttonBuilder.textAlignment;
            }

            UIElementTooltip tooltip = newButton.GetComponent<UIElementTooltip>();
            tooltip.tooltipEnabledText = buttonBuilder.tooltipEnabledText;
            tooltip.tooltipDisabledText = buttonBuilder.tooltipDisabledText;

            newButton.AddComponent<ButtonFeatures>();

            return newButton;
        }

        private static GameObject CreateLabel(string name, string labelText, Transform parent, Rect rect, FontStyles fontStyle, TextAlignmentOptions textAlignment, RectTransformAnchoring anchor, Vector2 pivot, Color? color = null, float? fontSize = null)
        {
            GameObject refLabel = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("title").gameObject;
            GameObject label = Object.Instantiate(refLabel, parent);
            label.name = $"Label {name}";

            TextMeshProUGUI titleText = label.GetComponent<TextMeshProUGUI>();
            titleText.text = labelText;
            titleText.alignment = textAlignment;
            titleText.fontStyle = fontStyle;
            if (color.HasValue)
                titleText.color = color.Value;

            if (fontSize.HasValue)
                titleText.fontSize = fontSize.Value;

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

            newTextField.name = $"TextField {textFieldBuilder.name}";
            TextMeshProUGUI text = newTextField.transform.Find("label").GetComponent<TextMeshProUGUI>();
            text.alignment = textFieldBuilder.textAlignment;
            text.fontStyle = FontStyles.Normal;
            text.text = "";

            TextField field = newTextField.AddComponent<TextField>();
            field.title = textFieldBuilder.name;
            field.isDigitOnly = textFieldBuilder.isDigitOnly;
            
            return newTextField;
        }

        private static GameObject CreateSection(Rect rect, RectTransformAnchoring anchor, Transform parent, Vector2? pivot = null)
        {
            GameObject refSection = SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.transform.Find("Main Menu").Find("Section").gameObject;
            GameObject newSection = Object.Instantiate(refSection, parent);
            RectTransform transform = newSection.GetComponent<RectTransform>();

            transform.ChangeAnchors(anchor);
            if (pivot.HasValue)
                transform.pivot = pivot.Value;
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
            public readonly string tooltipEnabledText;
            public readonly string tooltipDisabledText;

            public ButtonBuilder(string name, string icon, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, string tooltipEnabledText = "", string tooltipDisabledText = "")
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
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
            }

            public ButtonBuilder(string name, string label, Transform parent, Rect pos, RectTransformAnchoring anchor, Vector2 pivot, TextAlignmentOptions textAlignment, string tooltipEnabledText = "", string tooltipDisabledText = "")
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
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
            }

            public ButtonBuilder(string name, string icon, Transform parent, GameObject btn = null, string tooltipEnabledText = "", string tooltipDisabledText = "")
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
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
            }

            public ButtonBuilder(string name, string label, Transform parent, TextAlignmentOptions textAlignment, GameObject btn = null, string tooltipEnabledText = "", string tooltipDisabledText = "")
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
                this.tooltipEnabledText = tooltipEnabledText;
                this.tooltipDisabledText = tooltipDisabledText;
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