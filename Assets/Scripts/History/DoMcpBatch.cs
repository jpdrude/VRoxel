using System.Collections.Generic;

// Several MCP actions undone and redone as one, e.g. loading a saved animation that replaces the scene's avatars
public class DoMcpBatch : DoAction
{
    List<DoAction> actions;

    public DoMcpBatch(DoType type, List<DoAction> _actions) : base(type)
    {
        actions = _actions;
    }

    public override bool Undo()
    {
        bool undone = false;
        for (int i = actions.Count - 1; i >= 0; i--)
            undone |= actions[i].Undo();

        return undone;
    }

    public override bool Redo()
    {
        bool redone = false;
        foreach (DoAction action in actions)
            redone |= action.Redo();

        return redone;
    }
}
