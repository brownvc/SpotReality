using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRGeneralControls : MonoBehaviour
{

    public Canvas UI;
    public Canvas hintUI;
    private bool UIShowing;

    public InputActionReference LAxB;
    public InputActionReference LAx;
    public InputActionReference LX;
    public InputActionReference LY;
    public InputActionReference LT1;
    public InputActionReference LT2;
    public InputActionReference RAxB;
    public InputActionReference RAx;
    public InputActionReference RA;
    public InputActionReference RB;
    public InputActionReference RT1;
    public InputActionReference RT2;

    public RosSharp.RosBridgeClient.KillMotor killSpot;
    public RosSharp.RosBridgeClient.StowArm stow;
    public RosSharp.RosBridgeClient.SetGripper gripper;
    private bool gripperOpen;
    public ModeManager manager;


    void Start()
    {
        gripperOpen = false;
        gripper.closeGripper();
        hintUI.enabled = true;
        UI.enabled = false;
        UIShowing = false;
    }

    void Update()
    {

        if(LX.action.WasPressedThisFrame())
            killSpot.killSpot();

        if (LT2.action.WasPressedThisFrame())
            stow.Stow();

        if (RA.action.WasPressedThisFrame())
            manager.nextMode();

        if (RT2.action.WasPressedThisFrame())
        {
            if (gripperOpen)
            {
                gripper.closeGripper();
                gripperOpen = false;
            }
            else
            {
                gripper.openGripper();
                gripperOpen = true;
            }
        }

        if (LY.action.WasPressedThisFrame())
        {
            toggleUI();
        }

    }

    public void toggleUI()
    {
        UI.enabled = !(UI.enabled);
        hintUI.enabled = !(hintUI.enabled);
        UIShowing = !UIShowing;
    }
}
