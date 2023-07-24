using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//AUTHOR: Ezra Odole/Summer 2023 Undergraduates
public class ControlMode : MonoBehaviour
{
    //this script simply contains information about what to enable/disable for the mode

    public string modeName;
    public string description;
    public string listOfControlMappings;

    public InputActionAsset controls;
    public List<GameObject> objectsOfModeToEnable;
    public List<GameObject> objectsOfModeToDISABLE;//?temporary solution to not having all modes specified, may end up being permanently useful (using it to disable IK rig when entering mode)
    public List<Behaviour> scriptsOfModeToEnable;

    //when entering this mode, these are the things we want enabled/disabled
    public void enableMode()
    {
        foreach (GameObject g in objectsOfModeToEnable)
            g.SetActive(true);

        foreach (Behaviour s in scriptsOfModeToEnable)
            s.enabled = true;

        foreach (GameObject g in objectsOfModeToDISABLE)
            g.SetActive(false);
 
    }

    //when exiting this mode, we return these things to their previous state?
    public void disableMode()
    {
        foreach (GameObject g in objectsOfModeToEnable)
            g.SetActive(false);

        foreach (Behaviour s in scriptsOfModeToEnable)
            s.enabled = false;
    }
    override
    public string ToString() { return modeName;  }

}