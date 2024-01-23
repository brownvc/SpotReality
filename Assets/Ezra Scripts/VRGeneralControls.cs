using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;

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

    /* To be accessed by MoveArm script */
    public bool gripperOpen;
    public float gripperPercentage;

    public RosSharp.RosBridgeClient.KillMotor killSpot;
    public RosSharp.RosBridgeClient.StowArm stow;
    public RosSharp.RosBridgeClient.SetGripper gripper;
    public ModeManager manager;

    /* Toggle Point Cloud */
    public GameObject body;
    private DrawMeshInstanced[] pointClouds;
    private float point_cloud_t;

    /* Track time in 2D vs 3D fields */
    private Stopwatch threed_time;
    private Stopwatch twod_time;

    void Start()
    {
        gripperOpen = false;
        gripper.closeGripper();
        hintUI.enabled = true;
        UI.enabled = false;
        UIShowing = false;
        gripperPercentage = 0f;

        pointClouds = body.GetComponentsInChildren<DrawMeshInstanced>();
        point_cloud_t = 1;
        threed_time = new Stopwatch();
        twod_time = new Stopwatch();

        threed_time.Start();
    }

    void Update()
    {

        /* LX commands */
        if (LX.action.WasPressedThisFrame())
        {
            /* Toggle every point cloud */
            point_cloud_t = point_cloud_t == 1f ? 0f : 1f;
            foreach (DrawMeshInstanced cloud in pointClouds)
            {
                cloud.t = point_cloud_t;
            }

            if (point_cloud_t == 1f)
            {
                /* Turn on 3D stopwatch */
                twod_time.Stop();
                threed_time.Start();
            }
            else
            {
                /* Turn on 2D stopwatch */
                threed_time.Stop();
                twod_time.Start();
            }

            /* Kill left gripper (LT1) and left trigger (LT2) are also pressed */
            if (LT1.action.IsPressed() && LT2.action.IsPressed())
            {
                killSpot.killSpot();
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

        /* Toggle point cloud if x was pressed this frame */
        if (LX.action.WasPressedThisFrame())
        {

        }

    }

    public void toggleUI()
    {
        UI.enabled = !(UI.enabled);
        hintUI.enabled = !(hintUI.enabled);
        UIShowing = !UIShowing;
    }

    private void OnApplicationQuit()
    {
        // Log time
        UnityEngine.Debug.Log("2D mode time elapsed: " + System.Math.Round(twod_time.Elapsed.TotalSeconds, 2) + " seconds");
        UnityEngine.Debug.Log("3D mode time elapsed: " + System.Math.Round(threed_time.Elapsed.TotalSeconds, 2) + " seconds");
    }
}
