using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Named colours for MCP avatars, so the user and the LLM can tell them apart ("the teal one").
// The first five are VRoxel's character colour sets (Animation/Characters/Material); the rest follow their rule:
// joints keep the body's hue at about half its brightness, strokes are about 15 % brighter.
public static class AvatarColors
{
    public class Entry
    {
        public string name;
        public Color body;
        public Color joints;
        public Color stroke;
    }

    public static readonly Entry[] Palette =
    {
        new Entry { name = "red", body = new Color(0.737f, 0.298f, 0.318f), joints = new Color(0.430f, 0.198f, 0.208f), stroke = new Color(0.802f, 0.391f, 0.409f) },
        new Entry { name = "blue", body = new Color(0.271f, 0.482f, 0.616f), joints = new Color(0.112f, 0.200f, 0.255f), stroke = new Color(0.343f, 0.611f, 0.780f) },
        new Entry { name = "mint", body = new Color(0.706f, 0.769f, 0.682f), joints = new Color(0.345f, 0.376f, 0.334f), stroke = new Color(0.815f, 0.887f, 0.787f) },
        new Entry { name = "canary", body = new Color(0.898f, 0.910f, 0.714f), joints = new Color(0.534f, 0.541f, 0.424f), stroke = new Color(0.987f, 1.000f, 0.784f) },
        new Entry { name = "pink", body = new Color(0.847f, 0.510f, 0.616f), joints = new Color(0.426f, 0.256f, 0.310f), stroke = new Color(0.986f, 0.593f, 0.717f) },
        new Entry { name = "orange", body = new Color(0.816f, 0.541f, 0.369f), joints = new Color(0.424f, 0.289f, 0.203f), stroke = new Color(0.938f, 0.638f, 0.450f) },
        new Entry { name = "green", body = new Color(0.514f, 0.635f, 0.443f), joints = new Color(0.270f, 0.330f, 0.235f), stroke = new Color(0.598f, 0.731f, 0.521f) },
        new Entry { name = "purple", body = new Color(0.561f, 0.490f, 0.690f), joints = new Color(0.295f, 0.260f, 0.359f), stroke = new Color(0.652f, 0.575f, 0.794f) },
        new Entry { name = "teal", body = new Color(0.298f, 0.580f, 0.565f), joints = new Color(0.162f, 0.302f, 0.294f), stroke = new Color(0.359f, 0.667f, 0.650f) },
        new Entry { name = "sand", body = new Color(0.718f, 0.608f, 0.478f), joints = new Color(0.373f, 0.319f, 0.255f), stroke = new Color(0.825f, 0.705f, 0.564f) },
    };

    public static IEnumerable<string> Names { get { return Palette.Select(entry => entry.name); } }

    public static Entry Find(string name)
    {
        return Palette.FirstOrDefault(entry => string.Equals(entry.name, name?.Trim(), System.StringComparison.OrdinalIgnoreCase));
    }

    // A random colour no other avatar wears; once all are taken, any random one
    public static Entry PickUnused(ICollection<string> used)
    {
        List<Entry> free = Palette.Where(entry => !used.Contains(entry.name)).ToList();
        List<Entry> choices = free.Count > 0 ? free : Palette.ToList();
        return choices[Random.Range(0, choices.Count)];
    }

    // Copies of the template's materials in this colour, so shaders and fade / stroke setup stay those of the colour sets
    public static CharacterColorSet Build(CharacterColorSet template, Entry color)
    {
        return new CharacterColorSet
        {
            mainColor = Tint(template.mainColor, color.body),
            secondaryColor = Tint(template.secondaryColor, color.joints),
            fadeColor = Tint(template.fadeColor, color.body),
            strokeColor = Tint(template.strokeColor, color.stroke)
        };
    }

    static Material Tint(Material template, Color color)
    {
        if (template == null) return null;

        Material material = new Material(template);
        if (!material.HasProperty("_Color")) return material;

        // Fade materials are alpha blended; keep their transparency
        color.a = template.color.a;
        material.color = color;
        return material;
    }
}
