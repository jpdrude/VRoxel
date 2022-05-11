using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PermanentMessage : MonoBehaviour
{
    Text text;
    TextMeshPro tmProText;
    bool tmPro = false;
    float timer = 0;
    float displayTime;
    float fadeOutTime;
    Color startColor;
    Color endColor;
    int priority = 0;

    public void Initialize(float _displayTime, float _fadeOutTime)
    {
        try
        {
            text = GetComponent<Text>();
            startColor = text.color;
        }
        catch
        {
            tmProText = GetComponent<TextMeshPro>();
            startColor = tmProText.color;
            tmPro = true;
        }

        displayTime = _displayTime;
        fadeOutTime = _fadeOutTime;

        endColor = startColor;
        endColor.a = 0;
    }

    public void DisplayMessage(string message, int _priority = 0)
    {
        if (!tmPro)
        {
            text.text = message;
            text.color = startColor;
        }
        else
        {
            if (priority > _priority) return;

            priority = _priority;
            tmProText.text = message;
            tmProText.color = startColor;
        }

        timer = 0;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < displayTime) return;

        if (timer > displayTime + fadeOutTime)
        {
            priority = 0;
            return;
        }

        if (!tmPro)
            text.color = Color.Lerp(startColor, endColor, (timer - displayTime) / fadeOutTime);
        else
            tmProText.color = Color.Lerp(startColor, endColor, (timer - displayTime) / fadeOutTime);
    }
}
