﻿using DVMultiplayer;
using DVMultiplayer.Utils;
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
    public bool isPaste;
    Button button;
    TextMeshProUGUI label;
    InputScreen input;
    Image buttonImage;
    Sprite enabledSprite;
    Sprite disabledSprite;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TextMeshProUGUI>();
        input = transform.parent.GetComponent<InputScreen>();
        buttonImage = GetComponent<Image>();
        enabledSprite = buttonImage.sprite;

        Texture2D disabledTexture = UUI.LoadTextureFromFile("UI_Button_disabled.png");
        disabledSprite = Sprite.Create(disabledTexture, new Rect(0, 0, disabledTexture.width, disabledTexture.height), new Vector2(0.5f, 0.5f), 100f);

        button.onClick.AddListener(() =>
        {
            if (isBackspace)
                input.Backspace();
            else if (isPaste)
                input.Paste();
            else
                input.SendKeyPress(key);
        });
    }

    private void Update()
    {
        CheckDigitOnly();
        CheckCasing();
    }

    private void CheckCasing()
    {
        if (input.isUppercase && label.fontStyle == FontStyles.UpperCase || !input.isUppercase && label.fontStyle == FontStyles.LowerCase || isBackspace || isPaste || !char.IsLetter(key) || input.isDigitOnly)
            return;

        label.fontStyle = input.isUppercase ? FontStyles.UpperCase : FontStyles.LowerCase;
    }

    private void CheckDigitOnly()
    {
        if ((input.isDigitOnly && char.IsDigit(key)) || isBackspace || isPaste)
            button.interactable = true;
        else if (!input.isDigitOnly)
            button.interactable = true;
        else
            button.interactable = false;
    }
}
