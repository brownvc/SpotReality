using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRDriveSpot : MonoBehaviour
{
    public InputActionReference LAx;
    public InputActionReference RAx;

    public RosSharp.RosBridgeClient.MoveSpot drive;


    void Start()
    {
        
    }

    void Update()
    {

        drive.drive(LAx.action.ReadValue<Vector2>(), RAx.action.ReadValue<Vector2>().x);

    }
}
