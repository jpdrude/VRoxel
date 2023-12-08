using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoStackVisualizer : MonoBehaviour
{
    [SerializeField]
    List<int> undoSteps = new List<int>();

    [SerializeField]
    List<int> redoSteps = new List<int>();

    // Update is called once per frame
    void Update()
    {
        undoSteps.Clear();
        redoSteps.Clear();

        foreach (DoAction action in UndoStack.GetUndoStack())
        {
            if (action.Type == DoAction.DoType.AddStrocke)
            {
                undoSteps.Add(((DoAddStroke)action).StrokeLength);
            }
            else if (action.Type == DoAction.DoType.RemoveStroke)
            {
                undoSteps.Add(((DoRemoveStroke)action).StrokeLength);
            }
        }

        foreach (DoAction action in UndoStack.GetRedoStack())
        {
            if (action.Type == DoAction.DoType.AddStrocke)
            {
                redoSteps.Add(((DoAddStroke)action).StrokeLength);
            }
            else if (action.Type == DoAction.DoType.RemoveStroke)
            {
                redoSteps.Add(((DoRemoveStroke)action).StrokeLength);
            }
        }
    }
}
