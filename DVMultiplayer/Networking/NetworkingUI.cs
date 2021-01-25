using DVMultiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.Networking
{
    public class NetworkingUI
    {
        private bool showUI = false;
        private string host = "";
        private string portString = "4296";

        public void ListenToInputs()
        {
            if (Input.GetKeyUp(KeyCode.Home))
            {
                ToggleUI();
                UUI.UnlockMouse(showUI);
            }
        }

        public void ToggleUI()
        {
            showUI = !showUI;
        }

        public void Draw()
        {
            if (showUI)
            {
                int yStart = 20;
                if (!NetworkManager.IsClient() && !NetworkManager.IsHost())
                {
                    GUIStyle textStyle = UUI.GenerateStyle(Color.white, 12, TextAnchor.MiddleLeft);
                    GUI.Box(new Rect(20, 20, 300, 140), "Connect to server");
                    GUI.Label(new Rect(80 - textStyle.CalcSize(new GUIContent("IP:")).x, 50, 30, 20), "IP:");
                    host = GUI.TextField(new Rect(90, 50, 300 - 120, 20), host);
                    GUI.Label(new Rect(80 - textStyle.CalcSize(new GUIContent("Port:")).x, 85, 30, 20), "Port:");
                    portString = GUI.TextField(new Rect(90, 85, 300 - 120, 20), portString);
                    bool connect = GUI.Button(new Rect(80, 120, 300 - 120, 20), "Connect");

                    int port = 0;
                    bool portValid = int.TryParse(portString, out port) && port < 65535 && port > 0;
                    if (connect && portValid)
                        NetworkManager.Connect(host, port);
                    yStart += 200;
                }

                if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                {
                    GUI.Box(new Rect(20, yStart, 300, 80), "Client");
                    bool connect = GUI.Button(new Rect(80, yStart + 30, 300 - 120, 20), "Disconnect");

                    if (connect)
                        NetworkManager.Disconnect();
                    yStart += 200;
                }
                
                if(!NetworkManager.IsHost() && !NetworkManager.IsClient() || NetworkManager.IsHost() && NetworkManager.IsClient())
                {
                    GUI.Box(new Rect(20, yStart, 300, 80), "Hosting");
                    bool button = GUI.Button(new Rect(80, yStart + 30, 300 - 120, 20), (!NetworkManager.IsHost() ? "Start server" : "Stop server"));

                    if (button)
                        if(!NetworkManager.IsHost())
                            NetworkManager.StartServer();
                        else
                            NetworkManager.StopServer();
                }
            }
        }

        internal void HideUI()
        {
            showUI = false;
        }
    }
}
