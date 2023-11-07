using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetOffsetMenu : MonoBehaviour
{
    [Header("UI ELements")]
    [SerializeField]
    TMP_InputField inputGlobalX;
    [SerializeField]
    TMP_InputField inputGlobalY;
    [SerializeField]
    TMP_InputField inputGlobalZ;

    [Space]
    [SerializeField]
    TMP_InputField inputLocalX;
    [SerializeField]
    TMP_InputField inputLocalY;
    [SerializeField]
    TMP_InputField inputLocalZ;

    UnityEngine.EventSystems.EventSystem eventSystem;

    private void Start()
    {
        eventSystem = UnityEngine.EventSystems.EventSystem.current;
    }

    private void Update()
    {
        CheckFocusChange();
    }

    void CheckFocusChange()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputGlobalX.isFocused)
                eventSystem.SetSelectedGameObject(inputGlobalY.gameObject);
            else if (inputGlobalY.isFocused)
                eventSystem.SetSelectedGameObject(inputGlobalZ.gameObject);
            else if (inputGlobalZ.isFocused)
                eventSystem.SetSelectedGameObject(inputGlobalX.gameObject);

            if (inputLocalX.isFocused)
                eventSystem.SetSelectedGameObject(inputLocalY.gameObject);
            else if (inputLocalY.isFocused)
                eventSystem.SetSelectedGameObject(inputLocalZ.gameObject);
            else if (inputLocalZ.isFocused)
                eventSystem.SetSelectedGameObject(inputLocalX.gameObject);
        }
    }

    private void OnEnable()
    {
        inputGlobalX.text = GameManager.Bond.Offset.x.ToString("F2");
        inputGlobalY.text = GameManager.Bond.Offset.y.ToString("F2");
        inputGlobalZ.text = GameManager.Bond.Offset.z.ToString("F2");
    }

    public void SetGlobalX(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputGlobalX.text = GameManager.Bond.Offset.x.ToString("F4");
            return;
        }

        if (Mathf.Abs((float)value - GameManager.Bond.Offset.x) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.x = RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputGlobalX.text = GameManager.Bond.Offset.x.ToString("F4");
        }
    }

    public void SetGlobalY(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputGlobalY.text = GameManager.Bond.Offset.y.ToString("F4");
            return;
        }

        if (Mathf.Abs((float)value - GameManager.Bond.Offset.y) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.y = RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputGlobalY.text = GameManager.Bond.Offset.y.ToString("F4");
        }
    }

    public void SetGlobalZ(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputGlobalZ.text = GameManager.Bond.Offset.z.ToString("F4");
            return;
        }

        if (Mathf.Abs((float)value - GameManager.Bond.Offset.z) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.z = RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputGlobalZ.text = GameManager.Bond.Offset.z.ToString("F4");
        }
    }

    public void SetLocalX(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputLocalX.text = "0";
            return;
        }

        if (Mathf.Abs((float)value) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.x += RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputLocalX.text = "0";
            inputGlobalX.text = GameManager.Bond.Offset.x.ToString("F4");
        }
    }

    public void SetLocalY(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputLocalY.text = "0";
            return;
        }

        if (Mathf.Abs((float)value) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.y += RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputLocalY.text = "0";
            inputGlobalY.text = GameManager.Bond.Offset.y.ToString("F4");
        }
    }

    public void SetLocalZ(string textValue)
    {
        float? value = textValue.ParseStringToFloat();
        if (value == null)
        {
            inputLocalZ.text = "0";
            return;
        }

        if (Mathf.Abs((float)value) > 0.0001)
        {
            Vector3 offset = GameManager.Bond.Offset;
            offset.z += RoundValue((float)value, 4);
            GameManager.Bond.Offset = offset;

            inputLocalZ.text = "0";
            inputGlobalZ.text = GameManager.Bond.Offset.z.ToString("F4");
        }
    }

    float RoundValue(float number, int decimalPositions)
    {
        int multiplier = Mathf.RoundToInt(Mathf.Pow(10, decimalPositions));
        int intNum = Mathf.RoundToInt(number * multiplier);
        number = intNum / ((float)multiplier);
        return number;
    }
}
