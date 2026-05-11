using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;

public static class ExtensionMethods
{
    private static readonly Regex EngFloatRegex = new Regex(@"^[-+]?[0-9]*(?:\.[0-9]*)?$");
    private static readonly Regex GerFloatRegex = new Regex(@"^[-+]?[0-9]*(?:\,[0-9]*)?$");
    private static readonly CultureInfo EnCulture = new CultureInfo("en-US");
    private static readonly CultureInfo DeCulture = new CultureInfo("de-DE");

    public static Vector3 Divide(this Vector3Int vec, float div)
    {
        return new Vector3(vec.x / div, vec.y / div, vec.z / div);
    }

    public static float? ParseStringToFloat(this string text)
    {
        Match engMatch = EngFloatRegex.Match(text);
        Match gerMatch = GerFloatRegex.Match(text);

        float? result = null;

        if (engMatch.Success)
        {
            float number = 0;
            if (!float.TryParse(engMatch.Value, NumberStyles.Any, EnCulture, out number))
                result = null;
            else
                result = number;
        }
        else if (gerMatch.Success)
        {
            float number = 0;
            if (!float.TryParse(gerMatch.Value, NumberStyles.Any, DeCulture, out number))
                result = null;
            else
                result = number;
        }

        return result;
    }
}
