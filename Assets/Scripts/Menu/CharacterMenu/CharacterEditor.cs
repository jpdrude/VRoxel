using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using TMPro;
using Valve.Newtonsoft.Json;
using System.IO;
using SimpleFileBrowser;
using UnityEngine.Events;

public class CharacterEditor : MonoBehaviour
{
    [SerializeField]
    UnityEngine.EventSystems.EventSystem eventSystem;
    [SerializeField]
    UnityEngine.UI.GraphicRaycaster graphicRaycaster;

    [SerializeField]
    UnityEvent Resume;

    [SerializeField]
    CharacterOptions characterOptions;

    [Space]
    [Header("Dimension Input")]
    [SerializeField]
    TMP_InputField inputBodyHeight;
    [SerializeField]
    TMP_InputField inputFootLength;
    [SerializeField]
    TMP_InputField inputShoulderHeight;
    [SerializeField]
    TMP_InputField inputShoulderWidth;
    [SerializeField]
    TMP_InputField inputElbowSpan;
    [SerializeField]
    TMP_InputField inputWristSpan;
    [SerializeField]
    TMP_InputField inputArmSpan;
    [SerializeField]
    TMP_InputField inputHipHeight;
    [SerializeField]
    TMP_InputField inputHipWidth;
    [SerializeField]
    TMP_InputField inputKneeHeight;
    [SerializeField]
    TMP_InputField inputAnkleHeight;
    [SerializeField]
    TMP_InputField inputSoleThickness;

    [Space]
    [SerializeField]
    TMP_Dropdown dropdownFileUnits;

    [Space]
    [Header("Trails")]
    [SerializeField]
    Toggle toggleUseTrails;
    [SerializeField]
    Slider sliderTrailSteps;
    [SerializeField]
    Text labelTrailSteps;
    [SerializeField]
    Slider sliderTrailTime;
    [SerializeField]
    Text labelTrailTime;
    [SerializeField]
    Toggle toggleUseBrushTrails;
    [SerializeField]
    Slider sliderBrushStrokeLength;
    [SerializeField]
    Text labelBrushStrokeLength;

    [Space]
    [Header("Color")]
    [SerializeField]
    TMP_Dropdown dropdownColorSets;
    [SerializeField]
    List<CharacterColorSet> colorSets;

    [Space]
    [Header("Body Customization")]
    [SerializeField]
    GameObject bodyCustomizationPanel;
    [SerializeField]
    GameObject customizationScene;
    [SerializeField]
    CharacterModel bodyCustomizationModel;

    [Space]
    [SerializeField]
    Slider sliderUpperLeg;
    [SerializeField]
    Slider sliderLowerLeg;
    [SerializeField]
    Slider sliderUpperArm;
    [SerializeField]
    Slider sliderForeArm;

    [Space]
    [SerializeField]
    Slider sliderBottomSize;
    [SerializeField]
    Slider sliderBellySize;
    [SerializeField]
    Slider sliderBreastSize;
    [SerializeField]
    Slider sliderChestSize;
    [SerializeField]
    Slider sliderNeckSize;

    bool customizationActive = false;

    float unitFactor = 1;

    public static List<CharacterColorSet> ColorSets { get; private set; }

    bool simpleDimensions = false;

    private void Start()
    {
        string persistentPath = Application.persistentDataPath + "\\lastAvatarSettings.json";
        if (File.Exists(persistentPath))
            LoadAvatarDimensions(persistentPath);

        ColorSets = colorSets;
    }

    private void Update()
    {
        CheckFocusChange();

        if (customizationActive)
            bodyCustomizationModel.GetComponent<CharacterModel>().InitializeAvatar(characterOptions.bodyDimensions, ColorSets[characterOptions.colorSet]);
    }

