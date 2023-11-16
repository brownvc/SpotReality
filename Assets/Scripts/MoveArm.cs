using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveArm : MonoBehaviour
{
    public RosSharp.RosBridgeClient.PoseStampedRelativePublisher armPublisher; // Reference to RosConnnector's arm publisher
    public GameObject rightController; // Reference to right controller object
    public GameObject dummyFinger; // Reference to dummy finger object

    // private MessageTypes.Geometry.Twist message;
    private bool triggerWasPressed = false;
    private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion initialHandRotation = Quaternion.identity;
    private Quaternion initialDummyRotation = Quaternion.identity;
    public InputActionReference RT1; // Changing how input actions are received
    public InputActionReference bButton;
    public Transform spotBody;

    private bool hideSpotBody = false;

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
                initialDummyRotation = dummyFinger.transform.rotation;
            }
            else
            {
                // Change the location of the finger the same way
                locationChange = (rightController.transform.position - lastHandLocation);
                rotationChange = rightController.transform.rotation * Quaternion.Inverse(initialHandRotation);
                dummyFinger.transform.position += locationChange;
                dummyFinger.transform.rotation = rotationChange * initialDummyRotation;
            }
            lastHandLocation = rightController.transform.position;
        }
        else
        {
            // trigger is not pressed
            triggerWasPressed = false;

            // turn off dummy hand tracking
            armPublisher.enabled = false;
        }

        // Hide or show Spot's rendering
        if (bButton.action.WasPressedThisFrame())
        {
            // Set invisible or visible
            setSpotVisible(spotBody, hideSpotBody);

            // Switch for next time
            hideSpotBody = !hideSpotBody;
        }
    }



    // Recursive function to get all children of the parent that have the name "unnamed" and are children of "Visuals"
    // Ignores children of arm0.link_wr0 and dummy_link_fngr
    // and set them active or inactive
    private void setSpotVisible(Transform parent, bool visible)
    {
        foreach(Transform child in parent) 
        {
            if (parent.gameObject.name == "Visuals" && child.gameObject.name == "unnamed")
            {
                child.gameObject.SetActive(visible);
            }
            else if (child.gameObject.name == "arm0.link_wr1" || child.gameObject.name == "dummy_link_fngr")
            {
                continue;
            }
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
        setSpotVisible(spotBody, true);
    }
}

