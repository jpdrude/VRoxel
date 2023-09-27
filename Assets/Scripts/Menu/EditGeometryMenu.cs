using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

public class EditGeometryMenu : MonoBehaviour
{
    [Header("Model")]
    [SerializeField]
    GameObject model;
    public GameObject Model { get { return model; } }

    [Space]
    [Header("UI Elements")]
    [SerializeField]
    TMP_InputField inputName;
    [SerializeField]
    TMP_InputField inputPosX;
    [SerializeField]
    TMP_InputField inputPosY;
    [SerializeField]
    TMP_InputField inputPosZ;

    [Space]
    [SerializeField]
    TMP_InputField inputRotX;
    [SerializeField]
    TMP_InputField inputRotY;
    [SerializeField]
    TMP_InputField inputRotZ;

    [Space]
    [SerializeField]
    TMP_InputField inputScaleX;
    [SerializeField]
    TMP_InputField inputScaleY;
    [SerializeField]
    TMP_InputField inputScaleZ;

    [Space]
    [SerializeField]
    GameObject lockClosed;
    [SerializeField]
    GameObject lockOpen;

    UnityEngine.EventSystems.EventSystem eventSystem;

    private void Start()
    {
        eventSystem = UnityEngine.EventSystems.EventSystem.current;
    }

    private void OnEnable()
    {
        UpdateTransformUIElements();
        UpdateLockUIElements();
        UpdateNameUIElement();
    }

    private void Update()
    {
        CheckFocusChange();
    }

