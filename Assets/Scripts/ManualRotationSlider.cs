using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualRotationSlider : MonoBehaviour
{
    public float sliderVal;
    private Slider slider;
    private TextMeshProUGUI sliderText;


    void Start()
    {
        slider = this.GetComponentInChildren<Slider>();
        sliderText = this.GetComponentInChildren<TextMeshProUGUI>();
    }

    //update slider's text
    void Update()
    {
        sliderVal = slider.value;
        sliderText.text = slider.value.ToString();
    }
}
