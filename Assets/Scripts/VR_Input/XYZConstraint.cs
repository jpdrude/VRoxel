using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class XYZConstraint : MonoBehaviour
{
    [Header("VR-Input")]
    [SerializeField]
    SteamVR_Action_Boolean _trigger = null;

    static SteamVR_Action_Boolean trigger;

    [SerializeField]
    SteamVR_AssemblySpawner spawner;

    public SteamVR_AssemblySpawner Spawner { get { return spawner; } set { spawner = value; } }

    [Header("Settings")]
    [SerializeField]
    float tolerance = 0.1f;

    bool ortho = false;
    public bool Ortho { get { return ortho; } }

    bool x = false;
    bool y = false;
    bool z = false;

    Vector3 savePos;
    Vector3 localSavePos;

    private void Start()
    {
        if (_trigger != null)
            trigger = _trigger;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (trigger.GetStateDown(SteamVR_Input_Sources.LeftHand))
        {
            ortho = true;
            savePos = transform.position;
            localSavePos = transform.localPosition;
            return;
        }

        if (trigger.GetStateUp(SteamVR_Input_Sources.LeftHand))
        {
            DeactivateConstraints();
        }

        if (ortho && !(x || y || z))
            CheckDirection();

        if (ortho && x)
            ConstrainToX();
        else if (ortho && y)
            ConstrainToY();
        else if (ortho && z)
            ConstrainToZ();
    }

    private void OnDisable()
    {
        DeactivateConstraints();
    }

    void DeactivateConstraints()
    {
        ortho = false;
        x = false;
        y = false;
        z = false;

        transform.localPosition = localSavePos;
        spawner.UpdateAllScales();
    }

    void ConstrainToX()
    {
        transform.position = new Vector3(transform.position.x, savePos.y, savePos.z);
    }

    void ConstrainToY()
    {
        transform.position = new Vector3(savePos.x, transform.position.y, savePos.z);
    }

    void ConstrainToZ()
    {
        transform.position = new Vector3(savePos.x, savePos.y, transform.position.z);
    }

    void CheckDirection()
    {
        Vector3 dir = savePos - transform.position;
        float _x = Mathf.Abs(dir.x);
        float _y = Mathf.Abs(dir.y);
        float _z = Mathf.Abs(dir.z);

        if (_x > _y && _x > _z)
        {
            if (_x > tolerance)
            {
                x = true;
                return;
            }
        }
        else if (_y > _x && _y > _z)
        {
            if (_y > tolerance)
            {
                y = true;
                return;
            }
        }
        else if (_z > _x && _z > _y)
        {
            if (_z > tolerance)
            {
                z = true;
                return;
            }
        }
    }
}
