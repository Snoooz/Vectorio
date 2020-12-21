﻿using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class EnemyClass : MonoBehaviour
{

    // Enemy stats
    public int health;
    public int damage;
    public int range;
    public float moveSpeed;
    public int attackSpeed;
    public int worth;
    public int explosiveRadius;
    public int explosiveDamage;
    public GameObject[] spawnOnDeath;
    public int[] amountToSpawn;
    public ParticleSystem Effect;
    public Rigidbody2D body;

    // Enemy vars
    protected GameObject target;
    protected int attackTimeout;
    protected Vector2 Movement;

    // Catalog variable 1
    public Sprite CatalogSprite1;
    public string CatalogTitle1 = "Undeclared title";
    [TextArea] public string CatalogDesc1 = "Undeclared description";

    // Catalog variable 2
    public Sprite CatalogSprite2;
    public string CatalogTitle2 = "Undeclared title";
    [TextArea] public string CatalogDesc2 = "Undeclared description";

    // Catalog variable 3
    public Sprite CatalogSprite3;
    public string CatalogTitle3 = "Undeclared title";
    [TextArea] public string CatalogDesc3 = "Undeclared description";

    // Attack Tile
    public void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollisionStay2D(collision);
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Building"))
        {
            if (attackTimeout <= 0)
            {
                collision.gameObject.GetComponent<TileClass>().DamageTile(damage);
                attackTimeout = attackSpeed;
            }
        }
    }

    // Called by Update() of child classes
    public void BaseUpdate()
    {
        if (attackTimeout > 0) attackTimeout -= 1;
    }

    // Kill entity
    public void KillEntity()
    {
        // If menu scene, re instantiate the object
        if (SceneManager.GetActiveScene().name == "Menu") {
            Instantiate(Effect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else
        {
            // If has explosive damage, apply it to surrounding tiles
            if (explosiveRadius > 0 && explosiveDamage > 0)
            {
                var colliders = Physics2D.OverlapCircleAll(transform.position, explosiveRadius, 1 << LayerMask.NameToLayer("Building"));
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].GetComponent<TileClass>().DamageTile(explosiveDamage);
                }
            }

            // If spawns on death, itterate through and spawn enemies
            if (spawnOnDeath.Length > 0)
            {
                if (amountToSpawn.Length != spawnOnDeath.Length)
                {
                    Debug.LogError("Custom error #001\n- Mismatched array size!");
                    return;
                }
                for (int a = 0; a < spawnOnDeath.Length; a++)
                {
                    for (int b = 0; b < amountToSpawn[a]; b++)
                    {
                        if (spawnOnDeath[a] == gameObject)
                        {
                            Debug.LogError("Custom error #002\n- Enemies cannot spawn themselves on death");
                        }
                        else
                        {
                            Instantiate(spawnOnDeath[a], transform.position, Quaternion.identity);
                        }
                    }
                }
            }

            // Instantiate death effect and destroy self
            GameObject.Find("Survival").GetComponent<Survival>().UpdateUnlock(gameObject.transform);
            Instantiate(Effect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    // Apply damage to entity
    public void DamageEntity(int dmgRecieved)
    {
        health -= dmgRecieved;
        if (health <= 0)
        {
             KillEntity();
        }
    }

    // Return damage
    public int getDamage()
    {
        return damage;
    }

    // Return moveSpeed
    public float getSpeed()
    {
        return moveSpeed;
    }

    // Set the move speed
    public void setSpeed(float a)
    {
        moveSpeed = a;
    }

    protected GameObject FindNearestDefence()
    {
        var colliders = Physics2D.OverlapCircleAll(
            this.gameObject.transform.position, 
            range, 
            1 << LayerMask.NameToLayer("Building"));
        GameObject result = null;
        float closest = float.PositiveInfinity;

        foreach (Collider2D collider in colliders)
        {
            float distance = (collider.transform.position - this.transform.position).sqrMagnitude;
            if (distance < closest) {
                result = collider.gameObject;
                closest = distance;
            }
        }
        return result;
    }

}
