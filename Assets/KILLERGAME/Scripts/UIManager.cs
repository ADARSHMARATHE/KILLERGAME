using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KillerGame
{
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI hudDay;
        public TextMeshProUGUI hudFood;
        public TextMeshProUGUI hudWood;
        public TextMeshProUGUI hudCoal;
        public TextMeshProUGUI hudIron;
        public TextMeshProUGUI hudGold;
        public TextMeshProUGUI hudTemp;

        [Header("HUD Resource Icons")]
        public Image hudFoodIcon;
        public Image hudWoodIcon;
        public Image hudCoalIcon;
        public Image hudIronIcon;
        public Image hudGoldIcon;

        [Header("Building Sprites (sawmill,coalmine,farm,ironmine,barracks,market order)")]
        public Sprite[] buildingSprites;

        [Header("Hero Sprites (scout,commander,engineer order)")]
        public Sprite[] heroSprites;

        [Header("Notification Bar")]
        public TextMeshProUGUI notifText;

        [Header("Tab Panels")]
        public GameObject furnacePanel;
        public GameObject basePanel;
        public GameObject troopsPanel;
        public GameObject heroesPanel;

        [Header("Tab Buttons")]
        public Button furnaceTabBtn;
        public Button baseTabBtn;
        public Button troopsTabBtn;
        public Button heroesTabBtn;

        [Header("Furnace Panel")]
        public TextMeshProUGUI furnaceEmoji;
        public TextMeshProUGUI furnaceTempText;
        public Image           fuelBar;
        public TextMeshProUGUI fuelLabel;
        public TextMeshProUGUI fuelPctText;
        public Button          addFuel10Btn;
        public Button          addFuel25Btn;
        public Button          addFuel50Btn;
        public TextMeshProUGUI burnRateText;
        public TextMeshProUGUI maxTempText;
        public TextMeshProUGUI furnaceLvlText;
        public Button          upgradeFurnaceBtn;
        public TextMeshProUGUI upgradeFurnaceCost;

        [Header("Buildings Scroll")]
        public Transform buildingsContainer;
        public GameObject buildingCardPrefab;

        [Header("Troops")]
        public Transform troopsContainer;
        public GameObject troopCardPrefab;

        [Header("Heroes")]
        public Transform heroesContainer;
        public GameObject heroCardPrefab;

        [Header("Event Modal")]
        public GameObject eventModal;
        public TextMeshProUGUI eventTitle;
        public TextMeshProUGUI eventMessage;
        public Button          eventOkBtn;

        [Header("Game Over")]
        public GameObject gameOverScreen;
        public TextMeshProUGUI gameOverDayText;
        public Button          restartBtn;

        private KillerGameManager _gm;
        private GameObject _activePanel;

        // WOS-style tab colors — warm amber for active, dimmed for inactive
        static readonly Color TAB_ACTIVE_FURNACE  = new Color(0.90f, 0.50f, 0.08f); // amber-orange
        static readonly Color TAB_ACTIVE_BASE     = new Color(0.22f, 0.55f, 0.90f); // steel blue
        static readonly Color TAB_ACTIVE_TROOPS   = new Color(0.30f, 0.75f, 0.35f); // forest green
        static readonly Color TAB_ACTIVE_HEROES   = new Color(0.65f, 0.35f, 0.90f); // purple
        static readonly Color TAB_INACTIVE_MULT   = new Color(0.48f, 0.48f, 0.48f);

        // Color constants
        static readonly Color COL_HOT    = new Color(1f, 0.55f, 0.1f);
        static readonly Color COL_WARM   = new Color(1f, 0.85f, 0.2f);
        static readonly Color COL_COOL   = new Color(0.5f, 0.8f, 1f);
        static readonly Color COL_COLD   = new Color(0.2f, 0.5f, 1f);
        static readonly Color COL_FREEZE = new Color(0.8f, 0.2f, 1f);
        static readonly Color COL_DANGER = new Color(0.97f, 0.32f, 0.29f);
        static readonly Color COL_WARN   = new Color(0.89f, 0.7f, 0.25f);

        void Start()
        {
            if (Object.FindAnyObjectByType<WildBrawlUI>() == null)
                new GameObject("WildBrawlUI").AddComponent<WildBrawlUI>();

            ResolveBuildReferences();

            if (furnaceEmoji != null)
            {
                furnaceEmoji.gameObject.SetActive(false);
                furnaceEmoji.text = string.Empty;
            }

            _gm = KillerGameManager.Instance;
            if (_gm == null)
            {
                Debug.LogError("UIManager: KillerGameManager.Instance is null.");
                return;
            }
            _gm.OnStateChanged.AddListener(Refresh);
            _gm.OnEvent.AddListener(ShowEvent);

            // Tab wiring
            furnaceTabBtn?.onClick.AddListener(() => ShowTab(furnacePanel));
            baseTabBtn?.onClick.AddListener(() => ShowTab(basePanel));
            troopsTabBtn?.onClick.AddListener(() => ShowTab(troopsPanel));
            heroesTabBtn?.onClick.AddListener(() => ShowTab(heroesPanel));

            // Fuel buttons
            addFuel10Btn?.onClick.AddListener(() => _gm.AddFuel(10f));
            addFuel25Btn?.onClick.AddListener(() => _gm.AddFuel(25f));
            addFuel50Btn?.onClick.AddListener(() => _gm.AddFuel(50f));

            // Furnace upgrade
            upgradeFurnaceBtn?.onClick.AddListener(() => _gm.UpgradeBuilding("furnace"));

            // Event modal
            eventOkBtn?.onClick.AddListener(() => eventModal?.SetActive(false));

            // Game over
            restartBtn?.onClick.AddListener(() => { _gm.RestartGame(); gameOverScreen?.SetActive(false); });

            // Start on furnace tab
            ShowTab(furnacePanel);
            Refresh();
        }

        void ResolveBuildReferences()
        {
            if (buildingsContainer == null)
            {
                var content = GameObject.Find("BContent");
                if (content != null) buildingsContainer = content.transform;
            }

            if (buildingCardPrefab == null)
            {
#if UNITY_EDITOR
                buildingCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/KILLERGAME/Prefabs/BuildingCard.prefab");
#endif
            }
        }

        void ShowTab(GameObject panel)
        {
            furnacePanel?.SetActive(false);
            basePanel?.SetActive(false);
            troopsPanel?.SetActive(false);
            heroesPanel?.SetActive(false);

            panel?.SetActive(true);
            _activePanel = panel;

            UpdateTabHighlights(panel);

            // Rebuild dynamic lists when tab opens
            if (panel == basePanel)    BuildBuildingCards();
            if (panel == troopsPanel)  BuildTroopCards();
            if (panel == heroesPanel)  BuildHeroCards();
        }

        void UpdateTabHighlights(GameObject activePanel)
        {
            SetTabActive(furnaceTabBtn, activePanel == furnacePanel, TAB_ACTIVE_FURNACE);
            SetTabActive(baseTabBtn,    activePanel == basePanel,    TAB_ACTIVE_BASE);
            SetTabActive(troopsTabBtn,  activePanel == troopsPanel,  TAB_ACTIVE_TROOPS);
            SetTabActive(heroesTabBtn,  activePanel == heroesPanel,  TAB_ACTIVE_HEROES);
        }

        void SetTabActive(Button btn, bool active, Color activeColor)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = active ? activeColor : activeColor * TAB_INACTIVE_MULT;
            // Scale slightly when active
            btn.transform.localScale = active ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;

            // Toggle the active indicator line (named "<TabName>_ActiveLine")
            string indicatorName = btn.gameObject.name + "_ActiveLine";
            var indicator = btn.transform.Find(indicatorName);
            if (indicator != null)
            {
                var lineImg = indicator.GetComponent<UnityEngine.UI.Image>();
                if (lineImg != null)
                    lineImg.color = active
                        ? new Color(activeColor.r, activeColor.g, activeColor.b, 1f)
                        : new Color(activeColor.r, activeColor.g, activeColor.b, 0f);
            }
        }

        public void Refresh()
        {
            if (_gm == null) return;
            var s = _gm.State;

            RefreshHUD(s);
            RefreshNotif(s);
            if (_activePanel == furnacePanel) RefreshFurnace(s);
            if (_activePanel == basePanel)    BuildBuildingCards();
            if (_activePanel == troopsPanel)  BuildTroopCards();
            if (_activePanel == heroesPanel)  BuildHeroCards();
            if (s.gameOver) ShowGameOver(s);
        }

        // WOS-style resource colors
        static readonly Color RES_FOOD = new Color(0.494f, 0.784f, 0.314f); // #7EC850 green
        static readonly Color RES_WOOD = new Color(0.784f, 0.502f, 0.251f); // #C88040 brown
        static readonly Color RES_COAL = new Color(0.565f, 0.565f, 0.627f); // #9090A0 grey
        static readonly Color RES_IRON = new Color(0.502f, 0.659f, 0.784f); // #80A8C8 steel blue
        static readonly Color RES_GOLD = new Color(0.941f, 0.753f, 0.188f); // #F0C030 yellow

        void RefreshHUD(GameState s)
        {
            if (hudDay)
            {
                hudDay.text      = $"Day {s.day}";
                hudDay.color     = Color.white;
            }
            if (hudFood) { hudFood.text = Fmt(s.GetResource("food").amount); hudFood.color = RES_FOOD; }
            if (hudWood) { hudWood.text = Fmt(s.GetResource("wood").amount); hudWood.color = RES_WOOD; }
            if (hudCoal) { hudCoal.text = Fmt(s.GetResource("coal").amount); hudCoal.color = RES_COAL; }
            if (hudIron) { hudIron.text = Fmt(s.GetResource("iron").amount); hudIron.color = RES_IRON; }
            if (hudGold) { hudGold.text = Fmt(s.GetResource("gold").amount); hudGold.color = RES_GOLD; }

            float t = s.furnace.temp;
            if (hudTemp)
            {
                hudTemp.text  = $"{t:0}°C";
                hudTemp.color = TempColor(t);
            }
        }

        void RefreshNotif(GameState s)
        {
            if (notifText == null || s.notifications.Count == 0) return;
            notifText.text = string.Join("\n", s.notifications.GetRange(0, Mathf.Min(4, s.notifications.Count)));
        }

        void RefreshFurnace(GameState s)
        {
            var f = s.furnace;

            if (furnaceTempText)
            {
                furnaceTempText.text  = $"{f.temp:0.0}°C";
                furnaceTempText.color = TempColor(f.temp);
            }

            if (fuelBar)
            {
                fuelBar.fillAmount = f.fuelPct / 100f;
                fuelBar.color      = f.fuelPct > 50f ? COL_HOT
                                   : f.fuelPct > 20f ? COL_WARN
                                   : COL_DANGER;
            }

            if (fuelLabel)
                fuelLabel.text = f.fuelPct < 15f ? "!! CRITICAL - Add Fuel Now !!"
                               : f.fuelPct < 30f ? "! Fuel Low"
                               : "Fuel Level";

            if (fuelPctText) fuelPctText.text = $"{f.fuelPct:0.0}%";
            if (burnRateText) burnRateText.text = $"{f.fuelRate:0.00}%/s";
            if (maxTempText)  maxTempText.text  = $"{f.maxTemp}°C";

            var bld = s.GetBuilding("furnace");
            if (furnaceLvlText) furnaceLvlText.text = $"Level {bld.level}";

            var def  = Defs.Buildings["furnace"];
            int next = bld.level + 1;
            if (next <= def.maxLevel)
            {
                var cost = def.UpgradeCost(next);
                if (upgradeFurnaceCost)
                    upgradeFurnaceCost.text = $"Wood:{cost.wood} Coal:{cost.coal} Iron:{cost.iron}";
            }
        }

        // Building key order matching buildingSprites array (furnace excluded from base panel)
        static readonly string[] BUILDING_SPRITE_KEYS = { "sawmill", "coalmine", "farm", "ironmine", "barracks", "market" };

        Sprite GetBuildingSprite(string key)
        {
            if (buildingSprites == null) return null;
            for (int i = 0; i < BUILDING_SPRITE_KEYS.Length; i++)
                if (BUILDING_SPRITE_KEYS[i] == key && i < buildingSprites.Length)
                    return buildingSprites[i];
            return null;
        }

        static readonly string[] HERO_SPRITE_KEYS = { "scout", "commander", "engineer" };

        Sprite GetHeroSprite(string key)
        {
            if (heroSprites == null) return null;
            for (int i = 0; i < HERO_SPRITE_KEYS.Length; i++)
                if (HERO_SPRITE_KEYS[i] == key && i < heroSprites.Length)
                    return heroSprites[i];
            return null;
        }

        void BuildBuildingCards()
        {
            if (buildingsContainer == null || buildingCardPrefab == null) return;
            foreach (Transform c in buildingsContainer) Destroy(c.gameObject);

            var s = _gm.State;
            foreach (var kv in Defs.Buildings)
            {
                if (kv.Key == "furnace") continue;
                var bld  = s.GetBuilding(kv.Key);
                var def  = kv.Value;
                var card = Instantiate(buildingCardPrefab, buildingsContainer);
                var bc   = card.GetComponent<BuildingCard>();
                if (bc != null) bc.Setup(def, bld, s, _gm, GetBuildingSprite(kv.Key));
            }

            // Force layout rebuild so cards get correct sizes immediately
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buildingsContainer.GetComponent<RectTransform>());

            // Scroll to top
            var sr = buildingsContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        void BuildTroopCards()
        {
            if (troopsContainer == null || troopCardPrefab == null) return;
            foreach (Transform c in troopsContainer) Destroy(c.gameObject);

            var s = _gm.State;
            foreach (var kv in Defs.Troops)
            {
                var troop = s.GetTroop(kv.Key);
                var def   = kv.Value;
                var card  = Instantiate(troopCardPrefab, troopsContainer);
                var tc    = card.GetComponent<TroopCard>();
                if (tc != null) tc.Setup(def, troop, s, _gm);
            }
        }

        void BuildHeroCards()
        {
            if (heroesContainer == null || heroCardPrefab == null) return;
            foreach (Transform c in heroesContainer) Destroy(c.gameObject);

            var s = _gm.State;
            foreach (var kv in Defs.Heroes)
            {
                var hero = s.GetHero(kv.Key);
                var def  = kv.Value;
                var card = Instantiate(heroCardPrefab, heroesContainer);
                var hc   = card.GetComponent<HeroCard>();
                if (hc != null) hc.Setup(def, hero, s, _gm, GetHeroSprite(kv.Key));
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(heroesContainer.GetComponent<RectTransform>());

            var sr = heroesContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        void ShowEvent(string msg)
        {
            if (eventModal == null) return;
            eventModal.SetActive(true);
            if (eventTitle)   eventTitle.text   = "Event!";
            if (eventMessage) eventMessage.text = msg;
        }

        void ShowGameOver(GameState s)
        {
            if (gameOverScreen == null) return;
            gameOverScreen.SetActive(true);
            if (gameOverDayText) gameOverDayText.text = $"Survived {s.day} days";
        }

        static string Fmt(float n)
        {
            if (n >= 1_000_000f) return $"{n/1_000_000f:0.0}M";
            if (n >= 1_000f)     return $"{n/1_000f:0.0}k";
            return $"{Mathf.FloorToInt(n)}";
        }

        static Color TempColor(float t)
        {
            if (t > 15f)  return COL_HOT;    // orange  >15°C
            if (t >= 5f)  return COL_WARM;   // yellow  5-15°C
            if (t >= -5f) return COL_COOL;   // blue-ish
            if (t >= -15f) return COL_COLD;
            return COL_FREEZE;               // red <-15°C (freeze)
        }
    }
}
