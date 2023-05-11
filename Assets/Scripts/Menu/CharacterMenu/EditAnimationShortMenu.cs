using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Globalization;
using Valve.Newtonsoft.Json;
using SimpleFileBrowser;
using System.IO;
using UnityEngine.UI;

public class EditAnimationShortMenu : MonoBehaviour
{
    [Header("Character Model")]
    [SerializeField]
    GameObject characterObject;

    [SerializeField]
    CharacterModel animationCharacter;

    [SerializeField]
    CustomAnimationPlayer animationPlayer;

    [SerializeField]
    CharacterSpawner characterSpawner;

    [Space]
    [Header("UI Elements")]
    [SerializeField]
    TMP_InputField inputName;
    [Space]
    [Header("Transform Offset")]
    [SerializeField]
    TMP_InputField inputX;
    float offsetX;
    [SerializeField]
    TMP_InputField inputY;
    float offsetY;
    [SerializeField]
    TMP_InputField inputZ;
    float offsetZ;
    [SerializeField]
    TMP_InputField inputRot;
    float offsetRot;

    [Space]
    [Header("Trim Animation")]
    [SerializeField]
    TMP_InputField inputStart;
    [SerializeField]
    TMP_InputField inputEnd;

    [Space]
    [Header("Trails")]
    [SerializeField]
    Toggle toggleStroke;
    [SerializeField]
    Toggle toggleShadow;
    [SerializeField]
    TMP_InputField inputFadeSteps;
    [SerializeField]
    TMP_InputField inputFadeTime;
    [SerializeField]
    TMP_InputField inputStrokeLength;

    [Space]
    [Header("Avatar Color")]
    [SerializeField]
    TMP_Dropdown dropdownColor;

    [Space]
    [Header("Speed")]
    [SerializeField]
    TMP_InputField inputSpeed;

    UnityEngine.EventSystems.EventSystem eventSystem;

    private void OnEnable()
    {
        UpdateTransformOffset();
        UpdateTrim();
        UpdateTrails();
        UpdateSpeed();
        UpdateColorSet();
    }

    private void Update()
    {
        CheckFocusChange();
    }

    private void Start()
    {
        eventSystem = UnityEngine.EventSystems.EventSystem.current;
    }

