using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

public class VRMenuItem : MonoBehaviour
{
    [Header("Event")]
    [SerializeField]
    SteamVR_Action_Boolean trigger;
    [SerializeField]
    SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.RightHand;
    public SteamVR_Input_Sources InputSource { get { return inputSource; } set { inputSource = value; } }

    [SerializeField]
    UnityEvent action;
    public UnityEvent Action { get { return action; } }
    [SerializeField]
    UnityEvent actionPressed;

    [SerializeField]
    bool interactable = true;

    Material originalMat = null;
    Material disabledMat = null;


    public bool Interactable
    {
        get { return interactable; }
        set
        {
            if (originalMat == null) SetMaterials();

            interactable = value;

            if (!interactable)
            {
                run = true;
                reverse = true;
                GetComponentInChildren<MeshRenderer>().material = disabledMat;
            }
            else
                GetComponentInChildren<MeshRenderer>().material = originalMat;
        }
    }
    

    [Header("Focus Animation Settings")]
    [SerializeField]
    float effectLength = 0.3f;
    [SerializeField]
    float effectScale = 1.25f;
    float timer = 0;
    bool run = false;
    bool reverse = false;
    bool focused = false;

    void SetMaterials()
    {
        var renderer = GetComponentInChildren<MeshRenderer>(true);
        if (renderer == null) return;
        originalMat = renderer.material;
        disabledMat = new Material(originalMat);
        disabledMat.color *= 0.5f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!interactable) return;
        if (other.gameObject.tag != "MenuSelector") return;

        run = true;
        reverse = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!interactable) return;
        if (other.gameObject.tag != "MenuSelector") return;

        run = true;
        reverse = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!interactable) return;
        if (other.gameObject.tag != "MenuSelector") return;
        focused = true;
    }

    private void Update()
    {
        if (focused && trigger.GetStateDown(inputSource))
            action.Invoke();

        if (focused && trigger.GetState(InputSource))
            actionPressed.Invoke();

        focused = false;

        if (!run) return;

        if (reverse)
            timer -= Time.deltaTime;
        else
            timer += Time.deltaTime;

        if (timer > effectLength)
        {
            timer = effectLength;
            run = false;
        }    
        else if (timer < 0)
        {
            timer = 0;
            run = false;
        }

        transform.localScale =  Vector3.Lerp(Vector3.one, Vector3.one * effectScale, timer / effectLength);
    }

    public void RemoteInvoke()
    {
        action.Invoke();
    }

    public void RemoteInvokePressed()
    {
        actionPressed.Invoke();
    }
}
