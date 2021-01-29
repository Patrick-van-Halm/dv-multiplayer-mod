using DVMultiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class InputButton : MonoBehaviour
{
    public char key;
    public bool isBackspace;
    Button button;
    InputScreen input;

    private void Awake()
    {
        button = GetComponent<Button>();
        input = transform.parent.GetComponent<InputScreen>();

        button.onClick.AddListener(() =>
        {
            if (!isBackspace)
                input.SendKeyPress(key);
            else
                input.Backspace();
        });
    }
}