    void CheckFocusChange()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputName.isFocused)
                eventSystem.SetSelectedGameObject(inputX.gameObject);
            if (inputX.isFocused)
                eventSystem.SetSelectedGameObject(inputY.gameObject);
            if (inputY.isFocused)
                eventSystem.SetSelectedGameObject(inputZ.gameObject);
            if (inputZ.isFocused)
                eventSystem.SetSelectedGameObject(inputRot.gameObject);
            if (inputRot.isFocused)
                eventSystem.SetSelectedGameObject(inputStart.gameObject);
            if (inputStart.isFocused)
                eventSystem.SetSelectedGameObject(inputEnd.gameObject);
            if (inputEnd.isFocused)
                eventSystem.SetSelectedGameObject(inputFadeSteps.gameObject);
            if (inputFadeSteps.isFocused)
                eventSystem.SetSelectedGameObject(inputFadeTime.gameObject);
            if (inputFadeTime.isFocused)
                eventSystem.SetSelectedGameObject(inputSpeed.gameObject);
            if (inputSpeed.isFocused)
                eventSystem.SetSelectedGameObject(inputName.gameObject);
        }
    }

    public void SetAnimationCharacter(GameObject _characterObject)
    {
        characterObject = _characterObject;
        animationCharacter = characterObject.GetComponent<CharacterModel>();
        animationPlayer = characterObject.GetComponent<CustomAnimationPlayer>();
        characterSpawner = characterObject.GetComponent<CharacterSpawner>();

        inputName.text = characterObject.name;

        UpdateTransformOffset();
        UpdateTrim();
        UpdateTrails();
        UpdateSpeed();
        UpdateColorSet();
    }

    void UpdateTransformOffset()
    {
        offsetX = animationPlayer.PositionOffset.x;
        inputX.text = offsetX.ToString("F2");

        offsetY = animationPlayer.PositionOffset.y;
        inputY.text = offsetY.ToString("F2");

        offsetZ = animationPlayer.PositionOffset.z;
        inputZ.text = offsetZ.ToString("F2");

        offsetRot = animationPlayer.RotationOffset;
        inputRot.text = offsetRot.ToString("F0");
    }

    void UpdateTrim()
    {
        inputStart.text = animationPlayer.AnimationStart.ToString("F1");

        inputEnd.text = animationPlayer.AnimationEnd.ToString("F1");
    }

    void UpdateTrails()
    {
        toggleShadow.isOn = animationPlayer.ShadowTrails;

        toggleStroke.isOn = animationCharacter.StrokeTrails;

        inputFadeSteps.text = animationPlayer.FadeSteps.ToString();

        inputFadeTime.text = animationPlayer.FadeTime.ToString("F1");

        inputStrokeLength.text = animationCharacter.StrokeLength.ToString("F1");
    }

    void UpdateSpeed()
    {
        inputSpeed.text = animationPlayer.Speed.ToString("F2");
    }

    void UpdateColorSet()
    {
        dropdownColor.value = CharacterEditor.ColorSets.IndexOf(animationCharacter.CurrentColorSet);
    }

    public void SetVisibility(bool value)
    {
        characterObject.gameObject.SetActive(value);
    }

    public void SetName(string value)
    {
        characterObject.name = value;
    }

    public void SetXOffset(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputX.text = offsetX.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - offsetX) > 0.01)
        {
            offsetX = (float)value;
            inputX.text = offsetX.ToString("F2");

            Vector3 offset = animationPlayer.PositionOffset;
            offset.x = offsetX;
            animationPlayer.PositionOffset = offset;
        }
    }

    public void SetYOffset(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputY.text = offsetY.ToString("F2");
            return;
        }
        if (Mathf.Abs((float)value - offsetY) > 0.01)
        {
            offsetY = (float)value;
            inputY.text = offsetY.ToString("F2");

            Vector3 offset = animationPlayer.PositionOffset;
            offset.y = offsetY;
            animationPlayer.PositionOffset = offset;
        }
    }

    public void SetZOffset(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputZ.text = offsetZ.ToString("F2");
            return;
        }

        if (Mathf.Abs((float)value - offsetZ) > 0.01)
        {
            offsetZ = (float)value;
            inputZ.text = offsetZ.ToString("F2");

            Vector3 offset = animationPlayer.PositionOffset;
            offset.z = offsetZ;
            animationPlayer.PositionOffset = offset;
        }
    }

    public void SetRotOffset(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputRot.text = offsetX.ToString("F0");
            return;
        }

        if (Mathf.Abs((float)value - offsetRot) > 1)
        {
            offsetRot = (float)value;
            inputRot.text = offsetRot.ToString("F0");

            animationPlayer.RotationOffset = offsetRot;
        }
    }

    public void SetAnimationStart(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputStart.text = animationPlayer.AnimationStart.ToString("F1");
            return;
        }

        animationPlayer.AnimationStart = (float)value;
        UpdateTrim();
    }

    public void SetAnimationEnd(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputEnd.text = animationPlayer.AnimationEnd.ToString("F1");
            return;
        }

        animationPlayer.AnimationEnd = (float)value;
        UpdateTrim();
    }

    public void SetShadowTrails(bool value)
    {
        animationPlayer.ShadowTrails = value;
    }

    public void SetStrokeTrails(bool value)
    {
        animationCharacter.SetTrails(value, animationCharacter.StrokeLength);
    }

    public void SetStrokeLength(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputStrokeLength.text = animationCharacter.StrokeLength.ToString("F1");
            return;
        }

        animationCharacter.SetTrails(animationCharacter.StrokeTrails, (float)value);
        UpdateTrails();
    }

    public void SetFadeSteps(string textValue)
    {
        int value;
        try
        {
            value = int.Parse(textValue);
        }
        catch
        {
            inputFadeSteps.text = animationPlayer.FadeSteps.ToString();
            return;
        }

        animationPlayer.FadeSteps = value;
        UpdateTrails();
    }

    public void SetFadeTime(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputFadeTime.text = animationPlayer.FadeTime.ToString("F1");
            return;
        }

        animationPlayer.FadeTime = (float)value;
        UpdateTrails();
    }

    public void SetColorSet(int value)
    {
        animationCharacter.SetColor(CharacterEditor.ColorSets[value]);
    }

    public void SetSpeed(string textValue)
    {
        float? value = ParseStringToFloat(textValue);
        if (value == null)
        {
            inputSpeed.text = animationPlayer.Speed.ToString("F2");
            return;
        }

        animationPlayer.Speed = (float)value;
        UpdateSpeed();
    }

    public void SaveAnimationOptions()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("VRoxel Mocap Settings", ".vrxlmco"));
        FileBrowser.SetDefaultFilter(".vrxlmco");

        FileBrowser.ShowSaveDialog(SaveAnimationOptions, OnCancel, FileBrowser.PickMode.Files, false);
    }

    public void LoadAnimationOptions()
    {
        var filter = new FileBrowser.Filter("VRoxel Mocap Settings", ".vrxlmco");
        FileBrowser.SetFilters(false, filter);

        FileBrowser.ShowLoadDialog(LoadAnimationOptions, OnCancel, FileBrowser.PickMode.Files, true);
    }

    public void DeleteAnimation()
    {
        Destroy(characterObject);
        Destroy(gameObject);
    }

    public void PauseAnimation()
    {
        animationPlayer.PauseAnimationState = !animationPlayer.PauseAnimationState;
    }

    void OnCancel()
    {

    }

    void SaveAnimationOptions(string[] paths)
    {
        AnimationOptions options = new AnimationOptions();

        options.animationName = inputName.text;

        options.offsetX = animationPlayer.PositionOffset.x;
        options.offsetY = animationPlayer.PositionOffset.y;
        options.offsetZ = animationPlayer.PositionOffset.z;

        options.offsetRot = animationPlayer.RotationOffset;

        options.startTime = animationPlayer.AnimationStart;
        options.endTime = animationPlayer.AnimationEnd;

        options.useShadowTrails = animationPlayer.ShadowTrails;
        options.useStrokeTrails = animationCharacter.StrokeTrails;

        options.fadeSteps = animationPlayer.FadeSteps;
        options.fadeTime = animationPlayer.FadeTime;
        options.strokeLength = animationCharacter.StrokeLength;

        options.speed = animationPlayer.Speed;

        options.color = CharacterEditor.ColorSets.IndexOf(animationCharacter.CurrentColorSet);

        JsonSerializer serializer = new JsonSerializer();
        serializer.NullValueHandling = NullValueHandling.Ignore;

        using (StreamWriter sw = new StreamWriter(paths[0]))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, options);
        }
    }

    void LoadAnimationOptions(string[] paths)
    {
        if (!File.Exists(paths[0])) return;

        AnimationOptions options = JsonConvert.DeserializeObject<AnimationOptions>(File.ReadAllText(paths[0]));

        animationPlayer.PositionOffset = new Vector3(options.offsetX, options.offsetY, options.offsetZ);
        animationPlayer.RotationOffset = options.offsetRot;
        UpdateTransformOffset();

        animationPlayer.AnimationStart = options.startTime;
        animationPlayer.AnimationEnd = options.endTime;
        UpdateTrim();

        animationCharacter.SetTrails(options.useStrokeTrails, options.strokeLength);
        animationPlayer.FadeSteps = options.fadeSteps;
        animationPlayer.FadeTime = options.fadeTime;
        animationPlayer.ShadowTrails = options.useShadowTrails;
        UpdateTrails();

        animationPlayer.Speed = options.speed;
        UpdateSpeed();

        inputName.text = options.animationName;
        SetName(options.animationName);

        SetColorSet(options.color);
        dropdownColor.value = options.color;
    }

    public void ToggleAddCharacterBrush()
    {
        if (!characterSpawner.AddBlocks)
            characterSpawner.AddCharacterBrush();
        else
            characterSpawner.DisableCharacterBrush();
    }

    public void ToggleRemvoeCharacterBrush()
    {
        if (!characterSpawner.RemoveBlocks)
            characterSpawner.RemoveCharacterBrush();
        else
            characterSpawner.DisableCharacterBrush();
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

    private void OnDisable()
    {
        characterSpawner.DisableCharacterBrush();
    }
}

public class AnimationOptions
{
    public string animationName;

    public float offsetX;
    public float offsetY;
    public float offsetZ;

    public float offsetRot;

    public float startTime;
    public float endTime;

    public bool useStrokeTrails;
    public bool useShadowTrails;
    public int fadeSteps;
    public float fadeTime;
    public float strokeLength;

    public float speed;

    public int color;
}