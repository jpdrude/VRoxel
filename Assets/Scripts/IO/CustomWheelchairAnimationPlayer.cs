using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomWheelchairAnimationPlayer : CustomAnimationPlayer
{
    Transform hips;

    Transform rightUpLeg;
    Transform rightLeg;

    Transform leftUpLeg;
    Transform leftLeg;

    AnimationCurve posXCurve;
    AnimationCurve posYCurve;
    AnimationCurve posZCurve;

    AnimationCurve rotXCurve;
    AnimationCurve rotYCurve;
    AnimationCurve rotZCurve;

    float curveOffset;

    private void Start()
    {
        hips = base.GetHips();

        var _rightLeg = base.GetRightLeg();
        rightUpLeg = _rightLeg.rightUpLeg;
        rightLeg = _rightLeg.rightLeg;

        var _leftLeg = base.GetLeftLeg();
        leftUpLeg = _leftLeg.leftUpLeg;
        leftLeg = _leftLeg.leftLeg;
    }

    protected override void Update()
    {
        base.Update(); // Calls the parent's Update method

        SetLegs();

        SetRootPosition(Timer);

        SetHipRotation(Timer);
    }

    void SetLegs()
    {
        rightUpLeg.localRotation = Quaternion.Euler(-90 - hips.rotation.eulerAngles.x, 0, 0);
        rightLeg.localRotation = Quaternion.Euler(90, 0, 0);

        leftUpLeg.localRotation = Quaternion.Euler(-90 - hips.rotation.eulerAngles.x, 0, 0);
        leftLeg.localRotation = Quaternion.Euler(90, 0, 0);
    }

    void SetHipRotation(float time)
    {
        var rot = Quaternion.Euler(rotXCurve.Evaluate(time + curveOffset), rotYCurve.Evaluate(time + curveOffset), rotZCurve.Evaluate(time + curveOffset));
        var fixRot = Quaternion.AngleAxis(90, rot * Vector3.right);

        rot = fixRot * rot;

        hips.localRotation = rot;
    }


    void SetRootPosition(float time)
    {
        Vector3 pos = new Vector3();
        Quaternion rot = Quaternion.Euler(0, RotationOffset, 0);

        pos.x = posXCurve.Evaluate(time + curveOffset);
        pos.y = posYCurve.Evaluate(time + curveOffset);
        pos.z = posZCurve.Evaluate(time + curveOffset);


        transform.position = (rot * pos) + PositionOffset;
        transform.rotation = rot;
    }

    public void SetWheelchairCurves(WheelchairMovement wheelchairMovement)
    {
        posXCurve = wheelchairMovement.posXCurve;
        posYCurve = wheelchairMovement.posYCurve;
        posZCurve = wheelchairMovement.posZCurve;

        rotXCurve = wheelchairMovement.rotXCurve;
        rotYCurve = wheelchairMovement.rotYCurve;
        rotZCurve = wheelchairMovement.rotZCurve;


        System.DateTime mvnStart = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day, 
            wheelchairMovement.mvnStartHours, wheelchairMovement.mvnStartMinutes, wheelchairMovement.mvnStartSeconds, wheelchairMovement.mvnStartMilliseconds);
        System.DateTime start = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day,
            wheelchairMovement.startHours, wheelchairMovement.startMinutes, wheelchairMovement.startSeconds, wheelchairMovement.startMilliseconds);

        curveOffset = (float)(start - mvnStart).TotalSeconds;
    }
}
