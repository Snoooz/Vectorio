﻿
///////////////////////////////////////////
// This class is currently being redone. //
///////////////////////////////////////////

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;

public class Survival : MonoBehaviour
{
    // Player stats
    public int gold = 0;
    public int essence = 0;
    public int iridium = 0;
    public int PowerConsumption = 0;
    public int AvailablePower = 100;

    // Placement sprites
    private SpriteRenderer Selected;
    private float Adjustment = 1f;
    private int AdjustLimiter = 0;
    private bool AdjustSwitch = false;

    // Object placements
    [SerializeField] private Transform HubObj;           // No ID
    [SerializeField] private Transform TurretObj;        // ID = 0
    [SerializeField] private Transform WallObj;          // ID = 1
    [SerializeField] private Transform CollectorObj;     // ID = 2
    [SerializeField] private Transform ShotgunObj;       // ID = 3
    [SerializeField] private Transform SniperObj;        // ID = 4
    [SerializeField] private Transform EnhancerObj;      // ID = 5
    [SerializeField] private Transform SMGObj;           // ID = 6
    [SerializeField] private Transform BoltObj;          // ID = 7
    [SerializeField] private Transform ChillerObj;       // ID = 8
    [SerializeField] private Transform RocketObj;        // ID = 9
    [SerializeField] private Transform EssenceObj;       // ID = 10
    [SerializeField] private Transform TurbineObj;       // ID = 11

    // Object variables
    public int seed;
    public GameObject Spawner;
    public GameObject SelectedOverlay;
    private Transform SelectedObj;
    private Transform HoveredObj;
    private Transform LastObj;
    private float rotation = 0f;
    public bool largerUnit = false;

    // UI Elements
    public GameObject HotbarUI;
    public Canvas Overlay;
    private bool MenuOpen;
    private bool BuildingOpen;
    private bool ResearchOpen;
    private bool ShowingInfo;
    public TextMeshProUGUI GoldAmount;
    public TextMeshProUGUI EssenceAmount;
    public TextMeshProUGUI IridiumAmount;
    public ModalWindowManager UOL;
    public ProgressBar PowerUsageBar;
    public ProgressBar[] UpgradeProgressBars;
    public TextMeshProUGUI UpgradeProgressName;
    public ButtonManagerBasic SaveButton;
    public ButtonManagerBasicIcon[] hotbarButtons;

    // Internal placement variables
    [SerializeField] private LayerMask ResourceLayer;
    [SerializeField] private LayerMask TileLayer;
    [SerializeField] private LayerMask UILayer;
    private Vector2 MousePos;
    protected float distance = 10;
    private Transform[] hotbar = new Transform[9];
    List<Transform> unlocked = new List<Transform>();

    // Unlock list
    public int UnlockLvl = 0;
    public bool UnlocksLeft = true;
    [System.Serializable]
    public class Unlockables
    {
        public Transform Unlock;
        public ButtonManagerBasicIcon InventoryButton;
        public Transform[] Enemy;
        public int[] AmountNeeded;
        public int[] AmountTracked;
    }
    public Unlockables[] UnlockTier;

    private void Start()
    {
        // Assign default variables
        Selected = GetComponent<SpriteRenderer>();
        MenuOpen = false;
        ResearchOpen = false;
        BuildingOpen = false;

        // Default starting unlocks / hotbar
        PopulateHotbar();
        unlocked.Add(TurretObj);
        unlocked.Add(CollectorObj);
        unlocked.Add(WallObj);

        // Check for save data on start, and if there is, set values for everything.
        try
        {
            // Load save data to file
            SaveData data = SaveSystem.LoadGame();

            // Set resource amounts
            gold = data.Gold;
            essence = data.Essence;
            iridium = data.Iridium;
            GoldAmount.text = gold.ToString();
            EssenceAmount.text = essence.ToString();
            IridiumAmount.text = iridium.ToString();

            // Update unlock level and research
            UnlockLvl = data.UnlockLevel - 1;
            seed = data.WorldSeed;

            StartNextUnlock();
            UpdateUnlockableGui();
            GameObject.Find("OnSpawn").GetComponent<OnSpawn>().GenerateWorldData(seed);
            PlaceSavedBuildings(data.Locations);

            try
            {
                if (data.UnlockProgress[0] >= 0)
                {
                    UnlockTier[UnlockLvl].AmountTracked = data.UnlockProgress;
                }
            }
            catch { Debug.Log("Save file does not contain tracking progress"); }

            PowerUsageBar.currentPercent = (float)PowerConsumption / (float)AvailablePower * 100;
        }
        catch
        {
            Debug.Log("No save data was found, or the save data found was corrupt.");
            seed = Random.Range(1000000, 10000000);
            GameObject.Find("OnSpawn").GetComponent<OnSpawn>().GenerateWorldData(seed);
        }
    }

