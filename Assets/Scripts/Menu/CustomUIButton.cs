using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CustomUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent onMouseButton;
    public UnityEvent onMouseButtonDown;
    public UnityEvent onMouseButtonUp;

    bool entered = false;
    bool wasClicked = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        entered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        entered = false;
    }

    private void Update()
    {
        if (entered && Input.GetMouseButtonDown(0))
        {
            onMouseButtonDown.Invoke();
            wasClicked = true;
        }
            
        if (wasClicked && Input.GetMouseButtonUp(0))
        {
            onMouseButtonUp.Invoke();
            wasClicked = false;
        }
            
        if (entered && Input.GetMouseButton(0))
            onMouseButton.Invoke();
    }
}
