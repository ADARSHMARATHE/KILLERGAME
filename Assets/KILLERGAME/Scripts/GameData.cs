using System;
using System.Collections.Generic;

namespace KillerGame
{
    [Serializable]
    public class ResourceState
    {
        public string key;
        public float amount;
        public float cap;
        public float rate; // per second
    }

    [Serializable]
    public class FurnaceState
    {
        public float temp       = 20f;
        public float targetTemp = 20f;
        public float maxTemp    = 40f;
        public float minTemp    = -30f;
        public float fuelPct    = 80f;
        public float fuelRate   = 0.3f; // % per second
        public bool  alive      = true;
    }

    [Serializable]
    public class BuildingState
    {
        public string key;
        public int    level     = 0;
        public bool   upgrading = false;
        public float  progress  = 0f;  // 0-100
    }

    [Serializable]
    public class TroopState
    {
        public string troopKey;
        public int    count     = 0;
        public float  training  = 0f;  // seconds remaining
        public float  trainTime = 10f;
    }

    [Serializable]
    public class HeroState
    {
        public string heroKey;
        public int    level     = 0;
        public bool   upgrading = false;
        public float  progress  = 0f;
    }

    [Serializable]
    public class WildBrawlState
    {
        public bool   active         = false;
        public int    enemyPower     = 0;
        public string enemyName      = "";
        public int    roundsWon      = 0;
        public float  timeRemaining  = 0f;
        public float  duration       = 60f;
        public string lastResult     = "";
        public int    previewGold    = 0;
        public int    previewFood    = 0;
    }

    [Serializable]
    public class GameState
    {
        public int   day    = 1;
        public float timer  = 0f;
        public bool  gameOver = false;

        public List<ResourceState> resources = new List<ResourceState>
        {
            new ResourceState { key="food",  amount=500,  cap=2000, rate=0 },
            new ResourceState { key="wood",  amount=300,  cap=2000, rate=0 },
            new ResourceState { key="coal",  amount=200,  cap=1000, rate=0 },
            new ResourceState { key="iron",  amount=50,   cap=500,  rate=0 },
            new ResourceState { key="gold",  amount=20,   cap=200,  rate=0 },
        };

        public FurnaceState furnace = new FurnaceState();

        public List<BuildingState> buildings = new List<BuildingState>
        {
            new BuildingState { key="furnace",  level=1 },
            new BuildingState { key="sawmill",  level=0 },
            new BuildingState { key="coalmine", level=0 },
            new BuildingState { key="farm",     level=0 },
            new BuildingState { key="ironmine", level=0 },
            new BuildingState { key="barracks", level=0 },
            new BuildingState { key="market",   level=0 },
        };

        public List<TroopState> troops = new List<TroopState>
        {
            new TroopState { troopKey="infantry",  count=0, trainTime=10f },
            new TroopState { troopKey="cavalry",   count=0, trainTime=20f },
            new TroopState { troopKey="marksmen",  count=0, trainTime=30f },
        };

        public List<HeroState> heroes = new List<HeroState>
        {
            new HeroState { heroKey="scout" },
            new HeroState { heroKey="commander" },
            new HeroState { heroKey="engineer" },
        };

        public List<string> notifications = new List<string>();

        public WildBrawlState wildBrawl = new WildBrawlState();

        public ResourceState GetResource(string key)
        {
            return resources.Find(r => r.key == key);
        }

        public BuildingState GetBuilding(string key)
        {
            return buildings.Find(b => b.key == key);
        }

        public TroopState GetTroop(string key)
        {
            return troops.Find(t => t.troopKey == key);
        }

        public HeroState GetHero(string key)
        {
            return heroes.Find(h => h.heroKey == key);
        }

        public void PushNotif(string msg)
        {
            notifications.Insert(0, msg);
            if (notifications.Count > 10)
                notifications.RemoveAt(notifications.Count - 1);
        }
    }
}
