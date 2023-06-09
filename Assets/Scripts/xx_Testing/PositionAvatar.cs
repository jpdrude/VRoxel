using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionAvatar : MonoBehaviour
{
    [SerializeField]
    GameObject pickupCharacter;
    Vector3? pickUpPosition = null;

    [SerializeField]
    Transform rotationTarget;

    [SerializeField]
    bool setCharacter = false;

    [SerializeField]
    bool setDirection = false;

    [SerializeField]
    bool finish = false;

    private void Update()
    {
        if (setCharacter && !setDirection)
        {
            if (pickUpPosition == null)
                pickUpPosition = pickupCharacter.transform.position;

            pickupCharacter.GetComponent<CustomAnimationPlayer>().PauseAnimation(true);
            pickupCharacter.transform.position = transform.position;
        }
        else if (setDirection)
        {
            pickupCharacter.transform.LookAt(rotationTarget.position, Vector3.up);
        }

        if (finish)
        {
            finish = false;
            setDirection = false;
            setCharacter = false;

            float angle = pickupCharacter.transform.rotation.eulerAngles.y;

            pickupCharacter.GetComponent<CustomAnimationPlayer>().SetOffset(transform.position, angle);
            pickUpPosition = null;
        }
    }
}
