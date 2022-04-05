using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailBehaviour : MonoBehaviour
{
    float timer = 0;
    Renderer body;
    float startFade;
    float lifeTime;
    Color startCol;
    Color endCol;

    public void Initialize(Renderer _body, Renderer joints, Material fadeMat, float _startFade, float _lifeTime)
    {
        Destroy(joints);
        body = _body;
        lifeTime = _lifeTime;
        startFade = _startFade;

        SetAsTrail(startFade, fadeMat);
    }

    public void SetAsTrail(float fade, Material fadeMat)
    {
        body.material = fadeMat;

        startCol = body.material.color;
        startCol.a = fade;
        body.material.color = startCol;

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

        body.material.color = Color.Lerp(startCol, endCol, timer / lifeTime);
        timer += Time.deltaTime;
    }
}