    void CheckFocusChange()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputName.isFocused)
                eventSystem.SetSelectedGameObject(inputPosX.gameObject);
            if (inputPosX.isFocused)
                eventSystem.SetSelectedGameObject(inputPosY.gameObject);
            if (inputPosY.isFocused)
                eventSystem.SetSelectedGameObject(inputPosZ.gameObject);
            if (inputPosZ.isFocused)
                eventSystem.SetSelectedGameObject(inputRotX.gameObject);
            if (inputRotX.isFocused)
                eventSystem.SetSelectedGameObject(inputRotY.gameObject);
            if (inputRotY.isFocused)
                eventSystem.SetSelectedGameObject(inputRotZ.gameObject);
            if (inputRotZ.isFocused)
                eventSystem.SetSelectedGameObject(inputScaleX.gameObject);
            if (inputScaleX.isFocused)
                eventSystem.SetSelectedGameObject(inputScaleY.gameObject);
            if (inputScaleY.isFocused)
                eventSystem.SetSelectedGameObject(inputScaleZ.gameObject);
        }
    }

    public void InitializeModel(GameObject go)
    {
        model = go;

        if (model.layer == 2)
            SetLock(true);
        else
            SetLock(false); 

        UpdateTransformUIElements();
        UpdateLockUIElements();
        UpdateNameUIElement();
    }

    void UpdateTransformUIElements()
    {
        inputPosX.text = model.transform.position.x.ToString("F2");
        inputPosY.text = model.transform.position.y.ToString("F2");
        inputPosZ.text = model.transform.position.z.ToString("F2");

        inputRotX.text = model.transform.rotation.eulerAngles.x.ToString("F0");
        inputRotY.text = model.transform.rotation.eulerAngles.y.ToString("F0");
        inputRotZ.text = model.transform.rotation.eulerAngles.z.ToString("F0");

        inputScaleX.text = model.transform.localScale.x.ToString("F2");
        inputScaleY.text = model.transform.localScale.y.ToString("F2");
        inputScaleZ.text = model.transform.localScale.z.ToString("F2");
    }

    void UpdateLockUIElements()
    {
        if (model.layer == 2)
        {
            lockClosed.SetActive(true);
            lockOpen.SetActive(false);
        }
        else
        {
            lockClosed.SetActive(false);
            lockOpen.SetActive(true);
        }
    }

    void UpdateNameUIElement()
    {
        inputName.text = model.name;
    }

    void SetLock(bool locked)
    {
        foreach (MeshCollider mc in model.GetComponentsInChildren<MeshCollider>())
        {
            if (locked)
                mc.gameObject.layer = 2;
            else
                mc.gameObject.layer = 11;
        }

        if (locked)
            model.layer = 2;
        else
            model.layer = 11;
    }

    public void SetVisibility(bool value)
    {
        model.SetActive(value);
    }

    public void SetName(string value)
    {
        model.name = value;
    }

    public void SetPosX(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputPosX.text = model.transform.position.x.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.position.x) > 0.01)
        {
            Vector3 pos = model.transform.position;
            pos.x = (float)value;
            model.transform.position = pos;

            inputPosX.text = pos.x.ToString("F2");
        }
    }

    public void SetPosY(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputPosY.text = model.transform.position.y.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.position.y) > 0.01)
        {
            Vector3 pos = model.transform.position;
            pos.y = (float)value;
            model.transform.position = pos;

            inputPosY.text = pos.y.ToString("F2");
        }
    }

    public void SetPosZ(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputPosZ.text = model.transform.position.z.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.position.z) > 0.01)
        {
            Vector3 pos = model.transform.position;
            pos.z = (float)value;
            model.transform.position = pos;

            inputPosZ.text = pos.z.ToString("F2");
        }
    }

    public void SetRotX(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputRotX.text = model.transform.rotation.eulerAngles.x.ToString("F0");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.rotation.eulerAngles.x) > 1)
        {
            Vector3 rot = model.transform.rotation.eulerAngles;
            rot.x = (float)value;
            model.transform.rotation = Quaternion.Euler(rot);

            inputRotX.text = rot.x.ToString("F0");
        }
    }

    public void SetRotY(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputRotY.text = model.transform.rotation.eulerAngles.y.ToString("F0");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.rotation.eulerAngles.y) > 1)
        {
            Vector3 rot = model.transform.rotation.eulerAngles;
            rot.y = (float)value;
            model.transform.rotation = Quaternion.Euler(rot);

            inputRotY.text = rot.y.ToString("F0");
        }
    }

    public void SetRotZ(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputRotZ.text = model.transform.rotation.eulerAngles.z.ToString("F0");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.rotation.eulerAngles.z) > 1)
        {
            Vector3 rot = model.transform.rotation.eulerAngles;
            rot.z = (float)value;
            model.transform.rotation = Quaternion.Euler(rot);

            inputRotZ.text = rot.z.ToString("F0");
        }
    }

    public void SetScaleX(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputScaleX.text = model.transform.localScale.x.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.localScale.x) > 0.01)
        {
            Vector3 scale = model.transform.localScale;
            scale.x = (float)value;
            model.transform.localScale = scale;

            inputScaleX.text = scale.x.ToString("F2");
        }
    }

    public void SetScaleY(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputScaleY.text = model.transform.localScale.y.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.localScale.y) > 0.01)
        {
            Vector3 scale = model.transform.localScale;
            scale.y = (float)value;
            model.transform.localScale = scale;

            inputScaleY.text = scale.y.ToString("F2");
        }
    }

    public void SetScaleZ(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputScaleZ.text = model.transform.localScale.z.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - model.transform.localScale.z) > 0.01)
        {
            Vector3 scale = model.transform.localScale;
            scale.z = (float)value;
            model.transform.localScale = scale;

            inputScaleZ.text = scale.z.ToString("F2");
        }
    }

    public void ToggleLock()
    {
        if (model.layer == 2)
        {
            lockOpen.SetActive(true);
            lockClosed.SetActive(false);
            SetLock(false);
        }
        else
        {
            lockOpen.SetActive(false);
            lockClosed.SetActive(true);
            SetLock(true);
        }
    }

    public void DuplicateFromMenu()
    {
        GameObject go = DuplicateModel();
    }

    public GameObject DuplicateModel()
    {
        var newModel = Instantiate(model, model.transform.parent);

        string name = model.name;
        bool isDuplicate = true;

        for (int i = name.Length - 3; i < name.Length; ++i)
        {
            if (!char.IsDigit(name.ToCharArray()[i]))
                isDuplicate = false;
        }

        int bestChild = 0;
        if (isDuplicate)
        {
            int highest = 1;
            for (int i = 0; i < transform.parent.childCount; ++i)
            {
                string currentName = transform.parent.GetChild(i).GetComponent<EditGeometryMenu>().Model.name;
                if (name.Substring(0, name.Length - 3) != currentName.Substring(0, currentName.Length - 3))
                    continue;

                StringBuilder lastDigits = new StringBuilder();
                for (int j = currentName.Length - 3; j < currentName.Length; ++j)
                {
                    lastDigits.Append(currentName.ToCharArray()[j]);
                }

                int number = int.Parse(lastDigits.ToString());
                if (number > highest)
                {
                    highest = number;
                    bestChild = i;
                }
            }

            highest += 1;
            name = name.Substring(0, name.Length - 3);
            name = name + highest.ToString("D3");

            newModel.name = name;
        }
        else
        {
            bestChild = transform.GetSiblingIndex();
            newModel.name = name + "_002";
            model.name = name + "_001";
            UpdateNameUIElement();
        }

        Vector3 newPos = transform.parent.GetChild(bestChild).GetComponent<EditGeometryMenu>().Model.transform.position;
        newPos.x = newPos.x + 1;
        newModel.transform.position = newPos;
        newModel.GetComponent<GeometryItemRoot>().InitializeRotation();

        var newMenuEntryGo = Instantiate(gameObject, transform.parent);
        newMenuEntryGo.GetComponent<EditGeometryMenu>().InitializeModel(newModel);
        newMenuEntryGo.transform.SetSiblingIndex(bestChild + 1);

        transform.parent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, transform.parent.childCount * 80);

        newModel.GetComponent<GeometryMenuItemReference>().MenuItem = newMenuEntryGo.GetComponent<EditGeometryMenu>();

        return newModel;
    }

    public void DeleteModel()
    {
        Destroy(model);
        Destroy(gameObject);

        transform.parent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (transform.parent.childCount - 1) * 80);
    }

    float? ParseStringToFloat(string text)
    {
        string engFloatRegex = @"^[-+]?[0-9]*(?:\.[0-9]*)?$";
        string gerFloatRegex = @"^[-+]?[0-9]*(?:\,[0-9]*)?$";

        Match engMatch = Regex.Match(text, engFloatRegex);
        Match gerMatch = Regex.Match(text, gerFloatRegex);

        float? result = null;

        if (engMatch.Success)
        {
            CultureInfo culture = new CultureInfo("en-US");
            float number = 0;
            if (!float.TryParse(engMatch.Value, NumberStyles.Any, culture, out number))
                result = null;
            else
                result = number;
        }
        else if (gerMatch.Success)
        {
            CultureInfo culture = new CultureInfo("de-DE");
            float number = 0;
            if (!float.TryParse(gerMatch.Value, NumberStyles.Any, culture, out number))
                result = null;
            else
                result = number;
        }

        return result;
    }
}
