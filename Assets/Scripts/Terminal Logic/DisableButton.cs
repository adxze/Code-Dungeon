using System;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.UI;

public class DisableButton : MonoBehaviour
{
    public Button[] buttons;
    public CodeGameController code;
    private bool isDisabled;
    private Color C;

    private void Awake()
    {
    }

    public void Update()
    {
        if (code.IsRunning && !isDisabled)
        {
            ButtonOff();
            isDisabled = true; 
        }
        else if (!code.IsRunning && isDisabled)
        {
           ButtonON();
           isDisabled = false;
        }
    }

    void ButtonOff()
    {
        foreach (Button button in buttons)
        {
            button.interactable = false;
            C = button.image.color;
            C.a = 0.4f; 
            button.image.color = C; 
        }
    }

    void ButtonON()
    {
        foreach (Button button in buttons)
        {
            button.interactable = true;
            C = button.image.color;
            C.a = 1f;
            button.image.color = C;
        }
    }
    
    
}
