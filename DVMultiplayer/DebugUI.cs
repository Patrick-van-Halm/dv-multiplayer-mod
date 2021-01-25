using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer
{
    static class DebugUI
    {
        private static bool showGUI = false;

        internal static void Update()
        {
            if (Input.GetKeyUp(KeyCode.End))
                showGUI = !showGUI;
        }

        internal static void OnGUI()
        {
            if (!showGUI)
                return;

            GUI.Box(new Rect(Screen.width - 250, Screen.height / 2 - 150, 250, 300), "DVMultiplayer DEBUGGER");
            int ypos = Screen.height / 2 - 150 + 20;

            // Tutorial Status
            if (TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress)
            {
                GUI.Label(new Rect(Screen.width - 190, ypos, 180, 20), "Tutorial Active", UUI.GenerateStyle(allignment: TextAnchor.MiddleCenter));
                ypos += 20;
            }

            // Train Car Status
            GUI.Label(new Rect(Screen.width - 245, ypos, 117, 20), $"TrainCar:");
            if (PlayerManager.Car)
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"{PlayerManager.Car.ID}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
            else
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"None", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
            ypos += 20;

            // Connection Status
            GUI.Label(new Rect(Screen.width - 245, ypos, 117, 20), $"Connection State:");
            if(!NetworkManager.IsClient() && !NetworkManager.IsHost())
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"Disconnected", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight, textColor: Color.red));
            else if (!NetworkManager.IsClient() && NetworkManager.IsHost())
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"Server UP", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight, textColor: Color.yellow));
            else if (NetworkManager.IsClient() && NetworkManager.IsHost())
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"Server UP + Connected", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight, textColor: Color.green));
            else if (NetworkManager.IsClient() && !NetworkManager.IsHost())
                GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"Connected to host", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight, textColor: Color.green));
            ypos += 20;

            // Connected Players Train
            if(NetworkManager.IsClient() && SingletonBehaviour<NetworkPlayerManager>.Instance && SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerCount() > 0)
            {
                foreach(NetworkPlayerSync playerSync in SingletonBehaviour<NetworkPlayerManager>.Instance.GetAllNonLocalPlayerSync()) 
                {
                    GUI.Label(new Rect(Screen.width - 245, ypos, 117, 20), $"Player [{playerSync.Id}] train:");
                    if(playerSync.Train)
                        GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"{playerSync.Train.ID}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                    else
                        GUI.Label(new Rect(Screen.width - 123, ypos - 1, 117, 20), $"None", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                    ypos += 15;

                    if (playerSync.Train && playerSync.Train.IsLoco)
                    {
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"InteriorLoaded:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{playerSync.Train.IsInteriorLoaded}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                        LocoControllerBase controller = playerSync.Train.GetComponent<LocoControllerBase>();
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"LocoControllerBase InputActive:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{controller.inputActive}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"Train ThrottleValue:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{controller.throttle.ToString("G3")}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"Train Velocity:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{controller.train.rb.velocity.ToString("G3")}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"Train ForwardSpeed:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{controller.GetForwardSpeed()}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                        GUI.Label(new Rect(Screen.width - 245, ypos, 187, 20), $"Train engineRPM:");
                        GUI.Label(new Rect(Screen.width - 53, ypos - 1, 47, 20), $"{controller.GetComponent<ShunterLocoSimulation>().engineRPM.value}", UUI.GenerateStyle(allignment: TextAnchor.MiddleRight));
                        ypos += 15;
                    }
                }
            }
        }
    }
}
