using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BondScaleUI : MonoBehaviour
{
    [SerializeField]
    List<BondScaleManager> scaleManagers;

    [SerializeField]
    TMP_InputField input;

    [SerializeField]
    Slider slider;

    float[] scaleSliderValues = new float[15] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1, 1.5f, 2, 3, 4, 5 };

    float scale;
    float Scale
    {
        get { return scale; }
        set 
        { 
            scale = value;
            foreach (BondScaleManager bsm in scaleManagers)
                bsm.Scale = scale;

            if (scale < 0.15f)
                slider.value = 0;
            else if (scale < 0.25f)
                slider.value = 1;
            else if (scale < 0.35f)
                slider.value = 2;
            else if (scale < 0.45f)
                slider.value = 3;
            else if (scale < 0.55f)
                slider.value = 4;
            else if (scale < 0.65f)
                slider.value = 5;
            else if (scale < 0.75f)
                slider.value = 6;
            else if (scale < 0.85f)
                slider.value = 7;
            else if (scale < 0.95f)
                slider.value = 8;
            else if (scale < 1.25f)
                slider.value = 9;
            else if (scale < 1.75f)
                slider.value = 10;
            else if (scale < 2.5f)
                slider.value = 11;
            else if (scale < 3.5f)
                slider.value = 12;
            else if (scale < 4.5f)
                slider.value = 13;
            else
                slider.value = 14;
        }
    }

    bool changedThroughInput = false;

    public void SetSliderValue(float num)
    {
        if (changedThroughInput)
        {
            changedThroughInput = false;
            return;
        }

        int i = Mathf.RoundToInt(num);
        input.text = scaleSliderValues[i].ToString("F1");
        Scale = scaleSliderValues[i];
    }

    public void SetInputValue(string text)
    {
        float? _scale = text.ParseStringToFloat();
        if (_scale != null)
        {
            changedThroughInput = true;
            Scale = (float)_scale;
        }
        else
            input.text = scale.ToString("F3");
    }

    private void OnEnable()
    {
        /*
        bool elligableBond = false;

        foreach (BondScaleManager bsm in scaleManagers)
            if (bsm.Bond == GameManager.Bond)
                elligableBond = true;

        if (elligableBond)
        {
            Scale = GameManager.Bond.Scale;
            input.text = scale.ToString("F3");
        }
        else
        {
            Scale = 1;
            input.text = "1.0";
        }
        */
    }
}
