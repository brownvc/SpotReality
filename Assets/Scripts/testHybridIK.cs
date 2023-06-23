using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//testHybridIK: if user drags "dummyfinger", then targetTransform follows the dummyfinger position which in turn resets all joint location and positions by HybridIK scripts
public class testHybridIK : MonoBehaviour
{
    private GameObject dummyFinger;
    public GameObject targetTransform;
    private Vector3 initialPos;

    private GameObject armObj;
    private GameObject shoulder;
    private GameObject elbow0;
    private GameObject elbow1;
    private GameObject wrist0;
    private GameObject wrist1;
    private GameObject finger;



    // Start is called before the first frame update
    void Start()
    {
        targetTransform = GameObject.Find("TargetTransform");
        dummyFinger = GameObject.Find("dummy_link_fngr");
        initialPos = dummyFinger.transform.position;

        armObj = GameObject.Find("ghost arm");
        shoulder = GameObject.Find("newarm0.link_sh1");
        elbow0 = GameObject.Find("newarm0.link_el0");
        elbow1 = GameObject.Find("newarm0.link_el1");
        wrist0 = GameObject.Find("newarm0.link_wr0");
        wrist1 = GameObject.Find("newarm0.link_wr1");
        finger = GameObject.Find("newarm0.link_fngr");

    }

    // Update is called once per frame
    void Update()
    {
        targetTransform.transform.position = dummyFinger.transform.position;
        targetTransform.transform.rotation = dummyFinger.transform.rotation;

        //printing out the positions and rotations of each joint gameobject
        //Debug.Log(getJointPosition(armObj));
        Debug.Log("Joints:");
        Debug.Log(getJointRotation(armObj));
        //Debug.Log(getJointPosition(shoulder));
        Debug.Log(getJointRotation(shoulder));
        //Debug.Log(getJointPosition(elbow0));
        Debug.Log(getJointRotation(elbow0));
        Debug.Log(getJointRotation(elbow1));
        Debug.Log(getJointRotation(wrist0));
        Debug.Log(getJointRotation(wrist1));
        //Debug.Log(getJointRotation(finger));

    }

    public Vector3 getJointPosition(GameObject joint)
    {
        return joint.transform.position;
    }

    public Vector3 getJointRotation(GameObject joint)
    {
        return joint.transform.localEulerAngles;
    }
}
