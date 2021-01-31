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
        private bool UIShown = false;
        private MenuScreen UI;
        private MenuScreen ConnectUI;
        private MenuScreen InputUI;
        private MenuScreen HostUI;

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

        internal void Draw()
        {
            if(UI == null)
            {
                UI = CustomUI.NetworkUI;
                ConnectUI = CustomUI.ConnectMenuUI;
                InputUI = CustomUI.InputScreenUI;
                HostUI = CustomUI.HostMenuUI;

                UI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(ConnectUI);
                });

                ConnectUI.transform.Find("Button Connect").GetComponent<Button>().onClick.AddListener(() =>
                {
                    string host = ConnectUI.transform.Find("TextField IP").GetComponentInChildren<TextMeshProUGUI>().text;
                    string portString = ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                    string username =  ConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;
                    if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portString) && int.TryParse(portString, out int port) && !string.IsNullOrWhiteSpace(username))
                    {
                        NetworkManager.Connect(host, port, username);
                        HideUI();
                    }
                });

                UI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(HostUI);
                });

                HostUI.transform.Find("Button Host").GetComponent<Button>().onClick.AddListener(() =>
                {
                    string portString = ConnectUI.transform.Find("TextField Port").GetComponentInChildren<TextMeshProUGUI>().text;
                    string username = ConnectUI.transform.Find("TextField Username").GetComponentInChildren<TextMeshProUGUI>().text;

                    bool portValid = ushort.TryParse(portString, out ushort port) && port < 65535 && port > 0;
                    if (!portValid)
                        port = 4296;

                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        NetworkManager.StartServer(username, port);
                        HideUI();
                    }
                });

                UI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    HideUI();
                });

                ConnectUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(UI);
                });

                InputUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(ConnectUI);
                });

                HostUI.transform.Find("Button Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    CustomUI.Open(UI);
                });
            }

            if (showUI && !UIShown)
            {
                // Disable the buttons if the tutorial is not yet finished.
                UI.transform.Find("Button Connect").GetComponent<Button>().interactable = !(TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress);
                UI.transform.Find("Button Connect").GetComponent<UIElementTooltip>().TooltipNonInteractableText = TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress ? "Finish the tutorial first" : "";
                UI.transform.Find("Button Host").GetComponent<Button>().interactable = !(TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress);
                UI.transform.Find("Button Host").GetComponent<UIElementTooltip>().TooltipNonInteractableText = TutorialController.tutorialPartOneInProgress || TutorialController.tutorialPartTwoInProgress ? "Finish the tutorial first" : "";

                UIShown = true;
                CustomUI.Open(UI);
            }
            else if (!showUI && UIShown)
            {
                UIShown = false;
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
