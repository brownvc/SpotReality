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
    private float gripperPercentage;
    public ModeManager manager;


    void Start()
    {
        gripperOpen = false;
        gripper.closeGripper();
        hintUI.enabled = true;
        UI.enabled = false;
        UIShowing = false;
        gripperPercentage = 0f;
    }

    void Update()
    {

        /* Kill spot if X and left gripper (LT1) are pressed */
        if (LX.action.WasPressedThisFrame() && LT1.action.IsPressed())
            killSpot.killSpot();
        /* Partially close gripper if gripper is open and X only is pressed */
        else if (LX.action.IsPressed())
        {
            if (gripperPercentage > 0)
            {
                gripperPercentage -= 0.5f;
                Debug.Log(gripperPercentage);
                gripper.setGripperPercentage(gripperPercentage);
                gripperOpen = false;
            }
        }

        /* Stow arm if left trigger (LT2) is pressed */
        if (LT2.action.WasPressedThisFrame())
            stow.Stow();

        /* Switch modes if A is pressed */
        if (RA.action.WasPressedThisFrame())
            manager.nextMode();

        /* Fully open/close gripper if right trigger (RT2) is pressed */
        if (RT2.action.WasPressedThisFrame())
        {
            if (gripperOpen)
            {
                gripper.closeGripper();
                gripperPercentage = 0f;
                gripperOpen = false;
            }
            else
            {
                gripper.openGripper();
                gripperPercentage = 100f;
                gripperOpen = true;
            }
        }

        /* Toggle UI if Y is pressed */
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
