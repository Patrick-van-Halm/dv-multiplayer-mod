using DVMultiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class TextField : MonoBehaviour
{
    public string title;
    private void Awake()
    {
        TextMeshProUGUI text = transform.Find("label").GetComponent<TextMeshProUGUI>();
        InputScreen input = CustomUI.InputScreenUI.GetComponent<InputScreen>();
        Button btnConfirm = CustomUI.InputScreenUI.transform.Find("Button Confirm").GetComponent<Button>();
        Button btnClose = CustomUI.InputScreenUI.transform.Find("Button Close").GetComponent<Button>();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            MenuScreen prevScreen = CustomUI.currentScreen;
            input.SetTitle(title);
            input.Input = text.text;
            CustomUI.Open(CustomUI.InputScreenUI);
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(() =>
            {
                text.text = input.Input;
                CustomUI.Open(prevScreen);
            });

            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {
                CustomUI.Open(prevScreen);
            });
        });
    }
}
