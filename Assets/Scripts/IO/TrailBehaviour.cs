using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailBehaviour : MonoBehaviour
{
    float timer = 0;
    List<Renderer> body;
    float startFade;
    float lifeTime;
    Color startCol;
    Color endCol;

    public void Initialize(List<Renderer> _body, List<Renderer> joints, Material fadeMat, float _startFade, float _lifeTime)
    {
        foreach(Renderer joint in joints)
            Destroy(joint);

        body = _body;
        lifeTime = _lifeTime;
        startFade = _startFade;

        SetAsTrail(startFade, fadeMat);
    }

    public void SetAsTrail(float fade, Material fadeMat)
    {
        foreach (Renderer renderer in body)
        {
            renderer.material = fadeMat;

            startCol = renderer.material.color;
            startCol.a = fade;
            renderer.material.color = startCol;
        }

        endCol = startCol;
        endCol.a = 0;
    }

    private void Update()
    {
        if (timer / lifeTime > 1)
        {
            Destroy(gameObject);
            return;
        }

        foreach (Renderer renderer in body)
            renderer.material.color = Color.Lerp(startCol, endCol, timer / lifeTime);

        timer += AnimationIO.FrameTime;
    }
}
