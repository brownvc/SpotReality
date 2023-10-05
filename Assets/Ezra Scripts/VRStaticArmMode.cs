using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRStaticArmMode : MonoBehaviour
{

    public GhostArmPublisher gArmPublisher;
    public VRSliderInput sliderValues;
    public Transform rightHand;
    public Transform targetPos;
    private Transform prevRightHand;
    public InputActionReference RT1;
    public bool controlInUnity;


    private void Start()
    {
        prevRightHand = rightHand;
    }

    void Update()
    {
        if (controlInUnity)
            gArmPublisher.PublishGhostArm();
        else
        {
            //Transform changeInRightHandPos = rightHand.transform - prevRightHand.transform;
            //targetPos.position = rightHand.position;
            //targetPos.rotation = rightHand.rotation;

            if (RT1.action.IsPressed())
                gArmPublisher.PublishGhostArm();
        }
    }

    private void OnDisable()
    {
        sliderValues.SetPrevAngles(gArmPublisher.GetGhostArmAngles());
    }
}
