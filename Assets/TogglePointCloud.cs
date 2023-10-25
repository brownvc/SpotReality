using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TogglePointCloud : MonoBehaviour
{

    public InputActionReference triggerButton;
    public GameObject body;
    private DrawMeshInstanced[] pointClouds;
    private float t;

    // Start is called before the first frame update
    void Start()
    {
        pointClouds = body.GetComponentsInChildren<DrawMeshInstanced>();
        t = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if(triggerButton.action.WasPressedThisFrame())
        {
            // Swap value of t
            t = t == 1f ? 0f : 1f;
            foreach (DrawMeshInstanced cloud in pointClouds)
            {
                cloud.t = t;
            }
        }
    }
}
