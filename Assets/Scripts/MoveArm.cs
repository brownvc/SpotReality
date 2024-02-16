using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
// using System.Drawing.Text;
using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class MoveArm : MonoBehaviour
{
    public RosSharp.RosBridgeClient.PoseStampedRelativePublisher armPublisher; // Reference to RosConnnector's arm publisher
    public GameObject rightController; // Reference to right controller object
    public Transform dummyHandTransform; // Reference to dummy hand object
    public Transform realHandTransform; // Reference to real hand

    public InputActionReference RT1; // Changing how input actions are received
    public InputActionReference LT1; // Toggle for slow open and close
    public InputActionReference LAx; // Left joystick controls slow close and open with LT1 toggle
    public InputActionReference bButton;
    public Transform spotBody;
    public DrawMeshInstanced handCloud;
    public TransformUpdater handExtUpdater;
    public JPEGImageSubscriber handImageSubscriber;
    public SetGripper gripper;
    public VRGeneralControls generalControls;
    public RawImageSubscriber[] depthSubscribers; // all depth subscribers except back, because if hand could move in front of a camera, depth history should be off

    // private MessageTypes.Geometry.Twist message;
    private bool triggerWasPressed = false;
    private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion initialHandRotation = Quaternion.identity;
    private Quaternion initialDummyRotation = Quaternion.identity;
    private Vector3 defaultDummyPos = new Vector3(-1f, -1f, -1f);
    private Quaternion defaultDummyRot = new Quaternion(-1f, -1f, -1f, -1f);

    // Used to change ghost gripper color when it goes out of reach - Are Oelsner
    public Transform armBase;
    public GameObject ghostArm;
    public GameObject ghostFinger;
    public Material greenMaterial;
    public Material redMaterial;
    private bool isGreen = true;
    private float maxArmLength = .73f;


    private bool hideSpotBody = false;
    public Material translucentSpotMaterial;
    public Material opaqueSpotMaterial;
    public Material opaqueSpotArmMaterial;
    public Material translucentSpotArmMaterial;
    


    private void OnEnable()
    {
        // Set the dummy hand to the same location as the real hand
        dummyHandTransform.position = realHandTransform.position;
        dummyHandTransform.rotation = realHandTransform.rotation;
    }

    void Update()
    {
        Vector3 locationChange;
        Quaternion rotationChange;

        // If the trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
        if (RT1.action.IsPressed())
        {
            // If trigger is just now getting pressed, save the location but don't send a command
            // Also turn on the publisher to track the dummy hand
            if (!triggerWasPressed)
            {
                triggerWasPressed = true;
                armPublisher.enabled = true;
                initialHandRotation = rightController.transform.rotation;
                initialDummyRotation = dummyHandTransform.rotation;
            }
            else
            {
                // Change the location of the hand the same way
                locationChange = (rightController.transform.position - lastHandLocation);
                rotationChange = rightController.transform.rotation * Quaternion.Inverse(initialHandRotation);
                dummyHandTransform.position += locationChange;
                dummyHandTransform.rotation = rotationChange * initialDummyRotation;
            }
            lastHandLocation = rightController.transform.position;
            // Change ghost gripper color if it is out of bounds            
            if ((dummyHandTransform.position - armBase.position).magnitude > maxArmLength)
            {
                if (isGreen)
                {
                    // Set ghost gripper color to red
                    ghostArm.GetComponent<MeshRenderer>().material = redMaterial;
                    ghostFinger.GetComponent<MeshRenderer>().material = redMaterial;
                    isGreen = false;
                }
            }
            else // Maybe check if color is already green / red before going to swap them
            {
                if (!isGreen)
                {
                    // Set ghost gripper color to green
                    ghostArm.GetComponent<MeshRenderer>().material = greenMaterial;
                    ghostFinger.GetComponent<MeshRenderer>().material = greenMaterial;
                    isGreen = true;
                }
            }


            // Pause depth history for 3 seconds
            foreach (RawImageSubscriber ds in depthSubscribers)
            {
                ds.pauseDepthHistory(3f);
            }
        }
       // Change the gripper percentage
        else if (LT1.action.IsPressed())
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
            // trigger is not pressed
            triggerWasPressed = false;

            // turn off dummy hand tracking
            armPublisher.enabled = false;
        }

        // Freeze or unfreeze the hand point cloud
        if (bButton.action.WasPressedThisFrame())
        {
            // Previous code to hide spot's body
            //// Set invisible or visible
            setSpotVisible(spotBody, hideSpotBody);

            //// Switch for next time
            hideSpotBody = !hideSpotBody;
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
                    if (visible)
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
        armPublisher.enabled = false;
        hideSpotBody = false;
        setSpotVisible(spotBody, false);
    }
}

