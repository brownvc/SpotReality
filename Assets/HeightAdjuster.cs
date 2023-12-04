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
    public InputActionReference LAx;
    public InputActionReference RAx;
    public Transform cameraTransform;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 leftMove;
        Vector2 rightMove;

        /* Set the camera higher or lower */
        // Detect input values
        bool low = goLower.action.IsPressed();
        bool high = goHigher.action.IsPressed();


        // Go lower
        if (low && !high)
        {
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y - speed, cameraTransform.position.z);
        }
        // Go higher
        else if(!low && high)
        {
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y + speed, cameraTransform.position.z);
        }

        /* Move camera position around */
        leftMove = LAx.action.ReadValue<Vector2>() / 50f;
        cameraTransform.position += cameraTransform.rotation * new Vector3(leftMove.x, 0f, leftMove.y);

        /* Move camera rotation */
        rightMove = RAx.action.ReadValue<Vector2>();
        cameraTransform.Rotate(new Vector3(0f, rightMove.x, 0f));

    }
}
