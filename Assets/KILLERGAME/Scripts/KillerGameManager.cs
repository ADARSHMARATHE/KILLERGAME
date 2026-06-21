using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KillerGame
{
    public class KillerGameManager : MonoBehaviour
    {
        public static KillerGameManager Instance { get; private set; }

        public GameState State { get; private set; } = new GameState();

        public UnityEvent OnStateChanged = new UnityEvent();
        public UnityEvent<string> OnEvent = new UnityEvent<string>();

        // How many in-game seconds per real second (speed multiplier)
        [SerializeField] float gameSpeed = 1f;

        private float _dayTimer = 0f;
        private const float DAY_DURATION = 120f; // 2 real minutes per game day

        private Dictionary<string, Coroutine> _upgradeCoroutines = new Dictionary<string, Coroutine>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            State = new GameState();
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f / gameSpeed);
                if (!State.gameOver)
                    Tick(1f);
            }
        }

        void Tick(float dt)
        {
            // ---- Resources (base rates) ----
            foreach (var r in State.resources)
            {
                r.amount = Mathf.Clamp(r.amount + r.rate * dt, 0f, r.cap);
            }

            // ---- Building Production ----
            foreach (var bld in State.buildings)
            {
                if (bld.level <= 0 || bld.upgrading) continue;
                var lvl = bld.level;
                switch (bld.key)
                {
                    case "sawmill":
                        State.GetResource("wood").amount  = Mathf.Min(State.GetResource("wood").cap,  State.GetResource("wood").amount  + 2f   * lvl * dt);
                        break;
                    case "coalmine":
                        State.GetResource("coal").amount  = Mathf.Min(State.GetResource("coal").cap,  State.GetResource("coal").amount  + 1.5f * lvl * dt);
                        break;
                    case "farm":
                        State.GetResource("food").amount  = Mathf.Min(State.GetResource("food").cap,  State.GetResource("food").amount  + 2f   * lvl * dt);
                        break;
                    case "ironmine":
                        State.GetResource("iron").amount  = Mathf.Min(State.GetResource("iron").cap,  State.GetResource("iron").amount  + 1f   * lvl * dt);
                        break;
                    case "market":
                        State.GetResource("gold").amount  = Mathf.Min(State.GetResource("gold").cap,  State.GetResource("gold").amount  + 0.5f * lvl * dt);
                        break;
                }
            }

            // ---- Food consumed by troops ----
            int totalTroops = 0;
            foreach (var t in State.troops) totalTroops += t.count;
            State.GetResource("food").amount = Mathf.Max(0f,
                State.GetResource("food").amount - totalTroops * 0.05f * dt);

            // ---- Furnace fuel burn ----
            var f = State.furnace;
            f.fuelPct -= f.fuelRate * dt;
            f.fuelPct  = Mathf.Clamp(f.fuelPct, 0f, 100f);

            if (f.fuelPct <= 0f)
            {
                // No fuel — cool down fast
                f.temp -= 5f * dt;
            }
            else
            {
                // Drift toward target temp
                f.temp = Mathf.MoveTowards(f.temp, f.targetTemp, 1f * dt);
            }

            // Ambient cold always pulls temp down
            f.temp -= 0.15f * dt;
            f.temp  = Mathf.Max(f.minTemp, f.temp);

            if (f.temp <= -20f)
            {
                State.gameOver = true;
                State.PushNotif("💀 The furnace died. Everyone froze.");
                OnStateChanged.Invoke();
                return;
            }

            // ---- Troop training countdown ----
            foreach (var troop in State.troops)
            {
                if (troop.training > 0f)
                {
                    troop.training -= dt;
                    if (troop.training <= 0f)
                    {
                        troop.training = 0f;
                        troop.count++;
                        var tDef = Defs.Troops[troop.troopKey];
                        State.PushNotif($"{tDef.icon} {tDef.name} training complete!");
                    }
                }
            }

            // ---- Day cycle ----
            _dayTimer += dt;
            if (_dayTimer >= DAY_DURATION)
            {
                _dayTimer = 0f;
                State.day++;
                if (State.day % 5 == 0) MaybeTriggerEvent();
            }

            OnStateChanged.Invoke();
        }

        // ---- Player Actions ----

        public void AddFuel(float amount)
        {
            if (State.gameOver) return;
            float coalCost = Mathf.Ceil(amount / 5f);
            var coal = State.GetResource("coal");
            if (coal.amount < coalCost) { State.PushNotif("⚠ Not enough coal!"); OnStateChanged.Invoke(); return; }
            coal.amount -= coalCost;
            State.furnace.fuelPct    = Mathf.Min(100f, State.furnace.fuelPct + amount);
            State.furnace.targetTemp = Mathf.Min(State.furnace.maxTemp, State.furnace.targetTemp + 2f);
            OnStateChanged.Invoke();
        }

        public void UpgradeBuilding(string key)
        {
            if (State.gameOver) return;
            var bld = State.GetBuilding(key);
            var def = Defs.Buildings[key];

            if (bld.upgrading) { State.PushNotif("Already upgrading!"); OnStateChanged.Invoke(); return; }
            if (bld.level >= def.maxLevel) { State.PushNotif("Max level reached!"); OnStateChanged.Invoke(); return; }

            int nextLvl = bld.level + 1;
            var cost    = def.UpgradeCost(nextLvl);

            if (!Defs.CanAfford(State, cost))
            {
                State.PushNotif($"Not enough resources for {def.name}!");
                OnStateChanged.Invoke();
                return;
            }

            Defs.Deduct(State, cost);
            bld.upgrading = true;
            bld.progress  = 0f;
            State.PushNotif($"Upgrading {def.name} to level {nextLvl}...");
            OnStateChanged.Invoke();

            if (_upgradeCoroutines.ContainsKey(key))
                StopCoroutine(_upgradeCoroutines[key]);
            _upgradeCoroutines[key] = StartCoroutine(UpgradeRoutine(key, def, nextLvl));
        }

        IEnumerator UpgradeRoutine(string key, BuildingDef def, int targetLevel)
        {
            var bld      = State.GetBuilding(key);
            float total  = def.upgradeTimePerLevel * targetLevel;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed    += Time.deltaTime * gameSpeed;
                bld.progress = Mathf.Clamp01(elapsed / total) * 100f;
                OnStateChanged.Invoke();
                yield return null;
            }

            bld.upgrading = false;
            bld.level     = targetLevel;
            bld.progress  = 0f;
            def.OnUpgrade(State, targetLevel);
            State.PushNotif($"✅ {def.name} upgraded to level {targetLevel}!");
            OnStateChanged.Invoke();
        }

        public void UpgradeHero(string key)
        {
            if (State.gameOver) return;
            var hero = State.GetHero(key);
            var def  = Defs.Heroes[key];

            if (hero.upgrading) { State.PushNotif("Hero already upgrading!"); OnStateChanged.Invoke(); return; }
            if (hero.level >= def.maxLevel) { State.PushNotif("Hero at max level!"); OnStateChanged.Invoke(); return; }

            int nextLvl = hero.level + 1;
            var cost    = def.UpgradeCost(nextLvl);

            if (!Defs.CanAfford(State, cost))
            {
                State.PushNotif($"Not enough resources for {def.name}!");
                OnStateChanged.Invoke(); return;
            }

            Defs.Deduct(State, cost);
            hero.upgrading = true;
            hero.progress  = 0f;
            State.PushNotif($"Hiring {def.name} to level {nextLvl}...");
            OnStateChanged.Invoke();

            if (_upgradeCoroutines.ContainsKey("hero_" + key))
                StopCoroutine(_upgradeCoroutines["hero_" + key]);
            _upgradeCoroutines["hero_" + key] = StartCoroutine(HeroUpgradeRoutine(key, def, nextLvl));
        }

        IEnumerator HeroUpgradeRoutine(string key, HeroDef def, int targetLevel)
        {
            var hero     = State.GetHero(key);
            float total  = def.upgradeTimePerLevel * targetLevel;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed      += Time.deltaTime * gameSpeed;
                hero.progress = Mathf.Clamp01(elapsed / total) * 100f;
                OnStateChanged.Invoke();
                yield return null;
            }

            hero.upgrading = false;
            hero.level     = targetLevel;
            hero.progress  = 0f;
            State.PushNotif($"✅ {def.name} is now level {targetLevel}!");
            OnStateChanged.Invoke();
        }

        public void TrainTroop(string key)
        {
            if (State.gameOver) return;
            var def   = Defs.Troops[key];
            var troop = State.GetTroop(key);
            var reqBld = State.GetBuilding(def.requiresBuilding);

            if (reqBld == null || reqBld.level == 0)
            {
                State.PushNotif($"Build {Defs.Buildings[def.requiresBuilding].name} first!");
                OnStateChanged.Invoke(); return;
            }
            if (troop.training > 0f)
            {
                State.PushNotif("Already training!"); OnStateChanged.Invoke(); return;
            }
            if (!Defs.CanAfford(State, def.cost))
            {
                State.PushNotif($"Not enough resources for {def.name}!");
                OnStateChanged.Invoke(); return;
            }

            Defs.Deduct(State, def.cost);
            troop.training = troop.trainTime;
            State.PushNotif($"Training {def.name}... ({troop.trainTime:0}s)");
            OnStateChanged.Invoke();
        }

        public void RestartGame()
        {
            foreach (var c in _upgradeCoroutines.Values)
                if (c != null) StopCoroutine(c);
            _upgradeCoroutines.Clear();
            State = new GameState();
            _dayTimer = 0f;
            OnStateChanged.Invoke();
        }

        // ---- Random Events ----
        void MaybeTriggerEvent()
        {
            if (Random.value > 0.45f) return;

            int roll = Random.Range(0, 7);
            string msg = "";
            switch (roll)
            {
                case 0:
                    State.furnace.fuelRate *= 3f;
                    msg = "BLIZZARD! Furnace burns 3x faster for 30s.";
                    StartCoroutine(ResetFuelRate(30f));
                    break;
                case 1:
                    State.GetResource("food").amount = Mathf.Max(0, State.GetResource("food").amount - 100);
                    msg = "WOLVES: Wolf attack! Lost 100 food.";
                    break;
                case 2:
                    State.GetResource("wood").amount = Mathf.Max(0, State.GetResource("wood").amount - 80);
                    State.GetResource("coal").amount = Mathf.Max(0, State.GetResource("coal").amount - 50);
                    msg = "RAID: Bandit raid! Lost 80 wood & 50 coal.";
                    break;
                case 3:
                    State.GetResource("coal").amount = Mathf.Min(State.GetResource("coal").cap, State.GetResource("coal").amount + 200);
                    msg = "BONUS: Rich coal vein found! +200 coal.";
                    break;
                case 4:
                    State.GetResource("food").amount = Mathf.Min(State.GetResource("food").cap, State.GetResource("food").amount + 300);
                    msg = "BONUS: Miracle harvest! +300 food.";
                    break;
                case 5:
                    State.GetTroop("infantry").count++;
                    msg = "A survivor joins your city! +1 Infantry.";
                    break;
                case 6:
                    State.GetResource("iron").amount = Mathf.Min(State.GetResource("iron").cap, State.GetResource("iron").amount + 150);
                    msg = "BONUS: Iron deposit! +150 iron.";
                    break;
            }

            if (!string.IsNullOrEmpty(msg))
            {
                State.PushNotif(msg);
                OnEvent.Invoke(msg);
                OnStateChanged.Invoke();
            }
        }

        IEnumerator ResetFuelRate(float delay)
        {
            yield return new WaitForSeconds(delay);
            var furnaceDef = Defs.Buildings["furnace"];
            var bldLvl     = State.GetBuilding("furnace").level;
            State.furnace.fuelRate = Mathf.Max(0.08f, 0.3f - bldLvl * 0.02f);
            State.PushNotif("Blizzard subsided.");
            OnStateChanged.Invoke();
        }
    }
}
