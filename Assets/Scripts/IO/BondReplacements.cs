using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BondReplacements
{
    static Dictionary<string, string> bondReplacements = new Dictionary<string, string>()
    {
        {"StackedGIK_2mm", "StackedGIK_2cm"},
        {"StackedGIK_4mm",  "StackedGIK_4cm"},
        {"StackedGIK_8mm", "StackedGIK_8cm"},
        {"David_Bond_Blocks", "DoubleH_Cube_8x8x16"},
        {"David_Bond_Checkerboard", "DoubleH_Checkerboard_8x8x16"},
        {"David_Bond_Full", "DoubleH_Full_8x8x16"},
        {"David_Bond_Horizontal",  "DoubleH_Horizontal_8x8x16"},
        {"Leander_Bond_Alternating", "Favel_Alternating"},
        {"Leander_Bond_Stacked", "Favel_Stacked"},
        {"Ella_6cm", "inter(b)lock_6cm_black"},
        {"Ella_6cm_white", "inter(b)lock_6cm_white"},
        {"Ella_18cm", "inter(b)lock_18cm_black"},
        {"Ella_18cm_white", "inter(b)lock_18cm_white"},
        {"Akro_Bond04", "Akro_BondBasic" },
        {"Akro_Bond01", "Akro_BondBrutalist" },
        {"Akro_Bond02", "Akro_BondReef" },
        {"Akro_Bond03", "Akro_BondElephant" }
    };

    public static bool CheckBondReplacement(ref string loadBond)
    {
        if (bondReplacements.ContainsKey(loadBond))
        {
            string newBondName = bondReplacements[loadBond];
            loadBond = newBondName;
            GameManager.Manager.SetBond(GameManager.AvailableBonds[newBondName]);
            return true;
        }

        return false;
    }
}
