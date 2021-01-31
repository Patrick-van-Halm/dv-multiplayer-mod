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
    TextMeshProUGUI label;
    InputScreen input;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TextMeshProUGUI>();
        input = transform.parent.GetComponent<InputScreen>();

        button.onClick.AddListener(() =>
        {
            if (!isBackspace)
                input.SendKeyPress(key);
            else
                input.Backspace();
        });
    }

    private void Update()
    {
        if (input.isDigitOnly && char.IsLetter(key) && !button.interactable || !input.isDigitOnly && char.IsLetter(key) && button.interactable)
            return;

        button.interactable = input.isDigitOnly && !char.IsLetter(key);

        if (input.isUppercase && label.fontStyle == FontStyles.UpperCase || !input.isUppercase && label.fontStyle == FontStyles.LowerCase || isBackspace || !char.IsLetter(key))
            return;
        
        label.fontStyle = input.isUppercase ? FontStyles.UpperCase : FontStyles.LowerCase;
    }
}
