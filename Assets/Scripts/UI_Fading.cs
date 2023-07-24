using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Fading : MonoBehaviour
{

    public int degreesOfFade = 40;
    public int startFull = 80; //when the alpha is fully Opaque
    public int endFull = 140;
    private int startFadeIn;
    private int endFadeOut;

    private CanvasGroup handUI;




    // Start is called before the first frame update
    void Start()
    {
        handUI = this.GetComponentInChildren<CanvasGroup>();

        startFadeIn = startFull - degreesOfFade;
        endFadeOut = endFull + degreesOfFade;
    }

    // Update is called once per frame
    void Update()
    {
        float angle = this.transform.localEulerAngles.z;

        if (angle < startFadeIn || angle > endFadeOut)
            handUI.alpha = 0;
        else if (angle < startFull)
            handUI.alpha = (degreesOfFade - (startFull - angle)) / degreesOfFade;
        else if (angle <= endFull)
            handUI.alpha = 1;
        else if (angle <= endFadeOut)
            handUI.alpha = (degreesOfFade-(angle-endFull)) / degreesOfFade;
;
    }
}
