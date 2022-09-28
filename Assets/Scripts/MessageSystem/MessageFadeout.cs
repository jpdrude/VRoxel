using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageFadeout : MonoBehaviour
{
    Text text;
    float timer = 0;
    float displayTime;
    float fadeOutTime;
    Color startColor;
    Color endColor;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer < displayTime) return;

        if (timer > displayTime + fadeOutTime)
        {
            DestroyText();
            return;
        }

        if (text != null)
            text.color = Color.Lerp(startColor, endColor, (timer - displayTime) / fadeOutTime);
    }

    public void Initialize(string message, float _displayTime, float _fadeOutTime, bool warning = false)
    {
        text = GetComponent<Text>();
        text.text = message;

        displayTime = _displayTime;
        fadeOutTime = _fadeOutTime;

        if (!warning)
            startColor = text.color;
        else
        {
            startColor = MessageSystem.WarningColor;
            text.color = startColor;
        }
        endColor = startColor;
        endColor.a = 0;
    }

    public void DestroyText()
    {
        MessageSystem.CurrentMessages.Remove(gameObject);
        Destroy(gameObject);
    }
}
