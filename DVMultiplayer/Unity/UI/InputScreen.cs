using DVMultiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

class InputScreen : MonoBehaviour
{
    private TextMeshProUGUI label;

    public string Input { get; internal set; }

    private void Awake()
    {
        label = transform.Find("Label Input").GetComponent<TextMeshProUGUI>();
        ResetInput();
    }
     
    public void ResetInput()
    {
        Input = "";
        UpdateLabel();
    }

    public void SendKeyPress(char key)
    {
        Input += key;
        UpdateLabel();
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
        UpdateLabel();
    }

    public void SetTitle(string inputWindowTitle)
    {
        transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"Entering {inputWindowTitle}";
    }
}