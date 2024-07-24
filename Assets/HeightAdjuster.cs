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
    public InputActionReference LT1;
    public Transform cameraOffset;
    public Transform mainCamera;
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
        Quaternion relativeRot;
        bool low;
        bool high;


        /* Set the camera higher or lower */
        low = goLower.action.IsPressed();
        high = goHigher.action.IsPressed();

        /* Go lower */
        if (low && !high)
        {
            cameraOffset.position = new Vector3(cameraOffset.position.x, cameraOffset.position.y - speed, cameraOffset.position.z);
        }
        /* Go higher */
        else if(!low && high)
        {
            cameraOffset.position = new Vector3(cameraOffset.position.x, cameraOffset.position.y + speed, cameraOffset.position.z);
        }

        if (!LT1.action.IsPressed())
        {
            /* Move camera position around according to left stick */
            leftMove = LAx.action.ReadValue<Vector2>() / 50f;
            relativeRot = Quaternion.Euler(0f, mainCamera.rotation.eulerAngles.y, 0f);// cameraTransform.rotation;
            cameraOffset.position += relativeRot * new Vector3(leftMove.x, 0f, leftMove.y);

            /* Adjust camera rotation according to right stick*/
            rightMove = RAx.action.ReadValue<Vector2>();
            if (rightMove.magnitude > 0f)
            {
                /* Only change one axis at a time */
                if (Math.Abs(rightMove.x) > Math.Abs(rightMove.y))
                {
                    /* Rotate left/right relative to world space */
                    cameraOffset.Rotate(new Vector3(0f, rightMove.x, 0f), Space.World);
                }
                else
                {
                    /* Rotate up/down relative to world space */
                    /* Disabled for now */
                    // cameraTransform.Rotate(new Vector3(rightMove.y * 0.5f, 0f, 0f), Space.World);
                }
                /* Don't allow z rotation to change */
                cameraOffset.rotation = Quaternion.Euler(new Vector3(cameraOffset.rotation.eulerAngles.x, cameraOffset.rotation.eulerAngles.y, 0f));
            }
        }
    }
}
