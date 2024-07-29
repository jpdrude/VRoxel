using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct AnimationMapping
{
    public string hips;

    [Space]
    public string rightUpLeg;
    public string rightLeg;
    public string rightFoot;
    public string rightToeBase;

    [Space]
    public string leftUpLeg;
    public string leftLeg;
    public string leftFoot;
    public string leftToeBase;

    [Space]
    public string spine;
    public string spine1;
    public string spine2;
    public string spine3;

    [Space]
    public string leftShoulder;
    public string leftArm;
    public string leftForeArm;
    public string leftHand;

    [Space]
    public string rightShoulder;
    public string rightArm;
    public string rightForeArm;
    public string rightHand;

    [Space]
    public string neck;
    public string head;
}
