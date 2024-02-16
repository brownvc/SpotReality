using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRDynamicArmMode : MonoBehaviour
{

    public GhostArmPublisher gArmPublisher;
    public VRSliderInput sliderValues;
    public Transform rightHand;
    public Transform targetPos;
    public InputActionReference RT1;
    public bool controlInUnity;



    void Update()
    {
        if (controlInUnity)
            gArmPublisher.PublishGhostArm();
        else
        {
            targetPos.position = rightHand.position;
            targetPos.rotation = rightHand.rotation;

            if (RT1.action.IsPressed())
                gArmPublisher.PublishGhostArm();
        }
    }

    private void OnDisable()
    {
        sliderValues.SetPrevAngles(gArmPublisher.GetGhostArmAngles());
    }
}
