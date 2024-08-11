using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpotMovingDetection : MonoBehaviour
{
    public InputActionReference LAxB;
    public InputActionReference LAx;
    public InputActionReference LX;
    public InputActionReference LY;
    public InputActionReference LT1;
    //public InputActionReference LT1Pressed;
    public InputActionReference LT2;
    public InputActionReference RAxB;
    public InputActionReference RAx;
    public InputActionReference RA;
    public InputActionReference RB;
    public InputActionReference RT1;
    public InputActionReference RT2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool is_moving()
    {
        return LAxB.action.IsPressed()
            || LAx.action.IsPressed()
            || LX.action.IsPressed()
            || LY.action.IsPressed()
            || LT1.action.IsPressed()
            || LT2.action.IsPressed()
            || RAxB.action.IsPressed()
            || RAx.action.IsPressed()
            || RA.action.IsPressed()
            || RB.action.IsPressed()
            || RT1.action.IsPressed()
            || RT2.action.IsPressed();
    }
}
