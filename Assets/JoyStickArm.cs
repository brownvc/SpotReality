using System;
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
    public InputActionReference RAx; // Right joystick controls slow close and open with LT1 toggle
    public InputActionReference bButton;
    public Transform spotBody;
    public DrawMeshInstanced[] cloudsToFreeze;
    public TransformUpdater handExtUpdater;
    public JPEGImageSubscriber handImageSubscriber;
    public SetGripper gripper;

    // joyadd
    public JoyArmPublisher joyArmPublisher;

    public VRGeneralControls generalControls;
    public RawImageSubscriber[] depthSubscribers; // all depth subscribers except back, because if hand could move in front of a camera, depth history should be off
                                    

    public Material translucentSpotMaterial;
    public Material opaqueSpotMaterial;
    public Material opaqueSpotArmMaterial;
    public Material translucentSpotArmMaterial;


    private double armFrontBack;
    private double armLeftRight;
    private double armUpDown;
    private float gripperRotate;
    private float gripperSwing;
    private float gripperNod;


    private void OnEnable()
    {

        setSpotVisible(spotBody, false);
        armFrontBack = 0.0d;
        armLeftRight = 0.0d;
        armUpDown = 0.0d;
        gripperRotate = 0.0f;
        gripperSwing = 0.0f;
        gripperNod = 0.0f;
    }

    void Update()
    {
        Vector3 locationChange;
        Quaternion rotationChange = new Quaternion(0f, 0f, 0f, 0f);

        // read left and right joystick movement
        Vector2 laxMove = LAx.action.ReadValue<Vector2>();
        Vector2 raxMove = RAx.action.ReadValue<Vector2>();

        // if any move start, start publish
        if (laxMove.x != 0 || laxMove.y != 0 || raxMove.y !=0)
        {
            // gripper rotation
            if (LT1.action.IsPressed())
            {
                gripperRotate = Math.Sign(laxMove.y) * 3.1415f / 8f;
                rotationChange.x = gripperRotate;

                joyArmPublisher.setCoordinate(0.0f, 0.0f, 0.0f, rotationChange);
            }
            // gripper nod and swing
            else if (RT1.action.IsPressed())
            {

                gripperNod = Math.Sign(raxMove.y) * 3.1415f / 12f;

                gripperSwing = Math.Sign(laxMove.x) * 3.1415f / 12f;

                // (rotate,nod,swing)
                //rotationChange = Quaternion.Euler(0f, -gripperNod, gripperSwing);
                
                rotationChange.y = -gripperNod;
                rotationChange.z = -gripperSwing;

                joyArmPublisher.setCoordinate(0.0f, 0.0f, 0.0f, rotationChange);
            }
            else
            // move arm using a velocity mapping by trigonometric functions
            {
                // left joystick
                armFrontBack = laxMove.y;
                armLeftRight = laxMove.x;
                armFrontBack = 0.01d * Math.Tan(1.55d * armFrontBack);
                armLeftRight = -0.01d * Math.Tan(1.55d * armLeftRight);

                // right joystick
                armUpDown = raxMove.y;
                armUpDown = 0.01d * Math.Tan(1.55d * armUpDown);

                // publish the coordinate (convert double to float)
                joyArmPublisher.setCoordinate((float)armFrontBack, (float)armLeftRight, (float)armUpDown, rotationChange);

            }

        }



        // Change the gripper percentage
        //if (LT1.action.IsPressed())
        //{
        //    Vector2 leftMove = LAx.action.ReadValue<Vector2>();
        //    if (leftMove.y < 0)
        //    {
        //        if (generalControls.gripperPercentage > 0)
        //        {
        //            generalControls.gripperPercentage -= 0.25f;
        //            gripper.setGripperPercentage(generalControls.gripperPercentage);
        //            generalControls.gripperOpen = false;
        //        }

        //    }
        //    if (leftMove.y > 0)
        //    {
        //        if (generalControls.gripperPercentage < 100.0f)
        //        {
        //            generalControls.gripperPercentage += 0.25f;
        //            gripper.setGripperPercentage(generalControls.gripperPercentage);
        //            generalControls.gripperOpen = true;
        //        }
        //    }

        //}

        
        // Freeze or unfreeze the hand point cloud
        if (bButton.action.WasPressedThisFrame())
        {
            //// Switch visibility
            //showSpotBody = !showSpotBody;

            //// Set invisible or visible
            //setSpotVisible(spotBody, showSpotBody);

            foreach (DrawMeshInstanced cloud in cloudsToFreeze)
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
    }
}
