using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrategyGame
{
    public class EventGameManager : MonoBehaviour
    {
        public static EventGameManager Instance { get; private set; }

        public const int LocalPlayerId = 0;
        public const float MatchDurationSeconds = 600f;
        public const float QuickTestMatchDurationSeconds = 90f;
        public const float CaptureDelaySeconds = 10f;
        public const float BasePointsPerSecond = 1f;
        public const float EliminationBonusPoints = 25f;

        [SerializeField] bool useQuickTestMatch = true;
        [SerializeField] float quickTestMatchDurationSeconds = QuickTestMatchDurationSeconds;
        [SerializeField] float fullMatchDurationSeconds = MatchDurationSeconds;
        [SerializeField] float captureDelaySeconds = CaptureDelaySeconds;
        [SerializeField] float basePointsPerSecond = BasePointsPerSecond;
        [SerializeField] float eliminationBonusPoints = EliminationBonusPoints;
        [SerializeField] bool useNavMeshAgents = true;

        public CapturePointZone[] CaptureZones { get; private set; }
        public float RemainingTime { get; private set; }
        public bool MatchActive { get; private set; }
        public EventLeaderboardUI LeaderboardUI => leaderboardUI;

        readonly List<Unit> units = new();
        readonly List<AIPlayerAgent> navAgents = new();
        readonly float[] playerScores = new float[PlayerRegistry.PlayerCount];
        readonly int[] playerEliminations = new int[PlayerRegistry.PlayerCount];
        readonly GridCoordinates[] edgeSpawnPoints = new GridCoordinates[PlayerRegistry.PlayerCount];
        GridManager grid;
        EventLeaderboardUI leaderboardUI;

        public event Action MatchEnded;

        public struct PlayerScoreEntry
        {
            public int PlayerId;
            public string PlayerName;
            public float Score;
            public int Eliminations;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(GridManager gridManager, EventLeaderboardUI ui)
        {
            grid = gridManager;
            leaderboardUI = ui;
            RemainingTime = useQuickTestMatch ? quickTestMatchDurationSeconds : fullMatchDurationSeconds;
            MatchActive = true;

            CaptureZones = CreateCaptureZones();
            if (!useNavMeshAgents)
            {
                BuildZoneVisuals();
                SpawnFreeForAllUnits();
            }

            if (!useNavMeshAgents)
            {
                leaderboardUI.BuildZoneProgressBars(CaptureZones);
            }

            RefreshLeaderboard();
            leaderboardUI.UpdateTimer(RemainingTime);
            var matchLabel = useQuickTestMatch ? "Quick test match (90s)" : "Full match (10 min)";
            if (useNavMeshAgents)
            {
                leaderboardUI.UpdateStatus(
                    $"{matchLabel} — You are {PlayerRegistry.GetName(LocalPlayerId)}. Stand in a capture circle alone for 10s to score. Citadel = 3× points.");
            }
            else
            {
                leaderboardUI.UpdateStatus(
                    $"{matchLabel} — You are {PlayerRegistry.GetName(LocalPlayerId)}. Move on blue tiles, attack adjacent enemies.");
            }
        }

        public IReadOnlyList<Unit> GetActiveUnits() => units;

        public IReadOnlyList<AIPlayerAgent> GetActiveNavAgents() => navAgents;

        public void RegisterNavAgent(AIPlayerAgent agent)
        {
            if (agent != null && !navAgents.Contains(agent))
            {
                navAgents.Add(agent);
            }
        }

        public void HandleNavAgentEliminated(UnitHealthAndCombat victimCombat, int killerPlayerId)
        {
            HandleUnitEliminated(victimCombat, killerPlayerId);
        }

        public int GetEliminations(int playerId) => playerEliminations[Mathf.Clamp(playerId, 0, PlayerRegistry.PlayerCount - 1)];

        public GridCoordinates GetEdgeSpawnPoint(int playerId) =>
            edgeSpawnPoints[Mathf.Clamp(playerId, 0, PlayerRegistry.PlayerCount - 1)];

        public GridCoordinates[] GetEdgeSpawnCandidates(int playerId)
        {
            var primary = GetEdgeSpawnPoint(playerId);
            return new[]
            {
                primary,
                new GridCoordinates(primary.X + 1, primary.Y),
                new GridCoordinates(primary.X - 1, primary.Y),
                new GridCoordinates(primary.X, primary.Y + 1),
                new GridCoordinates(primary.X, primary.Y - 1)
            };
        }

        public void HandleUnitEliminated(UnitHealthAndCombat victimCombat, int killerPlayerId)
        {
            if (victimCombat == null)
            {
                return;
            }

            var victim = victimCombat.GetComponent<Unit>();
            var navVictim = victimCombat.GetComponent<AIPlayerAgent>();
            var victimPlayerId = victim != null ? victim.PlayerId : navVictim != null ? navVictim.PlayerId : -1;

            if (victimPlayerId < 0 || killerPlayerId < 0 || killerPlayerId >= PlayerRegistry.PlayerCount)
            {
                return;
            }

            if (killerPlayerId == victimPlayerId)
            {
                return;
            }

            playerEliminations[killerPlayerId]++;
            playerScores[killerPlayerId] += eliminationBonusPoints;
            RefreshLeaderboard();
            StartCoroutine(RespawnUnitAfterPenalty(victimCombat));
        }

        IEnumerator RespawnUnitAfterPenalty(UnitHealthAndCombat combat)
        {
            if (combat == null)
            {
                yield break;
            }

            combat.SetRespawnCountdown(UnitHealthAndCombat.RespawnPenaltySeconds);
            var remaining = UnitHealthAndCombat.RespawnPenaltySeconds;
            while (remaining > 0f && MatchActive)
            {
                remaining -= Time.deltaTime;
                combat.SetRespawnCountdown(Mathf.Max(0f, remaining));
                yield return null;
            }

            if (MatchActive && combat != null)
            {
                combat.RespawnAtEdge();
            }
        }

        void Update()
        {
            if (!MatchActive)
            {
                return;
            }

            RemainingTime -= Time.deltaTime;
            leaderboardUI.UpdateTimer(Mathf.Max(0f, RemainingTime));

            if (!useNavMeshAgents)
            {
                TickCaptureZones();
                AwardCapturePoints();
                UpdateZoneProgressBars();
            }

            RefreshLeaderboard();

            if (RemainingTime <= 0f)
            {
                EndMatch();
            }
        }

        public void RegisterUnit(Unit unit)
        {
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
            }
        }

        public IReadOnlyList<PlayerScoreEntry> GetRankedScores()
        {
            var entries = new List<PlayerScoreEntry>(PlayerRegistry.PlayerCount);
            for (var i = 0; i < PlayerRegistry.PlayerCount; i++)
            {
                entries.Add(new PlayerScoreEntry
                {
                    PlayerId = i,
                    PlayerName = PlayerRegistry.GetName(i),
                    Score = playerScores[i],
                    Eliminations = playerEliminations[i]
                });
            }

            entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            return entries;
        }

        CapturePointZone[] CreateCaptureZones()
        {
            return new[]
            {
                new CapturePointZone
                {
                    ZoneName = "Central Citadel",
                    ZoneType = CaptureZoneType.Citadel,
                    Tiles = new[]
                    {
                        new GridCoordinates(5, 5),
                        new GridCoordinates(5, 6),
                        new GridCoordinates(6, 5),
                        new GridCoordinates(6, 6)
                    }
                },
                new CapturePointZone
                {
                    ZoneName = "Northwest Fortress",
                    ZoneType = CaptureZoneType.Fortress,
                    Tiles = TileBlock(0, 0, 2, 2)
                },
                new CapturePointZone
                {
                    ZoneName = "Northeast Fortress",
                    ZoneType = CaptureZoneType.Fortress,
                    Tiles = TileBlock(10, 0, 2, 2)
                },
                new CapturePointZone
                {
                    ZoneName = "Southwest Fortress",
                    ZoneType = CaptureZoneType.Fortress,
                    Tiles = TileBlock(0, 10, 2, 2)
                },
                new CapturePointZone
                {
                    ZoneName = "Southeast Fortress",
                    ZoneType = CaptureZoneType.Fortress,
                    Tiles = TileBlock(10, 10, 2, 2)
                }
            };
        }

        static GridCoordinates[] TileBlock(int startX, int startY, int width, int height)
        {
            var tiles = new GridCoordinates[width * height];
            var index = 0;
            for (var y = startY; y < startY + height; y++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    tiles[index++] = new GridCoordinates(x, y);
                }
            }

            return tiles;
        }

        void BuildZoneVisuals()
        {
            foreach (var zone in CaptureZones)
            {
                zone.VisualRoot = new GameObject(zone.ZoneName);
                zone.VisualRoot.transform.SetParent(transform, false);

                var zoneColor = zone.ZoneType == CaptureZoneType.Citadel
                    ? new Color(0.95f, 0.75f, 0.15f, 0.35f)
                    : new Color(0.55f, 0.65f, 0.85f, 0.30f);

                foreach (var tileCoord in zone.Tiles)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = $"{zone.ZoneName}_Marker";
                    marker.transform.SetParent(zone.VisualRoot.transform, false);
                    marker.transform.position = grid.GridToWorld(tileCoord) + Vector3.up * 0.05f;
                    marker.transform.localScale = new Vector3(0.85f, 0.04f, 0.85f);

                    var renderer = marker.GetComponent<Renderer>();
                    renderer.material.color = zoneColor;

                    var collider = marker.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }
                }

                BuildZoneProgressBar(zone);
            }
        }

        void BuildZoneProgressBar(CapturePointZone zone)
        {
            var center = Vector3.zero;
            foreach (var tileCoord in zone.Tiles)
            {
                center += grid.GridToWorld(tileCoord);
            }

            center /= zone.Tiles.Length;
            center += Vector3.up * 0.55f;

            var barRoot = new GameObject($"{zone.ZoneName}_ProgressBar");
            barRoot.transform.SetParent(zone.VisualRoot.transform, false);
            barRoot.transform.position = center;
            barRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(barRoot.transform, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(1.4f, 0.12f, 0.08f);
            zone.ProgressBackground = background.GetComponent<Renderer>();
            zone.ProgressBackground.material.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            Destroy(background.GetComponent<Collider>());

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(barRoot.transform, false);
            fill.transform.localPosition = new Vector3(-0.7f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(0.001f, 0.1f, 0.06f);
            var fillRenderer = fill.GetComponent<Renderer>();
            fillRenderer.material.color = zone.ZoneType == CaptureZoneType.Citadel
                ? new Color(0.95f, 0.75f, 0.15f, 0.95f)
                : new Color(0.45f, 0.65f, 0.95f, 0.95f);
            Destroy(fill.GetComponent<Collider>());
            zone.ProgressFill = fill.transform;
        }

        void SpawnFreeForAllUnits()
        {
            var spawnPoints = new[]
            {
                new GridCoordinates(2, 2),
                new GridCoordinates(9, 2),
                new GridCoordinates(2, 9),
                new GridCoordinates(9, 9),
                new GridCoordinates(2, 5),
                new GridCoordinates(9, 5),
                new GridCoordinates(5, 2),
                new GridCoordinates(5, 9),
                new GridCoordinates(3, 5),
                new GridCoordinates(8, 5)
            };

            for (var playerId = 0; playerId < PlayerRegistry.PlayerCount; playerId++)
            {
                edgeSpawnPoints[playerId] = spawnPoints[playerId];
                var unit = CreateUnit(playerId, spawnPoints[playerId]);
                units.Add(unit);
            }
        }

        Unit CreateUnit(int playerId, GridCoordinates position)
        {
            var unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = $"{PlayerRegistry.GetName(playerId)}_Unit";
            unitObject.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            var unit = unitObject.AddComponent<Unit>();
            unit.InitializeForEvent(playerId, position, maxHealth: 100, moveRange: 4, attackRange: 1, attackDamage: 25);
            grid.PlaceUnit(unit, position);

            var combat = unitObject.AddComponent<UnitHealthAndCombat>();
            combat.Initialize(maxHealth: 100, attackPower: 25, edgeRespawnPoint: edgeSpawnPoints[playerId], range: 1);
            combat.Eliminated += HandleUnitEliminated;
            unitObject.AddComponent<UnitBuffs>();

            if (playerId != LocalPlayerId)
            {
                unitObject.AddComponent<EventUnitAI>();
            }

            RegisterUnit(unit);
            return unit;
        }

        void TickCaptureZones()
        {
            foreach (var zone in CaptureZones)
            {
                zone.Tick(Time.deltaTime, units, captureDelaySeconds);
                UpdateZoneVisual(zone);
            }
        }

        void UpdateZoneVisual(CapturePointZone zone)
        {
            if (zone.VisualRoot == null)
            {
                return;
            }

            var progress = zone.CaptureProgress(captureDelaySeconds);
            var scaleY = 0.04f + progress * 0.12f;
            var ownerColor = zone.ControllingPlayerId.HasValue
                ? PlayerRegistry.GetColor(zone.ControllingPlayerId.Value)
                : zone.ZoneType == CaptureZoneType.Citadel
                    ? new Color(0.95f, 0.75f, 0.15f, 0.35f)
                    : new Color(0.55f, 0.65f, 0.85f, 0.30f);

            ownerColor.a = zone.IsGeneratingPoints ? 0.65f : 0.30f;

            foreach (Transform child in zone.VisualRoot.transform)
            {
                if (child.name.EndsWith("_ProgressBar"))
                {
                    continue;
                }

                child.localScale = new Vector3(0.85f, scaleY, 0.85f);
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = ownerColor;
                }
            }

            UpdateWorldProgressBar(zone, progress);
        }

        void UpdateWorldProgressBar(CapturePointZone zone, float progress)
        {
            if (zone.ProgressFill == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(progress);
            zone.ProgressFill.localScale = new Vector3(Mathf.Max(0.001f, 1.4f * clamped), 0.1f, 0.06f);
            zone.ProgressFill.localPosition = new Vector3(-0.7f + 0.7f * clamped, 0f, -0.01f);

            if (zone.ProgressFill.TryGetComponent<Renderer>(out var fillRenderer))
            {
                var fillColor = zone.IsGeneratingPoints && zone.ControllingPlayerId.HasValue
                    ? PlayerRegistry.GetColor(zone.ControllingPlayerId.Value)
                    : zone.CapturingPlayerId.HasValue
                        ? PlayerRegistry.GetColor(zone.CapturingPlayerId.Value)
                        : zone.ZoneType == CaptureZoneType.Citadel
                            ? new Color(0.95f, 0.75f, 0.15f, 0.95f)
                            : new Color(0.45f, 0.65f, 0.95f, 0.95f);
                fillRenderer.material.color = fillColor;
            }
        }

        void UpdateZoneProgressBars()
        {
            foreach (var zone in CaptureZones)
            {
                leaderboardUI.UpdateZoneProgress(zone, zone.CaptureProgress(captureDelaySeconds));
            }
        }

        void AwardCapturePoints()
        {
            foreach (var zone in CaptureZones)
            {
                if (!zone.IsGeneratingPoints || !zone.ControllingPlayerId.HasValue)
                {
                    continue;
                }

                var rate = basePointsPerSecond * zone.PointMultiplier;
                playerScores[zone.ControllingPlayerId.Value] += rate * Time.deltaTime;
            }
        }

        public void AddCaptureScore(int playerId, float amount)
        {
            if (playerId < 0 || playerId >= PlayerRegistry.PlayerCount)
            {
                return;
            }

            playerScores[playerId] += amount;
        }

        public void RefreshLeaderboardPublic() => RefreshLeaderboard();

        void RefreshLeaderboard()
        {
            leaderboardUI.UpdateLeaderboard(GetRankedScores());
        }

        void EndMatch()
        {
            MatchActive = false;
            var ranked = GetRankedScores().ToList();
            var winner = ranked[0];
            var message = ranked.Count > 1 && Mathf.Approximately(winner.Score, ranked[1].Score)
                ? "Match ended in a tie!"
                : $"{winner.PlayerName} wins with {winner.Score:0} points!";

            leaderboardUI.UpdateStatus("Match over");
            leaderboardUI.ShowFinalResults(message);
            MatchEnded?.Invoke();
        }

        public bool IsCaptureZoneTile(GridCoordinates coordinates)
        {
            foreach (var zone in CaptureZones)
            {
                if (zone.Contains(coordinates))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
