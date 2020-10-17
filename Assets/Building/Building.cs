﻿using UnityEngine;

public class Building : MonoBehaviour
{

    // Placement sprites
    public Sprite Turret;
    public Sprite Enemy;
    public Sprite Bomber;
    private SpriteRenderer Selected;
    private float Adjustment = 1f;
    private int AdjustLimiter = 0;
    private bool AdjustSwitch = false;
    private bool GridSnap = false;

    // Object placements
    [SerializeField]
    private GameObject TurretObj;
    [SerializeField]
    private GameObject EnemyObj;
    [SerializeField]
    private GameObject BomberObj;
    private GameObject SelectedObj;

    // Internal placement variables
    [SerializeField]
    private LayerMask TileLayer;
    private Vector2 MousePos;

    private void Start()
    {
        Selected = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Get mouse position and round to middle grid coordinate
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (GridSnap == false) {
            transform.position = MousePos;
        } else {
            transform.position = new Vector2(Mathf.Round(MousePos.x), Mathf.Round(MousePos.y));
        }

        // Make color flash
        Color tmp = this.GetComponent<SpriteRenderer>().color;
        tmp.a = Adjustment;
        this.GetComponent<SpriteRenderer>().color = tmp;
        AdjustAlphaValue();

        // If user left clicks, place object
        if (Input.GetButtonDown("Fire1"))
        {

            Vector2 mouseRay = Camera.main.ScreenToWorldPoint(transform.position);
            RaycastHit2D rayHit = Physics2D.Raycast(mouseRay, Vector2.zero, Mathf.Infinity, TileLayer);

            // Raycast tile to see if there is already a tile placed
            if (rayHit.collider == null)
            {
                Instantiate(SelectedObj, transform.position, Quaternion.identity);
            }
        }

        // If user right clicks, place object
        else if (Input.GetButtonDown("Fire2"))
        {

            Vector2 mouseRay = Camera.main.ScreenToWorldPoint(transform.position);
            RaycastHit2D rayHit = Physics2D.Raycast(mouseRay, Vector2.zero, Mathf.Infinity, TileLayer);

            // Raycast tile to see if there is already a tile placed
            if (rayHit.collider != null)
            {
                Destroy(rayHit.collider.gameObject);
            }
        }

        // Change selected object
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Adjustment = 1f;
            Selected.sprite = Bomber;
            SelectedObj = BomberObj;
            transform.localScale = new Vector3(0.2f, 0.2f, 0.12f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Adjustment = 1f;
            Selected.sprite = Turret;
            SelectedObj = TurretObj;
            transform.localScale = new Vector3(0.19f, 0.19f, 0.19f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Adjustment = 1f;
            Selected.sprite = Enemy;
            SelectedObj = EnemyObj;
            transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }
        else if (Input.GetKeyDown(KeyCode.G) && GridSnap == false)
        {
            GridSnap = true;
        }
        else if (Input.GetKeyDown(KeyCode.G) && GridSnap == true)
        {
            GridSnap = false;
        }

    }

    public void AdjustAlphaValue()
    {
        if (AdjustLimiter == 40)
        {
            if (AdjustSwitch == false)
            {
                Adjustment -= 0.1f;
            }
            else if (AdjustSwitch == true)
            {
                Adjustment += 0.1f;
            }
            if (AdjustSwitch == false && Adjustment <= 0f)
            {
                AdjustSwitch = true;
            }
            else if (AdjustSwitch == true && Adjustment >= 1f)
            {
                AdjustSwitch = false;
            }
            AdjustLimiter = 0;
        }
        else
        {
            AdjustLimiter += 1;
        }
    }

}