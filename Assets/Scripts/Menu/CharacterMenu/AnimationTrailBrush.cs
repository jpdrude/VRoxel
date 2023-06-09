using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTrailBrush : MonoBehaviour
{
    [SerializeField]
    LineRenderer renderer;

    [SerializeField]
    CustomAnimationPlayer animationPlayer;

    [SerializeField]
    float strokeLength = 5;
    public float StrokeLength { get { return strokeLength; } set { strokeLength = value; } }

    float timer;

    Queue<Vector3> controlPoints = new Queue<Vector3>();
    Queue<float> addTime = new Queue<float>();

    bool loopBegin = true;

    [SerializeField]
    bool pause = false;

    // Update is called once per frame
    void Update()
    {
        if (pause) return;

        if (loopBegin)
        {
            controlPoints.Clear();
            addTime.Clear();
            loopBegin = false;
            return;
        }

        controlPoints.Enqueue(transform.position);
        addTime.Enqueue(timer);

        while (true)
        {
            if (addTime.Peek() + strokeLength < timer)
            {
                addTime.Dequeue();
                controlPoints.Dequeue();
            }
            else
                break;
        }

        renderer.positionCount = controlPoints.Count;
        renderer.SetPositions(controlPoints.ToArray());

        timer += Time.deltaTime;
    }

    public void PauseStroke(bool value)
    {
        pause = value;
    }


    public void ResetStroke()
    {
        loopBegin = true;
    }

    private void OnEnable()
    {
        ResetStroke();
    }
}
