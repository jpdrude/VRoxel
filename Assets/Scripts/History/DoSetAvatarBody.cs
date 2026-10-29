using UnityEngine;

// Body change of an MCP avatar (height, weight, sex, muscularity, measurements); its sequence plays on
public class DoSetAvatarBody : DoAction
{
    GameObject avatar;
    AvatarDimensions oldDimensions;
    AvatarBodies.Parameters oldBody;
    AvatarDimensions newDimensions;
    AvatarBodies.Parameters newBody;

    public DoSetAvatarBody(DoType type, GameObject _avatar, AvatarDimensions _oldDimensions, AvatarBodies.Parameters _oldBody,
        AvatarDimensions _newDimensions, AvatarBodies.Parameters _newBody) : base(type)
    {
        avatar = _avatar;
        oldDimensions = _oldDimensions;
        oldBody = _oldBody;
        newDimensions = _newDimensions;
        newBody = _newBody;
    }

    public override bool Undo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBody(avatar, oldDimensions, oldBody);

        return true;
    }

    public override bool Redo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBody(avatar, newDimensions, newBody);

        return true;
    }
}
