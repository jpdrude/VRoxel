using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoChangeObjectPos : DoAction
{
    GameObject gameObject;
    Matrix4x4 oldTransform;
    Matrix4x4 newTransform;

    public DoChangeObjectPos(DoType type, GameObject _gameObject, Matrix4x4 _oldTransform, Matrix4x4 _newTransform) : base(type)
    {
        gameObject = _gameObject;
        oldTransform = _oldTransform;
        newTransform = _newTransform;
    }

    public DoChangeObjectPos(DoType type, GameObject _gameObject, Transform _oldTransform, Transform _newTransform) : base(type)
    {
        gameObject = _gameObject;
        oldTransform = Matrix4x4.TRS(_oldTransform.position, _oldTransform.rotation, _oldTransform.localScale);
        newTransform = Matrix4x4.TRS(_newTransform.position, _newTransform.rotation, _newTransform.localScale);
    }

    public override bool Undo()
    {
        if (gameObject == null) return false;

        gameObject.transform.rotation = oldTransform.GetRotation();
        gameObject.transform.position = oldTransform.GetPosition();
        gameObject.transform.localScale = oldTransform.GetScale();

        return true;
    }

    public override bool Redo()
    {
        if (gameObject == null) return false;

        gameObject.transform.rotation = newTransform.GetRotation();
        gameObject.transform.position = newTransform.GetPosition();
        gameObject.transform.localScale = newTransform.GetScale();

        return true;
    }
}
