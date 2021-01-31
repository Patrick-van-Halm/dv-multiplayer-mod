using DVMultiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRTK;
using Object = UnityEngine.Object;

namespace DVMultiplayer.Networking
{
    public class NetworkingUI
    {
        private bool showUI = false;
        private string host = "";
        private string conPortString = "4296";
        private string hostPortString = "4296";
        private string username = "";
        private bool VRShown = false;
        private MenuScreen VRUI;
        private MenuScreen VRConnectUI;
        private MenuScreen VRInputUI;
        private MenuScreen VRHostUI;

        public void ListenToInputs()
        {
            if ((!VRManager.IsVREnabled() && Input.GetKeyUp(KeyCode.Home)) || (VRManager.IsVREnabled() && Input.GetKeyUp(KeyCode.F7)))
            {
                ToggleUI();
            }
        }

        public void ToggleUI()
        {
            showUI = !showUI;
            if(!VRManager.IsVREnabled())
                UUI.UnlockMouse(showUI);
        }

        public void Draw()
        {
            if (showUI)
            {
                GUIStyle textStyle = UUI.GenerateStyle(Color.white, 12, TextAnchor.MiddleLeft);
                int yStart = 20;
                if (!NetworkManager.IsClient() && !NetworkManager.IsHost())
                {
                    GUI.Box(new Rect(20, 20, 300, 70), "Multiplayer settings");
                    GUI.Label(new Rect(110 - textStyle.CalcSize(new GUIContent("Username:")).x, 45, 80, 20), "Username:");
                    username = GUI.TextField(new Rect(120, 45, 300 - 130, 20), username);

                    yStart += 80;

                    GUI.Box(new Rect(20, yStart, 300, 110), "Connect to server");
                    GUI.Label(new Rect(110 - textStyle.CalcSize(new GUIContent("IP:")).x, yStart + 25, 80, 20), "IP:");
                    host = GUI.TextField(new Rect(120, yStart + 25, 300 - 130, 20), host);
                    GUI.Label(new Rect(110 - textStyle.CalcSize(new GUIContent("Port:")).x, yStart + 50, 80, 20), "Port:");
                    conPortString = GUI.TextField(new Rect(120, yStart + 50, 300 - 130, 20), conPortString);
                    bool connect = GUI.Button(new Rect(80, yStart + 80, 300 - 120, 20), "Connect");

                    ushort port = 0;
                    bool portValid = ushort.TryParse(conPortString, out port) && port < 65535 && port > 0;
                    if (connect && portValid && !string.IsNullOrWhiteSpace(username))
                        NetworkManager.Connect(host, port, username);
                    yStart += 120;
                }

                if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                {
                    GUI.Box(new Rect(20, yStart, 300, 80), "Client");
                    bool connect = GUI.Button(new Rect(80, yStart + 30, 300 - 120, 20), "Disconnect");

                    if (connect)
                        NetworkManager.Disconnect();
                    yStart += 90;
                }
                
                if(!NetworkManager.IsHost() && !NetworkManager.IsClient())
                {
                    GUI.Box(new Rect(20, yStart, 300, 80), "Hosting");
                    GUI.Label(new Rect(110 - textStyle.CalcSize(new GUIContent("Port:")).x, yStart + 25, 80, 20), "Port:");
                    hostPortString = GUI.TextField(new Rect(120, yStart + 25, 300 - 130, 20), hostPortString);
                    bool button = GUI.Button(new Rect(80, yStart + 50, 300 - 120, 20), "Start server");

                    bool portValid = ushort.TryParse(hostPortString, out ushort port) && port < 65535 && port > 0;
                    if (!portValid)
                        port = 4296;

                    if (button && !string.IsNullOrWhiteSpace(username))
                        NetworkManager.StartServer(username, port);

                    yStart += 90;
                }

                if(NetworkManager.IsHost() && NetworkManager.IsClient())
                {
                    GUI.Box(new Rect(20, yStart, 300, 80), "Hosting");
                    GUIStyle center = UUI.GenerateStyle(Color.white, 12, TextAnchor.MiddleCenter);
                    GUI.Label(new Rect(30, yStart + 25, 290, 20), $"Connected as {username}", center);
                    bool button = GUI.Button(new Rect(80, yStart + 50, 300 - 120, 20), "Stop server");

                    if (button)
                        NetworkManager.StopServer();

                }
            }
        }

        internal void VRDraw()
        {
            if(VRUI == null)
            {
                VRUI = CustomUI.NetworkUI;
                VRConnectUI = CustomUI.ConnectMenuUI;
                VRInputUI = CustomUI.InputScreenUI;
                VRHostUI = CustomUI.HostMenuUI;

                VRUI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(VRConnectUI);
                });

                VRConnectUI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
                {
                    string host = VRConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text;
                    string portString = VRConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                    string username =  VRConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;
                    if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portString) && int.TryParse(portString, out int port) && !string.IsNullOrWhiteSpace(username))
                    {
                        NetworkManager.Connect(host, port, username);
                        HideUI();
                    }
                });

                VRUI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(VRHostUI);
                });

                VRHostUI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
                {
                    string portString = VRConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                    string username = VRConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;

                    bool portValid = ushort.TryParse(portString, out ushort port) && port < 65535 && port > 0;
                    if (!portValid)
                        port = 4296;

                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        NetworkManager.StartServer(username, port);
                        HideUI();
                    }
                });

                VRUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    HideUI();
                });

                VRConnectUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(VRUI);
                });

                VRInputUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(VRConnectUI);
                });
            }

            if (showUI && !VRShown)
            {
                VRShown = true;
                CustomUI.Open(VRUI);
            }
            else if (!showUI && VRShown)
            {
                VRShown = false;
                CustomUI.Close();
            }
        }

        internal void HideUI()
        {
            showUI = false;
            if(!VRManager.IsVREnabled())
                UUI.UnlockMouse(showUI);
        }
    }
}
