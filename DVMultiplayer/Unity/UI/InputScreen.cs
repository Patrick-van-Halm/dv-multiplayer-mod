using DVMultiplayer;
using DVMultiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class InputScreen : MonoBehaviour
{
    private TextMeshProUGUI label;

    private string input;
    public bool isUppercase;
    public bool isDigitOnly = false;
    Button casingButton;
    private bool listenToKeyboard = false;

    public string Input {
        get
        {
            return input;
        }
        internal set
        {
            input = value;
            UpdateLabel();
        }
    }

    private void Awake()
    {
        label = transform.Find("Label Input").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI casingBtnText = transform.Find("Button Casing").Find("label").GetComponent<TextMeshProUGUI>();
        casingButton = transform.Find("Button Casing").GetComponent<Button>();
        casingButton.onClick.AddListener(() =>
        {
            isUppercase = !isUppercase;
            casingBtnText.text = isUppercase ? "Lowercase" : "Uppercase";
        });
        ResetInput();
    }

    internal void OnOpen()
    {
        listenToKeyboard = true;
        casingButton.interactable = !isDigitOnly;
    }

    internal void OnClose()
    {
        listenToKeyboard = false;
    }

    private void Update()
    {
        if(listenToKeyboard)
            foreach (char c in UnityEngine.Input.inputString)
            {
                if (c == '\b') // has backspace/delete been pressed?
                {
                    Backspace();
                }
                else if((char.IsLetterOrDigit(c) || c == '.' || c == '-') && !isDigitOnly || char.IsDigit(c) && isDigitOnly)
                {
                    Input += c;
                }
            }
    }
     
    public void ResetInput()
    {
        Input = "";
    }

    public void SendKeyPress(char key)
    {
        Input += (isUppercase ? $"{key}".ToUpper() : $"{key}");
    }

    private void UpdateLabel()
    {
        label.text = Input;
    }

    public void Backspace()
    {
        if (Input.Length <= 0)
            return;
        
        Input = Input.Substring(0, Input.Length - 1);
    }

    public void SetTitle(string inputWindowTitle)
    {
        transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Entering {inputWindowTitle}";
    }
}