    private void Update()
    {
        // Get mouse position and round to middle grid coordinate
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (!largerUnit) transform.position = new Vector2(5 * Mathf.Round(MousePos.x / 5), 5 * Mathf.Round(MousePos.y / 5));
        else transform.position = new Vector2(5 * Mathf.Round(MousePos.x / 5) - 2.5f, 5 * Mathf.Round(MousePos.y / 5) + 2.5f);

        // Make color flash
        Color tmp = this.GetComponent<SpriteRenderer>().color;
        tmp.a = Adjustment;
        this.GetComponent<SpriteRenderer>().color = tmp;
        AdjustAlphaValue();

        // If user left clicks, place object
        if (Input.GetButton("Fire1") && !BuildingOpen && !ResearchOpen && Input.mousePosition.y >= 200)
        {
            bool ValidTile = true;
            if (SelectedObj != null && (SelectedObj.name == "Rocket Pod" || SelectedObj.name == "Turbine"))
            {
                // Check for wires and adjust accordingly 
                RaycastHit2D a = Physics2D.Raycast(new Vector2(MousePos.x, MousePos.y), Vector2.zero, Mathf.Infinity, TileLayer);
                RaycastHit2D b = Physics2D.Raycast(new Vector2(MousePos.x - 5f, MousePos.y), Vector2.zero, Mathf.Infinity, TileLayer);
                RaycastHit2D c = Physics2D.Raycast(new Vector2(MousePos.x, MousePos.y + 5f), Vector2.zero, Mathf.Infinity, TileLayer);
                RaycastHit2D d = Physics2D.Raycast(new Vector2(MousePos.x - 5f, MousePos.y + 5f), Vector2.zero, Mathf.Infinity, TileLayer);

                if (a.collider != null || b.collider != null || c.collider != null || d.collider != null) ValidTile = false;
            }

            // Raycast tile to see if there is already a tile placed
            RaycastHit2D rayHit = Physics2D.Raycast(MousePos, Vector2.zero, Mathf.Infinity, TileLayer);

            if (ValidTile && rayHit.collider == null && SelectedObj != null && transform.position.x <= 250 && transform.position.x >= -245 && transform.position.y <= 245 && transform.position.y >= -245)
            {
                if (SelectedObj == EssenceObj)
                {
                    RaycastHit2D resourceCheck = Physics2D.Raycast(transform.position, Vector2.zero, Mathf.Infinity, ResourceLayer);
                    if (resourceCheck.collider == null || resourceCheck.collider.name != "Essencetile") return;
                }
                int cost = SelectedObj.GetComponent<TileClass>().GetCost();
                int power = SelectedObj.GetComponent<TileClass>().getConsumption();
                if (cost <= gold && PowerConsumption + power <= AvailablePower)
                {
                    RemoveGold(cost);
                    if (SelectedObj == WallObj)
                    {
                        LastObj = Instantiate(SelectedObj, transform.position, Quaternion.Euler(new Vector3(0, 0, 0)));
                    }
                    else
                    {
                        LastObj = Instantiate(SelectedObj, transform.position, Quaternion.Euler(new Vector3(0, 0, rotation)));
                    }
                    LastObj.name = SelectedObj.name;
                    increasePowerConsumption(LastObj.GetComponent<TileClass>().getConsumption());
                    Spawner.GetComponent<WaveSpawner>().increaseHeat(LastObj.GetComponent<TileClass>().GetHeat());
                }
            }
            else if (rayHit.collider != null)
            {
                if (rayHit.collider.name != "Hub")
                {
                    ShowTileInfo(rayHit.collider);
                    ShowingInfo = true;
                    SelectedOverlay.transform.position = rayHit.collider.transform.position;
                    SelectedOverlay.SetActive(true);
                }
            }
        }

        // If user right clicks, remove object
        else if (Input.GetButton("Fire2") && !BuildingOpen)
        {
            //Overlay.transform.Find("Hovering Stats").GetComponent<CanvasGroup>().alpha = 0;
            RaycastHit2D rayHit = Physics2D.Raycast(MousePos, Vector2.zero, Mathf.Infinity, TileLayer);

            // Raycast tile to see if there is already a tile placed
            if (rayHit.collider != null && rayHit.collider.name != "Hub")
            {
                if (rayHit.collider.name == "Wall")
                {
                    RaycastHit2D a = Physics2D.Raycast(new Vector2(transform.position.x + 5f, transform.position.y), Vector2.zero, Mathf.Infinity, TileLayer);
                    RaycastHit2D b = Physics2D.Raycast(new Vector2(transform.position.x - 5f, transform.position.y), Vector2.zero, Mathf.Infinity, TileLayer);
                    RaycastHit2D c = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 5f), Vector2.zero, Mathf.Infinity, TileLayer);
                    RaycastHit2D d = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - 5f), Vector2.zero, Mathf.Infinity, TileLayer);
                    if (a.collider != null && a.collider.name == "Wall")
                    {
                        a.collider.GetComponent<WallAI>().UpdateSprite(-1);
                    }
                    if (b.collider != null && b.collider.name == "Wall")
                    {
                        b.collider.GetComponent<WallAI>().UpdateSprite(-3);
                    }
                    if (c.collider != null && c.collider.name == "Wall")
                    {
                        c.collider.GetComponent<WallAI>().UpdateSprite(-2);
                    }
                    if (d.collider != null && d.collider.name == "Wall")
                    {
                        d.collider.GetComponent<WallAI>().UpdateSprite(-4);
                    }
                }

                if (rayHit.collider.name.Contains("Enhancer"))
                {
                    var colliders = Physics2D.OverlapBoxAll(rayHit.collider.transform.position, new Vector2(7, 7), 1 << LayerMask.NameToLayer("Building"));
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (colliders[i].name.Contains("Collector"))
                        {
                            colliders[i].GetComponent<CollectorAI>().deenhanceCollector();
                        }
                    }
                }
                ShowingInfo = false;
                SelectedOverlay.SetActive(false);
                Spawner.GetComponent<WaveSpawner>().decreaseHeat(rayHit.collider.GetComponent<TileClass>().GetHeat());
                decreasePowerConsumption(rayHit.collider.gameObject.GetComponent<TileClass>().getConsumption());
                int cost = rayHit.collider.GetComponent<TileClass>().GetCost();
                AddGold(cost - cost / 5);
                Destroy(rayHit.collider.gameObject);
            }
        }

        if (BuildingOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetHotbarSlot(0, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetHotbarSlot(1, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetHotbarSlot(2, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetHotbarSlot(3, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetHotbarSlot(4, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                SetHotbarSlot(5, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                SetHotbarSlot(6, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                SetHotbarSlot(7, HoveredObj);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                SetHotbarSlot(8, HoveredObj);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectHotbar(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectHotbar(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectHotbar(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectHotbar(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectHotbar(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectHotbar(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SelectHotbar(6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SelectHotbar(7);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SelectHotbar(8);
        }
        else if (Input.GetKeyDown(KeyCode.R) && BuildingOpen == false && MenuOpen == false && SelectedObj != null)
        {
            rotation = rotation -= 90f;
            if (rotation == -360f)
            {
                rotation = 0;
            }
            Selected.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, rotation));
        }

        if (Input.GetKeyDown(KeyCode.E) && BuildingOpen == false)
        {
            if (ResearchOpen)
            {
                ResearchOpen = false;
                // CLOSE RESEARCH MENU HERE
            }
            BuildingOpen = true;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().alpha = 1;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().interactable = true;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().blocksRaycasts = true;
        }
        else if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) && BuildingOpen == true)
        {
            BuildingOpen = false;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().alpha = 0;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().interactable = false;
            Overlay.transform.Find("Survival Menu").GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
        else if ((Input.GetKeyDown(KeyCode.Escape) && ResearchOpen == true))
        {
            ResearchOpen = false;
            // CLOSE RESEARCH MENU HERE
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && SelectedObj != null)
        {
            Overlay.transform.Find("Selected").GetComponent<CanvasGroup>().alpha = 0;
            Selected.sprite = null;
            SelectedObj = null;
            rotation = 0;
            DisableActiveInfo();
            ShowingInfo = false;
            SelectedOverlay.SetActive(false);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && ShowingInfo == true)
        {
            ShowingInfo = false;
            SelectedOverlay.SetActive(false);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && MenuOpen == false)
        {
            SaveButton.GetComponent<CanvasGroup>().interactable = true;
            SaveButton.buttonText = "SAVE";
            SaveButton.UpdateUI();

            MenuOpen = true;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().alpha = 1;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().blocksRaycasts = true;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().interactable = true;

            Time.timeScale = Mathf.Approximately(Time.timeScale, 0.0f) ? 1.0f : 0.0f;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuOpen = false;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().alpha = 0;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().blocksRaycasts = false;
            Overlay.transform.Find("Paused").GetComponent<CanvasGroup>().interactable = false;

            Time.timeScale = Mathf.Approximately(Time.timeScale, 0.0f) ? 1.0f : 0.0f;
        }
    }

    public void setGameSpeed(int a)
    {
        Time.timeScale = a;
    }

    public void PlaceSavedBuildings(int[,] a)
    {
        for (int i = 0; i < a.GetLength(0); i++)
        {
            Transform building = GetBuildingWithID(a[i, 0]);
            Transform obj = Instantiate(building, new Vector3(a[i, 2], a[i, 3], 0), Quaternion.Euler(new Vector3(0, 0, 0)));
            obj.name = building.name;

            increasePowerConsumption(building.GetComponent<TileClass>().getConsumption());
            Spawner.GetComponent<WaveSpawner>().increaseHeat(building.GetComponent<TileClass>().GetHeat());

            Debug.Log("Placed " + obj.name + " at " + a[i, 2] + " " + a[i, 3]);
        }
    }

    public Transform GetBuildingWithID(int a)
    {
        for (int i = 0; i < unlocked.Count; i++)
        {
            if (unlocked[i].GetComponent<TileClass>().getID() == a)
            {
                return unlocked[i];
            }
        }
        return null;
    }

    // dont touch this
    public void UpdateUnlockableGui()
    {
        for (int i = 0; i < UnlockLvl; i++)
        {
            addUnlocked(UnlockTier[i].Unlock);
            UnlockTier[i].InventoryButton.buttonIcon = Resources.Load<Sprite>("Sprites/" + UnlockTier[i].Unlock.name);
            UnlockTier[i].InventoryButton.UpdateUI();
        }
    }

    public void UpdateUnlock(Transform a)
    {
        if (UnlocksLeft)
        {
            // Itterate through list and update GUI accordingly
            for (int i = 0; i < UnlockTier[UnlockLvl].Enemy.Length; i++)
            {
                if (UnlockTier[UnlockLvl].Enemy[i].name == a.name)
                {
                    // Increment amount tracked and update GUI
                    UnlockTier[UnlockLvl].AmountTracked[i] += 1;
                    UpdateUnlockGui(i, ((double)UnlockTier[UnlockLvl].AmountTracked[i] / (double)UnlockTier[UnlockLvl].AmountNeeded[i]) * 100);
                }
            }

            // Check if requirements have been met
            bool RequirementsMetCheck = true;
            for (int i = 0; i < UnlockTier[UnlockLvl].Enemy.Length; i++)
            {
                if (UnlockTier[UnlockLvl].AmountTracked[i] < UnlockTier[UnlockLvl].AmountNeeded[i])
                {
                    RequirementsMetCheck = false;
                }
            }

            // If requirements met, unlock and start next unlock
            if (RequirementsMetCheck == true)
            {
                Transform newUnlock = UnlockTier[UnlockLvl].Unlock;

                unlockDefense(newUnlock, UnlockTier[UnlockLvl].InventoryButton, newUnlock.GetComponent<TileClass>().GetDescription());
                StartNextUnlock();
            }
        }
    }

    public void UpdateUnlockGui(int a, double b)
    {
        UpgradeProgressBars[a].currentPercent = (float)b;
    }

    public int[] GetAmountTracked()
    {
        try
        {
            return UnlockTier[UnlockLvl].AmountTracked;
        }
        catch
        {
            int[] result = new int[1];
            result[0] = 0;
            return result;
        }
    }

    public void StartNextUnlock()
    {
        UnlockLvl += 1;
        Transform c = Overlay.transform.Find("Upgrade");

        try
        {
            int z = UnlockTier[UnlockLvl].Enemy.Length;
        }
        catch
        {
            UnlocksLeft = false;
            c.gameObject.SetActive(false);
        }
        finally
        {
            if(UnlocksLeft)
            {
                for (int i = 0; i <= 4; i++)
                {
                    UpgradeProgressBars[i].currentPercent = 0;
                    try
                    {
                        UpgradeProgressBars[i].transform.Find("Type").GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/" + UnlockTier[UnlockLvl].Enemy[i].name);
                    }
                    catch
                    {
                        UpgradeProgressBars[i].transform.Find("Type").GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/" + "Undiscovered");
                    }
                }
                UpgradeProgressName.text = UnlockTier[UnlockLvl].Unlock.transform.name;
            }
        }
    }

    void ShowTileInfo(Collider2D a)
    {
        Transform b = Overlay.transform.Find("Prompt");
        b.transform.Find("Health").GetComponent<ProgressBar>().currentPercent = a.GetComponent<TileClass>().GetPercentage();
        b.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = a.name;
    }

    void ShowSelectedInfo(Transform a)
    {
        Overlay.transform.Find("Selected").GetComponent<CanvasGroup>().alpha = 1;
        Transform b = Overlay.transform.Find("Selected");
        b.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = a.name;
        b.transform.Find("Cost").GetComponent<TextMeshProUGUI>().text = a.GetComponent<TileClass>().GetCost().ToString();
        b.transform.Find("Heat").GetComponent<TextMeshProUGUI>().text = a.GetComponent<TileClass>().GetHeat().ToString();
        b.transform.Find("Power").GetComponent<TextMeshProUGUI>().text = a.GetComponent<TileClass>().getConsumption().ToString();
        b.transform.Find("Building").GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/" + a.name);
    }

    public void unlockDefense(Transform a, ButtonManagerBasicIcon b, string c)
    {
        addUnlocked(a);
        b.normalIcon.sprite = Resources.Load<Sprite>("Sprites/" + a.transform.name);
        UOL.icon = Resources.Load<Sprite>("Sprites/" + a.transform.name);
        UOL.titleText = a.transform.name.ToUpper();
        UOL.descriptionText = c;
        UOL.UpdateUI();
        UOL.OpenWindow();
    }

    public void increaseAvailablePower(int a)
    {
        AvailablePower += a;
        PowerUsageBar.currentPercent = (float)PowerConsumption / (float)AvailablePower * 100;
    }

    public void decreaseAvailablePower(int a)
    {
        AvailablePower -= a;
        PowerUsageBar.currentPercent = (float)PowerConsumption / (float)AvailablePower * 100;
    }

    public int getAvailablePower()
    {
        return AvailablePower;
    }

    public void increasePowerConsumption(int a)
    {
        PowerConsumption += a;
        PowerUsageBar.currentPercent = (float)PowerConsumption / (float)AvailablePower * 100;
    }

    public void decreasePowerConsumption(int a)
    {
        PowerConsumption -= a;
        PowerUsageBar.currentPercent = (float)PowerConsumption / (float)AvailablePower * 100;
    }

    public int getPowerConsumption()
    {
        return PowerConsumption;
    }

    public void AddGold(int a)
    {
        gold += a;
        GoldAmount.text = gold.ToString();
    }

    public void RemoveGold(int a)
    {
        gold -= a;
        GoldAmount.text = gold.ToString();
    }

    public void AddEssence(int a)
    {
        essence += a;
        EssenceAmount.text = essence.ToString();
    }

    public void RemoveEssence(int a)
    {
        essence -= a;
        EssenceAmount.text = essence.ToString();
    }

    public void AddIridium(int a)
    {
        iridium += a;
        IridiumAmount.text = iridium.ToString();
    }

    public void RemoveIridium(int a)
    {
        iridium -= a;
        IridiumAmount.text = iridium.ToString();
    }

    public void AdjustAlphaValue()
    {
        if (AdjustLimiter == 5)
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

    private void PopulateHotbar()
    {
        hotbar[0] = TurretObj;
        hotbar[1] = WallObj;
        hotbar[2] = CollectorObj;
        UpdateHotbar();
    }

    public void SelectHotbar(int index)
    {
        try
        {
            SelectedObj = hotbar[index];
            SwitchObj();
            if (SelectedObj.name == "Rocket Pod" || SelectedObj.name == "Turbine")
            {
                largerUnit = true;
                transform.localScale = new Vector3(2, 2, 1);
            }
        }
        catch { return; }
        if (index == 0)
        {
            HotbarUI.transform.Find("One").GetComponent<Button>().interactable = false;
        }
        else if (index == 1)
        {
            HotbarUI.transform.Find("Two").GetComponent<Button>().interactable = false;
        }
        else if (index == 2)
        {
            HotbarUI.transform.Find("Three").GetComponent<Button>().interactable = false;
        }
        else if (index == 3)
        {
            HotbarUI.transform.Find("Four").GetComponent<Button>().interactable = false;
        }
        else if (index == 4)
        {
            HotbarUI.transform.Find("Five").GetComponent<Button>().interactable = false;
        }
        else if (index == 5)
        {
            HotbarUI.transform.Find("Six").GetComponent<Button>().interactable = false;
        }
        else if (index == 6)
        {
            HotbarUI.transform.Find("Seven").GetComponent<Button>().interactable = false;
        }
        else if (index == 7)
        {
            HotbarUI.transform.Find("Eight").GetComponent<Button>().interactable = false;
        }
        else
        {
            HotbarUI.transform.Find("Nine").GetComponent<Button>().interactable = false;
        }
    }

    // Changes the object that the player has selected (pass null to deselect)
    public void SelectObject(Transform obj)
    {
        SelectedObj = obj;
        if (obj != null && !checkIfUnlocked(obj)) return;
        SwitchObj();
        if (SelectedObj.name == "Rocket Pod" || SelectedObj.name == "Turbine")
        {
            largerUnit = true;
            transform.localScale = new Vector3(2, 2, 1);
        }
    }

    // Changes the stored object for hotbar changing
    public void SetHoverObject(Transform obj)
    {
        if (!checkIfUnlocked(obj)) return;
        HoveredObj = obj;
    }

    // Changes the object stored in a hotbar slot
    public void SetHotbarSlot(int slot, Transform obj)
    {
        if (!checkIfUnlocked(obj)) return;
        if (slot < 0 || slot > hotbar.Length) return;
        hotbar[slot] = obj;
        UpdateHotbar();
    }

    public void UpdateHotbar()
    {
        for (int i = 0; i < hotbar.Length; i++)
        {
            if (hotbar[i] != null)
                hotbarButtons[i].buttonIcon = Resources.Load<Sprite>("Sprites/" + hotbar[i].name);
            else
                hotbarButtons[i].buttonIcon = Resources.Load<Sprite>("Sprites/Undiscovered");
            hotbarButtons[i].UpdateUI();
        }
    }

    public void SwitchObj()
    {
        if (largerUnit)
        {
            largerUnit = false;
            transform.localScale = new Vector3(1, 1, 1);
        }
        DisableActiveInfo();
        Adjustment = 1f;
        Selected.sprite = Resources.Load<Sprite>("Sprites/" + SelectedObj.name);
        ShowSelectedInfo(SelectedObj);
    }

    public bool checkIfUnlocked(Transform a)
    {
        for (int i = 0; i < unlocked.Count; i++)
        {
            if (a.name == unlocked[i].name)
            {
                return true;
            }
        }
        DisableActiveInfo();
        return false;
    }

    public void DisableActiveInfo()
    {
        HotbarUI.transform.Find("One").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Two").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Three").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Four").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Five").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Six").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Seven").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Eight").GetComponent<Button>().interactable = true;
        HotbarUI.transform.Find("Nine").GetComponent<Button>().interactable = true;
    }

    public void Quit()
    {
        SceneManager.LoadScene("Menu");
    }

    public void Save()
    {
        Debug.Log("Attempting to save data");
        SaveSystem.SaveGame(this, Spawner.GetComponent<WaveSpawner>());
        Debug.Log("Data was saved successfully");

        SaveButton.buttonText = "SAVED";
        SaveButton.GetComponent<CanvasGroup>().interactable = false;
        SaveButton.UpdateUI();
    }

    public int[,] GetLocationData()
    {
        Transform[] allObjects = FindObjectsOfType<Transform>();

        int length = 0;
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].tag == "Defense") length += 1;
        }

        int[,] data = new int[length, 4];
        length = 0;
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].tag == "Defense")
            {
                try
                {
                    Debug.Log(allObjects[i].name);
                    data[length, 0] = allObjects[i].GetComponent<TileClass>().getID();
                    data[length, 1] = allObjects[i].GetComponent<TileClass>().GetLevel();
                    data[length, 2] = (int)allObjects[i].position.x;
                    data[length, 3] = (int)allObjects[i].position.y;
                    length += 1;
                }
                catch
                {
                    Debug.Log("Error saving " + allObjects[i].name);
                }
            }
        }

        return data;
    }

    public void addUnlocked(Transform a)
    {
        unlocked.Add(a);
        if (a == EssenceObj)
        {
            // SHOW RESEARCH GUI BUTTON
        }
    }

    public bool checkIfBuildingUnlocked(GameObject a)
    {
        for (int i = 0; i < unlocked.Count; i++)
        {
            if (a.name == unlocked[i].name)
            {
                return true;
            }
        }
        return false;
    }

}