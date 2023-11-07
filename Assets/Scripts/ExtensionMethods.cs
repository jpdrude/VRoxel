using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;

public static class ExtensionMethods
{
    public static Vector3 Divide(this Vector3Int vec, float div)
    {
        return new Vector3(vec.x / div, vec.y / div, vec.z / div);
    }

    public static float? ParseStringToFloat(this string text)
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
