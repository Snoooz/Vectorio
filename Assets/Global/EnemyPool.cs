﻿// Add this script to an enemy to add it to a defenses
// targetting AI. If this script is not added to an
// enemy, then it will not be instantiated in memory
// as an enemy object and will not be targetted by AI.

using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    // Get the global HashSet of every enemy using this script
    public readonly static HashSet<GameObject> Pool = new HashSet<GameObject>();

    // On enable, add enemy to the HashSet
    private void OnEnable()
    {
        EnemyPool.Pool.Add(gameObject);
    }

    // On disable, remove enemy from the HashSet
    private void OnDisable()
    {
        EnemyPool.Pool.Remove(gameObject);
    }

    // Find the nearest enemy, and return the object
    // Updates ever 0.5s and caches (very cpu efficient)
    public static GameObject FindClosestEnemy(Vector3 pos, float maxDistance)
    {
        GameObject result = null;
        float dist = float.PositiveInfinity;
        var e = EnemyPool.Pool.GetEnumerator();
        while (e.MoveNext())
        {
            float d = (e.Current.transform.position - pos).sqrMagnitude;
            if (d < dist)
            {
                result = e.Current;
                dist = d;
            }
        }

        if (dist <= maxDistance)
        {
            return result;
        }
        return null;
    }
}
