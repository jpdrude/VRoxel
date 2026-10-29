using UnityEngine;

// Brush mode change of an MCP avatar (add, remove, off). The blocks it brushes are design edits on the user's UndoStack;
// undoing this only switches the brush back.
public class DoSetAvatarBrush : DoAction
{
    GameObject avatar;
    string oldMode;
    string newMode;

    public DoSetAvatarBrush(DoType type, GameObject _avatar, string _oldMode, string _newMode) : base(type)
    {
        avatar = _avatar;
        oldMode = _oldMode;
        newMode = _newMode;
    }

    public override bool Undo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBrush(avatar, oldMode);

        return true;
    }

    public override bool Redo()
    {
        if (avatar == null) return false;

        AvatarCommands.ApplyBrush(avatar, newMode);

        return true;
    }
}