    void CheckFocusChange()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputBodyHeight.isFocused)
                eventSystem.SetSelectedGameObject(inputFootLength.gameObject);
            if (inputFootLength.isFocused)
                eventSystem.SetSelectedGameObject(inputShoulderHeight.gameObject);
            if (inputShoulderHeight.isFocused)
                eventSystem.SetSelectedGameObject(inputShoulderWidth.gameObject);
            if (inputShoulderWidth.isFocused)
                eventSystem.SetSelectedGameObject(inputElbowSpan.gameObject);
            if (inputElbowSpan.isFocused)
                eventSystem.SetSelectedGameObject(inputWristSpan.gameObject);
            if (inputWristSpan.isFocused)
                eventSystem.SetSelectedGameObject(inputArmSpan.gameObject);
            if (inputArmSpan.isFocused)
                eventSystem.SetSelectedGameObject(inputHipHeight.gameObject);
            if (inputHipHeight.isFocused)
                eventSystem.SetSelectedGameObject(inputHipWidth.gameObject);
            if (inputHipWidth.isFocused)
                eventSystem.SetSelectedGameObject(inputKneeHeight.gameObject);
            if (inputKneeHeight.isFocused)
                eventSystem.SetSelectedGameObject(inputAnkleHeight.gameObject);
            if (inputAnkleHeight.isFocused)
                eventSystem.SetSelectedGameObject(inputSoleThickness.gameObject);
        }
    }

    void UpdateUIValues()
    {
        inputBodyHeight.text = characterOptions.bodyDimensions.bodyHeight.ToString("F1") + " cm";
        inputFootLength.text = characterOptions.bodyDimensions.footLength.ToString("F1") + " cm";
        inputShoulderHeight.text = characterOptions.bodyDimensions.shoulderHeight.ToString("F1") + " cm";
        inputShoulderWidth.text = characterOptions.bodyDimensions.shoulderWidth.ToString("F1") + " cm";
        inputElbowSpan.text = characterOptions.bodyDimensions.elbowSpan.ToString("F1") + " cm";
        inputWristSpan.text = characterOptions.bodyDimensions.wristSpan.ToString("F1") + " cm";
        inputArmSpan.text = characterOptions.bodyDimensions.armSpan.ToString("F1") + " cm";
        inputHipHeight.text = characterOptions.bodyDimensions.hipHeight.ToString("F1") + " cm";
        inputHipWidth.text = characterOptions.bodyDimensions.hipWidth.ToString("F1") + " cm";
        inputKneeHeight.text = characterOptions.bodyDimensions.kneeHeight.ToString("F1") + " cm";
        inputAnkleHeight.text = characterOptions.bodyDimensions.ankleHeight.ToString("F1") + " cm";
        inputSoleThickness.text = characterOptions.bodyDimensions.extraShowSoleThickness.ToString("F1") + " cm";

        SetTrailSteps(characterOptions.trailSteps);
        SetTrailTime(characterOptions.trailTime);
        toggleUseTrails.isOn = characterOptions.useMotionTrails;

        SetBrushStrokeLength(characterOptions.strokeLength);
        toggleUseBrushTrails.isOn = characterOptions.useBrushStrokes;

        dropdownColorSets.value = characterOptions.colorSet;
        SetUnitDropdownFromValue(characterOptions.unitFactor);

        sliderUpperLeg.value = characterOptions.bodyDimensions.upperLegWidth;
        sliderLowerLeg.value = characterOptions.bodyDimensions.lowerLegWidth;
        sliderUpperArm.value = characterOptions.bodyDimensions.upperArmWidth;
        sliderForeArm.value = characterOptions.bodyDimensions.foreArmWidth;

        sliderBottomSize.value = characterOptions.bodyDimensions.bottomSize;
        sliderBellySize.value = characterOptions.bodyDimensions.bellySize;
        sliderBreastSize.value = characterOptions.bodyDimensions.breastSize;
        sliderChestSize.value = characterOptions.bodyDimensions.chestSize;
        sliderNeckSize.value = characterOptions.bodyDimensions.neckSize;
    }

    public void StartImportDialog()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("FBX", ".fbx"));
        FileBrowser.SetDefaultFilter(".fbx");

        FileBrowser.ShowLoadDialog(OnImportSuccess, OnCancel, FileBrowser.PickMode.Files, false);
    }

    public void StartSaveDialog()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("VRoxel Body Dimensions", ".vrxlbody"));
        FileBrowser.SetDefaultFilter(".vrxlbody");

        FileBrowser.ShowSaveDialog(OnAvatarDimensionSaveSuccess, OnCancel, FileBrowser.PickMode.Files, false);
    }

    public void StartLoadDialog()
    {
        var filter = new FileBrowser.Filter("VRoxel Body Dimensions", ".vrxlbody", ".json");
        FileBrowser.SetFilters(false, filter);

        FileBrowser.ShowLoadDialog(OnAvatarDimensionLoadSuccess, OnCancel, FileBrowser.PickMode.Files, true);
    }

    public void ToggleBodyCustomizer()
    {
        customizationActive = !customizationActive;
        bodyCustomizationPanel.SetActive(customizationActive);
        customizationScene.SetActive(customizationActive);
    }

    void OnImportSuccess(string[] args)
    {
        AnimationIO.ImportMotionCapture(args[0], characterOptions);
        Resume.Invoke();
    }

    void OnAvatarDimensionLoadSuccess(string[] args)
    {
        LoadAvatarDimensions(args[0]);
    }

    void LoadAvatarDimensions(string path)
    {
        characterOptions = JsonConvert.DeserializeObject<CharacterOptions>(File.ReadAllText(path));
        UpdateUIValues();
    }

    void OnAvatarDimensionSaveSuccess(string[] args)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.NullValueHandling = NullValueHandling.Ignore;

        using (StreamWriter sw = new StreamWriter(args[0]))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, characterOptions);
        }

        using (StreamWriter sw = new StreamWriter(Application.persistentDataPath + "\\lastAvatarSettings.json"))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, characterOptions);
        }
    }

    void OnCancel()
    {

    }

    public void SetColorScheme(int item)
    {
        characterOptions.colorSet = item;
    }

    public void SetFileUnits(int item)
    {
        switch (item)
        {
            case 0:
                characterOptions.unitFactor = 1;
                break;
            case 1:
                characterOptions.unitFactor = 0.01f;
                break;
            case 2:
                characterOptions.unitFactor = 0.001f;
                break;
            case 3:
                characterOptions.unitFactor = 0.0254f;
                break;
            case 4:
                characterOptions.unitFactor = 0.3048f;
                break;
        }
    }

    void SetUnitDropdownFromValue(float value)
    {
        float epsilon = 0.00001f;

        if (Mathf.Abs(value - 1) < epsilon)
            dropdownFileUnits.value = 0;
        else if (Mathf.Abs(value - 0.01f) < epsilon)
            dropdownFileUnits.value = 1;
        else if (Mathf.Abs(value - 0.001f) < epsilon)
            dropdownFileUnits.value = 2;
        else if (Mathf.Abs(value - 0.0254f) < epsilon)
            dropdownFileUnits.value = 3;
        else if (Mathf.Abs(value - 0.3048f) < epsilon)
            dropdownFileUnits.value = 4;
    }
    public void SetTrails(bool state)
    {
        characterOptions.useMotionTrails = state;

        sliderTrailSteps.interactable = state;
        sliderTrailTime.interactable = state;
    }

    public void SetTrailSteps(float steps)
    {
        characterOptions.trailSteps = Mathf.RoundToInt(steps);
        labelTrailSteps.text = "Trails:\n" + characterOptions.trailSteps + " steps";
    }

    public void SetTrailTime(float time)
    {
        characterOptions.trailTime = time;
        labelTrailTime.text = "Fade Time:\n" + time.ToString("F1") + " seconds";
    }

    public void SetBrushStrokes(bool state)
    {
        characterOptions.useBrushStrokes = state;

        sliderBrushStrokeLength.interactable = state;
    }

    public void SetBrushStrokeLength(float length)
    {
        characterOptions.strokeLength = length;
        labelBrushStrokeLength.text = "Stroke length;\n" + length.ToString("F1") + " seconds";
    }

    public void SetSimpleDimensions(bool state)
    {
        simpleDimensions = state;

        inputFootLength.interactable = !state;
        inputShoulderHeight.interactable = !state;
        inputShoulderWidth.interactable = !state;
        inputElbowSpan.interactable = !state;
        inputWristSpan.interactable = !state;
        inputArmSpan.interactable = !state;
        inputHipHeight.interactable = !state;
        inputHipWidth.interactable = !state;
        inputKneeHeight.interactable = !state;
        inputAnkleHeight.interactable = !state;
        inputSoleThickness.interactable = !state;

        if (state)
            SetSimpleDimensionValues();
    }

    public void SetBodyHeight(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.bodyHeight = number;

        inputBodyHeight.text = characterOptions.bodyDimensions.bodyHeight.ToString("F1") + " cm";

        if (simpleDimensions)
            SetSimpleDimensionValues();
    }

    public void SetFootLength(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.footLength = number;

        inputFootLength.text = characterOptions.bodyDimensions.footLength.ToString("F1") + " cm";
    }

    public void SetShoulderHeight(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.shoulderHeight = number;

        inputShoulderHeight.text = characterOptions.bodyDimensions.shoulderHeight.ToString("F1") + " cm";
    }

    public void SetShoulderWidth(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.shoulderWidth = number;

        inputShoulderWidth.text = characterOptions.bodyDimensions.shoulderWidth.ToString("F1") + " cm";
    }

    public void SetElbowSpan(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.elbowSpan = number;

        inputElbowSpan.text = characterOptions.bodyDimensions.elbowSpan.ToString("F1") + " cm";
    }

    public void SetWristSpan(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.wristSpan = number;

        inputWristSpan.text = characterOptions.bodyDimensions.wristSpan.ToString("F1") + " cm";
    }

    public void SetArmSpan(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.armSpan = number;

        inputArmSpan.text = characterOptions.bodyDimensions.armSpan.ToString("F1") + " cm";
    }

    public void SetHipHeight(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.hipHeight = number;

        inputHipHeight.text = characterOptions.bodyDimensions.hipHeight.ToString("F1") + " cm";
    }

    public void SetHipWidth(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.hipWidth = number;

        inputHipWidth.text = characterOptions.bodyDimensions.hipWidth.ToString("F1") + " cm";
    }

    public void SetKneeHeight(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.kneeHeight = number;

        inputKneeHeight.text = characterOptions.bodyDimensions.kneeHeight.ToString("F1") + " cm";
    }

    public void SetAnkleHeight(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.ankleHeight = number;

        inputAnkleHeight.text = characterOptions.bodyDimensions.ankleHeight.ToString("F1") + " cm";
    }

    public void SetSoleThickness(string text)
    {
        float number = ParseDimensionString(text);
        if (number > 0)
            characterOptions.bodyDimensions.extraShowSoleThickness = number;

        inputAnkleHeight.text = characterOptions.bodyDimensions.ankleHeight.ToString("F1") + " cm";
    }

    public void SetUpperLegWidth(float value)
    {
        characterOptions.bodyDimensions.upperLegWidth = value;
    }

    public void SetLowerLegWidth(float value)
    {
        characterOptions.bodyDimensions.lowerLegWidth = value;
    }

    public void SetUpperArmWidth(float value)
    {
        characterOptions.bodyDimensions.upperArmWidth = value;
    }

    public void SetForeArmWidth(float value)
    {
        characterOptions.bodyDimensions.foreArmWidth = value;
    }

    public void SetBottomSize(float value)
    {
        characterOptions.bodyDimensions.bottomSize = value;
    }

    public void SetBellySize(float value)
    {
        characterOptions.bodyDimensions.bellySize = value;
    }

    public void SetBreastSize(float value)
    {
        characterOptions.bodyDimensions.breastSize = value;
    }

    public void SetChestSize(float value)
    {
        characterOptions.bodyDimensions.chestSize = value;
    }

    public void SetNeckSize(float value)
    {
        characterOptions.bodyDimensions.neckSize = value;
    }

    void SetSimpleDimensionValues()
    {
        float bodyHeight = characterOptions.bodyDimensions.bodyHeight;

        characterOptions.bodyDimensions.footLength = bodyHeight * 0.14f;
        inputFootLength.text = characterOptions.bodyDimensions.footLength.ToString("F1") + " cm";

        characterOptions.bodyDimensions.shoulderHeight = bodyHeight * 0.845f;
        inputShoulderHeight.text = characterOptions.bodyDimensions.shoulderHeight.ToString("F1") + " cm";

        characterOptions.bodyDimensions.shoulderWidth = bodyHeight * 0.229f;
        inputShoulderWidth.text = characterOptions.bodyDimensions.shoulderWidth.ToString("F1") + " cm";

        characterOptions.bodyDimensions.elbowSpan = bodyHeight * 0.521f;
        inputElbowSpan.text = characterOptions.bodyDimensions.elbowSpan.ToString("F1") + " cm";

        characterOptions.bodyDimensions.wristSpan = bodyHeight * 0.815f;
        inputWristSpan.text = characterOptions.bodyDimensions.wristSpan.ToString("F1") + " cm";

        characterOptions.bodyDimensions.armSpan = bodyHeight * 1.059f;
        inputArmSpan.text = characterOptions.bodyDimensions.armSpan.ToString("F1") + " cm";

        characterOptions.bodyDimensions.hipHeight = bodyHeight * 0.54f;
        inputHipHeight.text = characterOptions.bodyDimensions.hipHeight.ToString("F1") + " cm";

        characterOptions.bodyDimensions.hipWidth = bodyHeight * 0.157f;
        inputHipWidth.text = characterOptions.bodyDimensions.hipWidth.ToString("F1") + " cm";

        characterOptions.bodyDimensions.kneeHeight = bodyHeight * 0.289f;
        inputKneeHeight.text = characterOptions.bodyDimensions.kneeHeight.ToString("F1") + " cm";

        characterOptions.bodyDimensions.ankleHeight = bodyHeight * 0.058f;
        inputAnkleHeight.text = characterOptions.bodyDimensions.ankleHeight.ToString("F1") + " cm";

        characterOptions.bodyDimensions.extraShowSoleThickness = 0;
        inputSoleThickness.text = "0.0 cm";
    }

    float ParseDimensionString(string text)
    {
        text = text.ToLower();
        text = Regex.Replace(text, @"\s+", "");

        float multiplier = 1;

        if (Regex.IsMatch(text, "cm"))
        {
            text = text.Replace("cm", "");
            multiplier = 1;
        }
        else if (Regex.IsMatch(text, "mm"))
        {
            text = text.Replace("mm", "");
            multiplier = 0.1f;
        }
        else if (Regex.IsMatch(text, "m"))
        {
            text = text.Replace("m", "");
            multiplier = 100;
        }
        else if (Regex.IsMatch(text, "\""))
        {
            text = text.Replace("\"", "");
            multiplier = 2.54f;
        }
        else if (Regex.IsMatch(text, "inch"))
        {
            text = text.Replace("inch", "");
            multiplier = 2.54f;
        }
        else if (Regex.IsMatch(text, "\'"))
        {
            text = text.Replace("\'", "");
            multiplier = 30.48f;
        }
        else if (Regex.IsMatch(text, "feet"))
        {
            text = text.Replace("feet", "");
            multiplier = 30.48f;
        }
        else if (Regex.IsMatch(text, "foot"))
        {
            text = text.Replace("foot", "");
            multiplier = 30.48f;
        }
        else
            multiplier = 1;

        string engFloatRegex = @"^[0-9]*(?:\.[0-9]*)?$";
        string gerFloatRegex = @"^[0-9]*(?:\,[0-9]*)?$";

        float number = -1;

        Match engMatch = Regex.Match(text, engFloatRegex);
        Match gerMatch = Regex.Match(text, gerFloatRegex);
        
        if (engMatch.Success)
        {
            CultureInfo culture = new CultureInfo("en-US");
            if (!float.TryParse(engMatch.Value, NumberStyles.Any, culture, out number))
                number = -1;
        }
        else if (gerMatch.Success)
        {
            CultureInfo culture = new CultureInfo("de-DE");
            NumberStyles style = NumberStyles.AllowDecimalPoint;
            if (!float.TryParse(gerMatch.Value, NumberStyles.Any, culture, out number))
                number = -1;
        }
        else
            number = -1;
        

        if (number < 0)
            return number;
        else
            return number * multiplier;
    }
}

[System.Serializable]
public class CharacterOptions
{
    public AvatarDimensions bodyDimensions = new AvatarDimensions();

    [Space]
    public bool useMotionTrails = false;
    public int trailSteps = 5;
    public float trailTime = 10;

    [Space]
    public bool useBrushStrokes = true;
    public float strokeLength = 5;

    [Space]
    public int colorSet = 0;

    [Space]
    public float unitFactor = 1;
}

[System.Serializable]
public class CharacterColorSet
{
    public Material mainColor;
    public Material secondaryColor;
    public Material fadeColor;
    public Material strokeColor;
}