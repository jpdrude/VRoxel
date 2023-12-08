using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class UndoStack
{
    static CappedStack<DoAction> undoStack = new CappedStack<DoAction>(20);
    static CappedStack<DoAction> redoStack = new CappedStack<DoAction>(20);

    public static UnityEvent UndoStackUnavailable = new UnityEvent();
    public static UnityEvent RedoStackUnavailable = new UnityEvent();

    public static UnityEvent UndoStackAvailable = new UnityEvent();
    public static UnityEvent RedoStackAvailable = new UnityEvent();

    public static void Push(DoAction doAction)
    {
        PushUndo(doAction);

        if (redoStack.Count > 0)
        {
            redoStack.Clear();
            RedoStackUnavailable.Invoke();
        }
    }

    public static void Undo()
    {
        if (undoStack.Count <= 0) return;

        DoAction undoAction = undoStack.Pop();

        if (undoAction == null)
        {
            Debug.LogError("Undo Action is null");
            
            Undo();
            return;
        }

        bool result = undoAction.Undo();

        if (result)
            PushRedo(undoAction);

        if (undoStack.Count == 0)
            UndoStackUnavailable.Invoke();
    }

    public static void Redo()
    {
        if (redoStack.Count <= 0) return;

        DoAction redoAction = redoStack.Pop();

        bool result = redoAction.Redo();

        if (result)
            PushUndo(redoAction);

        if (redoStack.Count == 0)
            RedoStackUnavailable.Invoke();
    }

    public static void Clear()
    {
        redoStack.Clear();
        undoStack.Clear();

        UndoStackUnavailable.Invoke();
        RedoStackUnavailable.Invoke();
    }

    static void PushUndo(DoAction action)
    {
        undoStack.Push(action);

        if (undoStack.Count == 1)
            UndoStackAvailable.Invoke();
    }

    static void PushRedo(DoAction action)
    {
        redoStack.Push(action);

        if (redoStack.Count == 1)
            RedoStackAvailable.Invoke();
    }

    public static CappedStack<DoAction> GetUndoStack()
    {
        return undoStack;
    }

    public static CappedStack<DoAction> GetRedoStack()
    {
        return redoStack;
    }
}

public class CappedStack<T> : LinkedList<T>
{
    readonly int size;
    public CappedStack(int _size)
    {
        size = _size;
    }

    public void Push(T item)
    {
        AddFirst(item);

        while(Count > size)
        {
            RemoveLast();
        }
    }

    public T Pop()
    {
        if (Count > 0)
        {
            var pop = First.Value;
            RemoveFirst();

            return pop;
        }

        return default(T);
    }
}