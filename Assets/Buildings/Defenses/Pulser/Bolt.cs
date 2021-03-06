﻿using UnityEngine;

public class Bolt : BulletClass
{

    public ParticleSystem Effect;

    public void Start()
    {
        HitEffect = Effect;
        StartCoroutine(SetLifetime(2));
    }

    public override void collide()
    {
        Instantiate(HitEffect, transform.position, Quaternion.Euler(0, 0, transform.localEulerAngles.z + 180f));
        Destroy(gameObject);
    }

}
