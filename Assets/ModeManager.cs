using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ModeManager : MonoBehaviour
{
    [SerializeField]
    private int currMode;
    public List<ControlMode> modes;
    public List<TextMeshProUGUI> UICanvases;
    public TextMeshProUGUI hintText;
    public string hintTextString;

    void Start()
    {
        currMode = modes.Count - 1;
        
        for (int i = 0; i < modes.Count; i++)
        {
            modes[i].disableMode();
        }

    }

    void Update()
    {
        if (Input.GetKeyDown("t"))
            nextMode();
    }


    public void switchToMode(int newMode)
    {
        //disable previous mode
        ControlMode mode = modes[currMode];
        mode.disableMode();

        currMode = newMode;

        //enable current mode
        mode = modes[currMode];
        mode.enableMode();

        //update UI
        UpdateUI("Mode: " + currMode.ToString() + " - " + mode.ToString());
        hintText.text = "Mode: " + currMode.ToString() + " - " + mode.modeName + "\n" + hintTextString;
    }

    public void nextMode()
    {
        //iterate mode number
        int newMode = currMode + 1;
        newMode %= modes.Count;

        switchToMode(newMode);
    }

    public void UpdateUI(string text)
    {
        for(int i = 0; i < UICanvases.Count; i++)
        {
            UICanvases[i].text = text;
        }
    }
}
