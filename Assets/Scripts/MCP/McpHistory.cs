using System.Collections.Generic;

// Undo history for actions made through MCP, separate from the user's UndoStack.
// Only the MCP undo / redo tools move through it.
public static class McpHistory
{
    static readonly CappedStack<DoAction> undoStack = new CappedStack<DoAction>(50);
    static readonly CappedStack<DoAction> redoStack = new CappedStack<DoAction>(50);

    public static int UndoCount { get { return undoStack.Count; } }
    public static int RedoCount { get { return redoStack.Count; } }

    public static void Push(DoAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
    }

    // Returns the undone action, or null if there was nothing to undo
    public static DoAction Undo()
    {
        // Actions whose objects were destroyed elsewhere fail; skip them
        while (undoStack.Count > 0)
        {
            DoAction action = undoStack.Pop();
            if (action != null && action.Undo())
            {
                redoStack.Push(action);
                return action;
            }
        }

        return null;
    }

    public static DoAction Redo()
    {
        while (redoStack.Count > 0)
        {
            DoAction action = redoStack.Pop();
            if (action != null && action.Redo())
            {
                undoStack.Push(action);
                return action;
            }
        }

        return null;
    }

    public static List<string> Describe()
    {
        List<string> entries = new List<string>();
        foreach (DoAction action in undoStack)
            entries.Add(action.Type.ToString());

        return entries;
    }
}
