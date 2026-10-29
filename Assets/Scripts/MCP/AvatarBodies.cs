using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Body shapes of MCP avatars from height, weight, sex, muscularity, measurements or a body file of the character editor.
// The model is gender neutral with a BMI of 22. The shape presets in Resources/BodyPresets (.vrxlbody files saved from
// the character editor in simple-dimensions mode) each change it in one respect. An avatar's shape is the model plus
// each preset's change, weighted like blend shapes: sex blends in masculine or feminine, the BMI lean or heavy, and
// muscularity muscular.
public static class AvatarBodies
{
    public const string Extension = ".vrxlbody";
    const string PresetFolder = "BodyPresets/";
    const string Masculine = "shape_masculine";
    const string Feminine = "shape_feminine";
    const string Lean = "shape_lean_bmi18";
    const string Heavy = "shape_heavy_bmi35";
    const string Muscular = "shape_muscular_athletic";

    // BMI the neutral model stands for, and those of the slim and heavy presets
    const float NeutralBmi = 22;
    const float SlimBmi = 18;
    const float HeavyBmi = 35;
    // BMI points full muscularity (a heavy weightlifter) accounts for, so muscle doesn't also read as fat
    const float MuscleBmi = 8;
    // Average adult heights (cm), the default when only the sex is given; without it the avatar is the 180.7 cm model
    const float MaleHeight = 179;
    const float FemaleHeight = 166;
    // Presets apply up to this much beyond their own shape for very low or high BMIs
    const float MaxPresetWeight = 1.25f;

    // Shape fields the presets change: width multipliers (neutral 1) and blend shape sizes (neutral 0)
    static readonly string[] shapeFields =
    {
        "upperLegWidth", "lowerLegWidth", "upperArmWidth", "foreArmWidth",
        "bottomSize", "bellySize", "breastSize", "chestSize", "neckSize",
    };
    // Widths and spans the presets change relative to body height, measured against the model's own (a masculine body has
    // broader shoulders and longer arms); the heights of hips, knees and shoulders come from the height and measurements only
    static readonly string[] widthRatios = { "hipWidth", "shoulderWidth", "elbowSpan", "wristSpan", "armSpan" };
    // Lengths in cm that can be given as measurements; the rest follow from the body height
    static readonly string[] lengthFields =
    {
        "bodyHeight", "footLength", "shoulderHeight", "shoulderWidth", "elbowSpan", "wristSpan", "armSpan",
        "hipHeight", "hipWidth", "kneeHeight", "ankleHeight", "extraShowSoleThickness",
    };

    [Serializable]
    public class Parameters
    {
        public float? heightCm;
        public float? weightKg;
        // 0 male, 0.5 neutral (the model), 1 female; null is neutral
        public float? sex;
        public float muscularity;
        public string bodyFile;
        public Dictionary<string, float> measurements = new Dictionary<string, float>();

        public Parameters Copy()
        {
            Parameters copy = (Parameters)MemberwiseClone();
            copy.measurements = new Dictionary<string, float>(measurements);
            return copy;
        }
    }

    static readonly Dictionary<GameObject, Parameters> parameters = new Dictionary<GameObject, Parameters>();

