using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageSystem : MonoBehaviour
{
    [SerializeField]
    float displayTime = 3;
    static float dTime;

    [SerializeField]
    float fadeOutTime = 0.5f;
    static float foTime;

    [SerializeField]
    GameObject messageTemplate;
    static GameObject template;

    [SerializeField]
    PermanentMessage permanentMessage;
    static PermanentMessage permanent;

    [SerializeField]
    PermanentMessage permanentVRMessage;
    static PermanentMessage permanentVR = null;

    [SerializeField]
    Color warningColor;
    static Color warningCol;
    public static Color WarningColor { get { return warningCol; } }

    static List<GameObject> currentMessages = new List<GameObject>();
    public static List<GameObject> CurrentMessages { get { return currentMessages; } }

    private void Start()
    {
        template = messageTemplate;
        dTime = displayTime;
        foTime = fadeOutTime;
        warningCol = warningColor;

        permanent = permanentMessage;
        permanent.Initialize(displayTime, fadeOutTime);

        if (permanentVRMessage != null)
        {
            permanentVR = permanentVRMessage;
            permanentVR.Initialize(displayTime, fadeOutTime);
        }
    }



    public static void DisplayMessage(string message)
    {
        GameObject messageGo = Instantiate(template, template.transform, template.transform.parent);
        var fadeout = messageGo.AddComponent<MessageFadeout>();
        fadeout.Initialize(message, dTime, foTime);

        foreach (GameObject m in currentMessages)
        {
            m.transform.localPosition = m.transform.localPosition + new Vector3(0, 20, 0);
            if (m.transform.localPosition.y > 150) m.GetComponent<MessageFadeout>().DestroyText();
        }

        currentMessages.Add(messageGo);

        if (permanentVR != null)
            permanentVR.DisplayMessage(message, 1);
    }

    public static void DisplayPermanentMessage(string message)
    {
        permanent.DisplayMessage(message);

        if (permanentVR != null)
            permanentVR.DisplayMessage(message);
    }

    public static void DisplayWarning(string message)
    {
        GameObject messageGo = Instantiate(template, template.transform, template.transform.parent);
        var fadeout = messageGo.AddComponent<MessageFadeout>();
        fadeout.Initialize(message, dTime, foTime, true);

        foreach (GameObject m in currentMessages)
        {
            m.transform.localPosition = m.transform.localPosition + new Vector3(0, 20, 0);
            if (m.transform.localPosition.y > 150) m.GetComponent<MessageFadeout>().DestroyText();
        }

        currentMessages.Add(messageGo);

        if (permanentVR != null)
            permanentVR.DisplayMessage(message, 1);
    }
}
