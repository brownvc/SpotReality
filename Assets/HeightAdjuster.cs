using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class HeightAdjuster : MonoBehaviour
{
    public InputActionReference goHigher;
    public InputActionReference goLower;
    public Transform cameraTransform;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Detect input values
        bool low = goLower.action.ReadValue<float>() != 0f;
        bool high = goHigher.action.ReadValue<float>() != 0f;

        // Go lower
        if (low && !high)
        {
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y - speed, cameraTransform.position.z);
        }
        else if(!low && high)
        {
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y + speed, cameraTransform.position.z);
        }
    }
}
