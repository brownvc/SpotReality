using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class JoyStickArm : MonoBehaviour
{
    //public RosSharp.RosBridgeClient.PoseStampedRelativePublisher armPublisher; // Reference to RosConnnector's arm publisher
    //public GameObject rightController; // Reference to right controller object

    public InputActionReference RT1; // Changing how input actions are received
    public InputActionReference LT1; // Toggle for slow open and close
    public InputActionReference LAx; // Left joystick controls slow close and open with LT1 toggle
    public InputActionReference bButton2;
    public Transform spotBody;
    public DrawMeshInstanced[] cloudsToFreeze2;
    public TransformUpdater handExtUpdater;
    public JPEGImageSubscriber handImageSubscriber;
    public SetGripper gripper;
    public VRGeneralControls generalControls;
    public RawImageSubscriber[] depthSubscribers; // all depth subscribers except back, because if hand could move in front of a camera, depth history should be off

    private bool bButtonFlag = false;                                          


    public Material translucentSpotMaterial;
    public Material opaqueSpotMaterial;
    public Material opaqueSpotArmMaterial;
    public Material translucentSpotArmMaterial;



    private void OnEnable()
    {

        setSpotVisible(spotBody, false);
        bButtonFlag = true;
    }

    void Update()
    {
        Vector3 locationChange;
        Quaternion rotationChange;


        // Change the gripper percentage
        if (LT1.action.IsPressed())
        {
            Vector2 leftMove = LAx.action.ReadValue<Vector2>();
            if (leftMove.y < 0)
            {
                if (generalControls.gripperPercentage > 0)
                {
                    generalControls.gripperPercentage -= 0.25f;
                    gripper.setGripperPercentage(generalControls.gripperPercentage);
                    generalControls.gripperOpen = false;
                }

            }
            if (leftMove.y > 0)
            {
                if (generalControls.gripperPercentage < 100.0f)
                {
                    generalControls.gripperPercentage += 0.25f;
                    gripper.setGripperPercentage(generalControls.gripperPercentage);
                    generalControls.gripperOpen = true;
                }
            }

        }
        else
        {

            // turn off dummy hand tracking
            //armPublisher.enabled = false;
        }

        // Freeze or unfreeze the hand point cloud
        if (bButton2.action.WasPressedThisFrame() && bButtonFlag)
        {
            //// Switch visibility
            //showSpotBody = !showSpotBody;

            //// Set invisible or visible
            //setSpotVisible(spotBody, showSpotBody);
            Debug.Log("joystick b click");

            foreach (DrawMeshInstanced cloud in cloudsToFreeze2)
            {
                cloud.toggleFreezeCloud();
            }
        }
    }

    // Recursive function to get all children of the parent that have the name "unnamed" and are children of "Visuals"
    // Ignores children of arm0.link_wr0 and dummy_arm0.link_wr1
    // and set them active or inactive
    private void setSpotVisible(Transform parent, bool visible)
    {
        foreach (Transform child in parent)
        {
            if (parent.gameObject.name == "unnamed")
            {
                if (child.gameObject.GetComponent<MeshRenderer>() != null)
                {
                    if (!visible)
                    {
                        if (child.gameObject.name.Contains("arm"))
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = translucentSpotArmMaterial;
                        }
                        else
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = translucentSpotMaterial;
                        }
                    }
                    else
                    {
                        if (child.gameObject.name.Contains("arm"))
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = opaqueSpotArmMaterial;
                        }
                        else
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = opaqueSpotMaterial;
                        }
                    }
                }
            }
            else if (child.gameObject.name == "arm0.link_wr1" || child.gameObject.name == "dummy_arm0.link_wr1")
            {
                return;
            }
            // child.gameObject.SetActive(visible);

            else
            {
                setSpotVisible(child, visible);
            }
        }
    }

    private void OnDisable()
    {
        //armPublisher.enabled = false;
        setSpotVisible(spotBody, true);
        bButtonFlag = false;
    }
}
