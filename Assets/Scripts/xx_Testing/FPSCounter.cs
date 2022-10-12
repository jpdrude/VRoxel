using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    SteamVR_Action_Boolean trigger;

    float timer = 0;
    int startCount;
    int above20 = 1;
    public SteamVR_Action_Boolean Trigger { get { return trigger; } }
    void Update()
    {
        timer += Time.deltaTime;

        if (trigger.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            timer = 0;
            startCount = BlockContainer.Container.GetCount();
            above20 = 1;
        }

        if (1 / Time.deltaTime < 20)
            above20 = 0;

        if (trigger.GetStateUp(SteamVR_Input_Sources.RightHand))
        {
            float blocksPerSecond = (float)(BlockContainer.Container.GetCount() - startCount) / timer * above20;
            MessageSystem.DisplayMessage(blocksPerSecond + "BPS");
        }
    }
}