    public static string Folder
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRoxel", "Bodies"); }
    }

    public static Parameters Get(GameObject avatar)
    {
        Parameters body;
        return avatar != null && parameters.TryGetValue(avatar, out body) ? body : null;
    }

    public static void Remember(GameObject avatar, Parameters body)
    {
        if (body != null)
            parameters[avatar] = body.Copy();
    }

    // Reads the body arguments of spawn_avatar / set_avatar_body on top of an avatar's current ones (null for a new avatar)
    public static Parameters Read(JObject args, Parameters current)
    {
        Parameters body = current != null ? current.Copy() : new Parameters();

        if (Given(args, "height_cm"))
            body.heightCm = InRange((float)args["height_cm"], 50, 250, "height_cm");
        if (Given(args, "weight_kg"))
            body.weightKg = InRange((float)args["weight_kg"], 20, 300, "weight_kg");
        if (Given(args, "sex"))
            body.sex = ReadSex(args["sex"]);
        if (Given(args, "muscularity"))
            body.muscularity = InRange((float)args["muscularity"], 0, 1, "muscularity");
        if (Given(args, "body_file"))
            body.bodyFile = string.IsNullOrWhiteSpace((string)args["body_file"]) ? null : (string)args["body_file"];

        if (args["measurements"] is JObject measurements)
            foreach (JProperty measurement in measurements.Properties())
            {
                if (!lengthFields.Contains(measurement.Name))
                    throw new McpException("Unknown measurement " + measurement.Name + ". Measurements (cm): " + string.Join(", ", lengthFields));
                if (measurement.Value.Type == JTokenType.Null)
                    body.measurements.Remove(measurement.Name);
                else
                    body.measurements[measurement.Name] = InRange((float)measurement.Value, 0, 300, measurement.Name);
            }

        return body;
    }

    // The dimensions CharacterModel builds the avatar from
    public static AvatarDimensions Build(Parameters body, AvatarDimensions native)
    {
        AvatarDimensions dimensions;
        if (body.bodyFile != null)
        {
            dimensions = LoadFile(body.bodyFile);
            if (body.heightCm.HasValue)
                ScaleLengths(dimensions, body.heightCm.Value / dimensions.bodyHeight);
        }
        else if (body.heightCm.HasValue || body.sex.HasValue)
        {
            dimensions = new AvatarDimensions { bodyHeight = body.heightCm ?? Mathf.Lerp(MaleHeight, FemaleHeight, body.sex.Value) };
            dimensions.SetProportionsFromBodyHeight();
        }
        else
            dimensions = Copy(native);

        ApplyShape(dimensions, body, native);

        foreach (KeyValuePair<string, float> measurement in body.measurements)
            Field(measurement.Key).SetValue(dimensions, measurement.Value);

        return dimensions;
    }

    public static float? Bmi(Parameters body, AvatarDimensions dimensions)
    {
        if (!body.weightKg.HasValue) return null;

        float meters = dimensions.bodyHeight / 100;
        return body.weightKg.Value / (meters * meters);
    }

    static void ApplyShape(AvatarDimensions dimensions, Parameters body, AvatarDimensions native)
    {
        float sex = body.sex ?? 0.5f;
        List<(string preset, float weight)> weights = new List<(string, float)>
        {
            (Masculine, Mathf.Clamp01(1 - 2 * sex)),
            (Feminine, Mathf.Clamp01(2 * sex - 1)),
            (Muscular, body.muscularity),
        };

        float? bmi = Bmi(body, dimensions);
        if (bmi.HasValue)
        {
            float fat = bmi.Value - body.muscularity * MuscleBmi;
            // The lean shape has little fat and little muscle (thin limbs); a muscular body only shares its low fat
            if (fat < NeutralBmi)
                weights.Add((Lean, Mathf.Min((NeutralBmi - fat) / (NeutralBmi - SlimBmi), MaxPresetWeight, 1 - body.muscularity)));
            else
                weights.Add((Heavy, Mathf.Min((fat - NeutralBmi) / (HeavyBmi - NeutralBmi), MaxPresetWeight)));
        }

        foreach (var (name, weight) in weights.Where(w => w.weight > 1e-3f))
        {
            AvatarDimensions preset = Preset(name);

            foreach (string field in shapeFields)
            {
                FieldInfo info = Field(field);
                float neutral = field.EndsWith("Width") ? 1 : 0;
                info.SetValue(dimensions, (float)info.GetValue(dimensions) + weight * ((float)info.GetValue(preset) - neutral));
            }

            foreach (string field in widthRatios)
            {
                FieldInfo info = Field(field);
                float change = (float)info.GetValue(preset) / preset.bodyHeight - (float)info.GetValue(native) / native.bodyHeight;
                info.SetValue(dimensions, (float)info.GetValue(dimensions) + weight * change * dimensions.bodyHeight);
            }
        }
    }

    // Read on every use, so presets edited during Play mode apply to the next body built
    static AvatarDimensions Preset(string name)
    {
        TextAsset file = Resources.Load<TextAsset>(PresetFolder + name);
        if (file == null)
            throw new McpException("The body shape preset " + name + " is missing (Resources/" + PresetFolder + name + ".json), so " +
                (name == Masculine || name == Feminine ? "sex" : name == Muscular ? "muscularity" : "weight_kg") + " can't be applied");

        return ParseBody(file.text, name);
    }

    // A body file saved by the character editor: a path, or a name in the Bodies folder
    static AvatarDimensions LoadFile(string reference)
    {
        string path = File.Exists(reference) ? reference : Path.Combine(Folder, reference + (reference.EndsWith(Extension) ? "" : Extension));
        if (!File.Exists(path))
        {
            string[] names = Directory.Exists(Folder) ? Directory.GetFiles(Folder, "*" + Extension).Select(Path.GetFileNameWithoutExtension).ToArray() : new string[0];
            throw new McpException("No body file " + reference + ". Give a path, or a name in " + Folder +
                (names.Length > 0 ? ": " + string.Join(", ", names) : " (empty)"));
        }

        return ParseBody(File.ReadAllText(path), reference);
    }

    // The character editor saves CharacterOptions, whose bodyDimensions hold the body
    static AvatarDimensions ParseBody(string json, string name)
    {
        try
        {
            JObject options = JObject.Parse(json);
            AvatarDimensions dimensions = (options["bodyDimensions"] ?? options).ToObject<AvatarDimensions>();
            if (dimensions.bodyHeight <= 0)
                throw new McpException("Body " + name + " has no body height");
            return dimensions;
        }
        catch (Newtonsoft.Json.JsonException e)
        {
            throw new McpException("Body " + name + " is not a body file of the character editor: " + e.Message);
        }
    }

    static void ScaleLengths(AvatarDimensions dimensions, float factor)
    {
        foreach (string field in lengthFields)
        {
            FieldInfo info = Field(field);
            info.SetValue(dimensions, (float)info.GetValue(dimensions) * factor);
        }
    }

    public static AvatarDimensions Copy(AvatarDimensions dimensions)
    {
        return JsonUtility.FromJson<AvatarDimensions>(JsonUtility.ToJson(dimensions));
    }

    static FieldInfo Field(string name)
    {
        return typeof(AvatarDimensions).GetField(name);
    }

    // The Python server sends options left out as null
    static bool Given(JObject args, string key)
    {
        return args[key] != null && args[key].Type != JTokenType.Null;
    }

    static float ReadSex(JToken token)
    {
        if (token.Type == JTokenType.String)
        {
            string text = ((string)token).Trim().ToLowerInvariant();
            if (text == "male") return 0;
            if (text == "neutral") return 0.5f;
            if (text == "female") return 1;
            throw new McpException("sex must be 'male', 'neutral', 'female' or a number from 0 (male) over 0.5 (neutral) to 1 (female)");
        }

        return InRange((float)token, 0, 1, "sex");
    }

    static float InRange(float value, float min, float max, string name)
    {
        if (value < min || value > max)
            throw new McpException(name + " must be between " + min + " and " + max);
        return value;
    }
}
