using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Diagnostics;


public class ModeManager : MonoBehaviour
{
    [SerializeField]
    private int currMode; // currMode is set to value from 0-modes.Count      
    public List<ControlMode> modes; 
    public List<TextMeshProUGUI> UICanvases;
    public TextMeshProUGUI hintText;
    public string hintTextString;

    // Time tracking
    private Stopwatch[] stopwatches;

    // Disable modes to at the beginning, user has to choose to enter into a mode
    // TODO: Default mode: LOOK THIS UP
    void Start()
    {
        stopwatches = new Stopwatch[modes.Count];
        for (int i = 0; i < modes.Count; i++)
        {
            stopwatches[i] = new Stopwatch();
        }
        currMode = modes.Count - 1; // Needs to be -1 at first so when nextMode() is called, it goes to 0

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
        if (hintText != null)
        {
            hintText.text = "Mode: " + (currMode + 1).ToString() + " - " + mode.modeName + "\n" + hintTextString;
        }
    }

    public void nextMode()
    {
        //iterate mode number
        int newMode;

        // Time tracking, end stopwatch for old mode
        if (currMode >= 0 && currMode < modes.Count)
        {
            stopwatches[currMode].Stop();
        }

        newMode = currMode + 1;
        newMode %= modes.Count;

        // Begin stopwatch for next mode
        stopwatches[newMode].Start();

        switchToMode(newMode);
    }

    public void UpdateUI(string text)
    {
        for(int i = 0; i < UICanvases.Count; i++)
        {
            UICanvases[i].text = text;
        }
    }

    private void OnApplicationQuit()
    {
        for (int i = 0; i < modes.Count; i++)
        {
            UnityEngine.Debug.Log(modes[i].modeName + " mode time elapsed: " + System.Math.Round(stopwatches[i].Elapsed.TotalSeconds, 2) + " seconds");
        }
    }
}
