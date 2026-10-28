using UnityEngine;

// Colour change of an MCP avatar; its colour name is how tools and the user refer to it
public class DoSetAvatarColor : DoAction
{
    GameObject avatar;
    string oldColor;
    string newColor;

    public DoSetAvatarColor(DoType type, GameObject _avatar, string _oldColor, string _newColor) : base(type)
    {
        avatar = _avatar;
        oldColor = _oldColor;
        newColor = _newColor;
    }

    public override bool Undo()
    {
        if (avatar == null) return false;

        AvatarRegistry.ApplyColor(avatar, oldColor);

        return true;
    }

    public override bool Redo()
    {
        if (avatar == null) return false;

        AvatarRegistry.ApplyColor(avatar, newColor);

        return true;
    }
}
