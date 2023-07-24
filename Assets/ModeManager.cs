using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ModeManager : MonoBehaviour
{
    public InputActionReference toggleModes = null;
    public TextMeshProUGUI modeIndicator = null;
    public List<ControlMode> modes;

    public string TEMPstringForUI;//put into modes object

    [SerializeField]
    private int currMode;

    public bool mSwitchingInInspector;
    public bool flushModesOnStart; //for testing
    public int manualMode;

    // Start is called before the first frame update
    void Start()
    {
        currMode = 0;

        if (flushModesOnStart)//for testing
        {

            for (int i = 0; i < modes.Count; i++)
            {
                nextMode();
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

        if (mSwitchingInInspector)
        {
            if (manualMode != currMode)
                switchToMode(manualMode);
        }
        else
        {
            if (toggleModes.action.WasReleasedThisFrame())
                nextMode();
        }
        
    }


    private void switchToMode(int newMode)
    {
        //disable previous mode
        ControlMode mode = modes[currMode];
        mode.disableMode();

        currMode = newMode;

        //enable current mode
        mode = modes[currMode];
        mode.enableMode();

        //update UI
        modeIndicator.text = currMode.ToString() + "\n" + mode.name + "\n" + mode.description + "\n" + mode.listOfControlMappings;

    }

    private void nextMode()
    {
        //iterate mode number
        int newMode = currMode + 1;
        newMode %= modes.Count;

        switchToMode(newMode);
    }
}
