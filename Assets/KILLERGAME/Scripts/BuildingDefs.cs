using System.Collections.Generic;
using UnityEngine;

namespace KillerGame
{
    public struct ResourceCost
    {
        public float food, wood, coal, iron, gold;
    }

    public class BuildingDef
    {
        public string key;
        public string name;
        public string icon;
        public string desc;
        public int    maxLevel;

        public System.Func<int, ResourceCost>   UpgradeCost;
        public float                             upgradeTimePerLevel; // seconds per level
        public System.Action<GameState, int>     OnUpgrade;
    }

    public class TroopDef
    {
        public string key;
        public string name;
        public string icon;
        public int    power;
        public ResourceCost cost;
        public string requiresBuilding;
    }

    public class HeroDef
    {
        public string key;
        public string name;
        public string icon;
        public string desc;
        public int    maxLevel;
        public System.Func<int, ResourceCost> UpgradeCost;
        public float  upgradeTimePerLevel;
    }

    public static class Defs
    {
        public static readonly Dictionary<string, BuildingDef> Buildings = new Dictionary<string, BuildingDef>
        {
            ["furnace"] = new BuildingDef {
                key="furnace", name="Furnace", icon="FIRE", desc="Keeps everyone alive. Never let it die.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { wood=lvl*100, coal=lvl*80, iron=lvl*30 },
                upgradeTimePerLevel = 15f,
                OnUpgrade = (s, lvl) => {
                    s.furnace.maxTemp  = 40 + lvl * 5;
                    s.furnace.fuelRate = Mathf.Max(0.08f, 0.3f - lvl * 0.02f);
                    s.GetResource("coal").cap = 1000 + lvl * 500;
                }
            },
            ["sawmill"] = new BuildingDef {
                key="sawmill", name="Sawmill", icon="SAW", desc="Produces 2 wood/sec per level.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { wood=50, coal=lvl*40, iron=lvl*10 },
                upgradeTimePerLevel = 10f,
                OnUpgrade = (s, lvl) => {
                    s.GetResource("wood").cap  += 300f;
                }
            },
            ["coalmine"] = new BuildingDef {
                key="coalmine", name="Coal Mine", icon="MINE", desc="Produces 1.5 coal/sec per level.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { wood=lvl*80, coal=50, iron=lvl*20 },
                upgradeTimePerLevel = 12f,
                OnUpgrade = (s, lvl) => { }
            },
            ["farm"] = new BuildingDef {
                key="farm", name="Farm", icon="FARM", desc="Produces 2 food/sec per level.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { food=50, wood=lvl*60, coal=lvl*20 },
                upgradeTimePerLevel = 8f,
                OnUpgrade = (s, lvl) => {
                    s.GetResource("food").cap  += 500f;
                }
            },
            ["ironmine"] = new BuildingDef {
                key="ironmine", name="Iron Mine", icon="IRON", desc="Produces 1 iron/sec per level.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { wood=lvl*100, coal=lvl*60, iron=20 },
                upgradeTimePerLevel = 15f,
                OnUpgrade = (s, lvl) => {
                    s.GetResource("iron").cap  += 100f;
                }
            },
            ["barracks"] = new BuildingDef {
                key="barracks", name="Barracks", icon="FORT", desc="Train soldiers to defend your city.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { food=lvl*100, wood=lvl*80, coal=lvl*40, iron=lvl*50 },
                upgradeTimePerLevel = 20f,
                OnUpgrade = (s, lvl) => {
                    s.GetTroop("infantry").trainTime  = Mathf.Max(3f,  10f - lvl);
                    s.GetTroop("cavalry").trainTime   = Mathf.Max(8f,  20f - lvl);
                    s.GetTroop("marksmen").trainTime  = Mathf.Max(12f, 30f - lvl);
                }
            },
            ["market"] = new BuildingDef {
                key="market", name="Market", icon="GOLD", desc="Produces 0.5 gold/sec per level.",
                maxLevel=10,
                UpgradeCost = lvl => new ResourceCost { food=lvl*50, wood=lvl*50, coal=lvl*30, iron=lvl*20, gold=5 },
                upgradeTimePerLevel = 10f,
                OnUpgrade = (s, lvl) => {
                    s.GetResource("gold").cap  += 50f;
                }
            },
        };

        public static readonly Dictionary<string, TroopDef> Troops = new Dictionary<string, TroopDef>
        {
            ["infantry"] = new TroopDef {
                key="infantry", name="Infantry", icon="INF", power=10,
                cost=new ResourceCost { food=20, iron=10 },
                requiresBuilding="barracks"
            },
            ["cavalry"] = new TroopDef {
                key="cavalry", name="Cavalry", icon="CAV", power=25,
                cost=new ResourceCost { food=40, iron=20, gold=2 },
                requiresBuilding="barracks"
            },
            ["marksmen"] = new TroopDef {
                key="marksmen", name="Marksmen", icon="MRK", power=20,
                cost=new ResourceCost { food=30, wood=20, iron=15, gold=1 },
                requiresBuilding="barracks"
            },
        };

        public static readonly Dictionary<string, HeroDef> Heroes = new Dictionary<string, HeroDef>
        {
            ["scout"] = new HeroDef {
                key="scout", name="Scout", icon="SCOUT",
                desc="Increases resource collection speed by 10% per level.",
                maxLevel=5,
                UpgradeCost = lvl => new ResourceCost { food=lvl*50, wood=lvl*80, gold=lvl*5 },
                upgradeTimePerLevel = 20f
            },
            ["commander"] = new HeroDef {
                key="commander", name="Commander", icon="CMD",
                desc="Reduces troop training time by 15% per level.",
                maxLevel=5,
                UpgradeCost = lvl => new ResourceCost { food=lvl*80, iron=lvl*40, gold=lvl*8 },
                upgradeTimePerLevel = 25f
            },
            ["engineer"] = new HeroDef {
                key="engineer", name="Engineer", icon="ENG",
                desc="Reduces building upgrade costs by 5% per level.",
                maxLevel=5,
                UpgradeCost = lvl => new ResourceCost { wood=lvl*100, coal=lvl*60, iron=lvl*20 },
                upgradeTimePerLevel = 30f
            },
        };

        public static bool CanAfford(GameState s, ResourceCost cost)
        {
            return s.GetResource("food").amount >= cost.food
                && s.GetResource("wood").amount >= cost.wood
                && s.GetResource("coal").amount >= cost.coal
                && s.GetResource("iron").amount >= cost.iron
                && s.GetResource("gold").amount >= cost.gold;
        }

        public static void Deduct(GameState s, ResourceCost cost)
        {
            s.GetResource("food").amount -= cost.food;
            s.GetResource("wood").amount -= cost.wood;
            s.GetResource("coal").amount -= cost.coal;
            s.GetResource("iron").amount -= cost.iron;
            s.GetResource("gold").amount -= cost.gold;
        }
    }
}
