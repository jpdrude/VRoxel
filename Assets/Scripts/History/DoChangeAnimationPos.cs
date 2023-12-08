using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoChangeAnimationPos : DoAction
{
    CustomAnimationPlayer animation;
    Vector3 oldPos;
    Vector3 newPos;
    float oldAngle;
    float newAngle;

    public DoChangeAnimationPos(DoType type, CustomAnimationPlayer _animation, Vector3 _oldPos, Vector3 _newPos, float _oldAngle, float _newAngle) : base(type)
    {
        animation = _animation;

        oldPos = _oldPos;
        newPos = _newPos;
        oldAngle = _oldAngle;
        newAngle = _newAngle;
    }

    public override bool Undo()
    {
        if (animation == null) return false;

        animation.SetOffset(oldPos, oldAngle);

        return true;
    }

    public override bool Redo()
    {
        if (animation == null) return false;

        animation.SetOffset(newPos, newAngle);

        return true;
    }
}
