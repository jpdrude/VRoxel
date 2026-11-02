using UnityEngine;

// Brush change of an MCP avatar (add, remove, off, and its size). The blocks it brushes are design edits on the user's
// UndoStack; undoing this only switches the brush back.
public class DoSetAvatarBrush : DoAction
{
    GameObject avatar;
    string oldMode;
    float oldSize;
    string newMode;
    float newSize;

    public DoSetAvatarBrush(DoType type, GameObject _avatar, string _oldMode, float _oldSize, string _newMode, float _newSize) : base(type)
    {
        avatar = _avatar;
        oldMode = _oldMode;
        oldSize = _oldSize;
        newMode = _newMode;
        newSize = _newSize;
    }

    public override bool Undo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBrush(avatar, oldMode, oldSize);

        return true;
    }

    public override bool Redo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBrush(avatar, newMode, newSize);

        return true;
    }
}
