using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame
{
    public enum CaptureZoneType
    {
        Fortress,
        Citadel
    }

    [System.Serializable]
    public class CapturePointZone
    {
        public string ZoneName;
        public CaptureZoneType ZoneType;
        public GridCoordinates[] Tiles;

        [System.NonSerialized] public float UncontestedTimer;
        [System.NonSerialized] public int? ControllingPlayerId;
        [System.NonSerialized] public int? CapturingPlayerId;
        [System.NonSerialized] public bool IsGeneratingPoints;
        [System.NonSerialized] public GameObject VisualRoot;
        [System.NonSerialized] public Transform ProgressFill;
        [System.NonSerialized] public Renderer ProgressBackground;

        public float PointMultiplier => ZoneType == CaptureZoneType.Citadel ? 3f : 1f;

        public bool Contains(GridCoordinates coordinates)
        {
            foreach (var tile in Tiles)
            {
                if (tile.Equals(coordinates))
                {
                    return true;
                }
            }

            return false;
        }

        public HashSet<int> GetPlayersPresent(IReadOnlyList<Unit> units)
        {
            var players = new HashSet<int>();
            foreach (var unit in units)
            {
                if (unit != null && unit.IsAlive && Contains(unit.Position))
                {
                    players.Add(unit.PlayerId);
                }
            }

            return players;
        }

        public void Tick(float deltaTime, IReadOnlyList<Unit> units, float captureDelaySeconds)
        {
            var playersPresent = GetPlayersPresent(units);

            if (playersPresent.Count == 0)
            {
                UncontestedTimer = 0f;
                IsGeneratingPoints = false;
                ControllingPlayerId = null;
                CapturingPlayerId = null;
                return;
            }

            if (playersPresent.Count > 1)
            {
                UncontestedTimer = 0f;
                IsGeneratingPoints = false;
                ControllingPlayerId = null;
                CapturingPlayerId = null;
                return;
            }

            foreach (var playerId in playersPresent)
            {
                CapturingPlayerId = playerId;
                UncontestedTimer += deltaTime;
                if (UncontestedTimer >= captureDelaySeconds)
                {
                    ControllingPlayerId = playerId;
                    IsGeneratingPoints = true;
                }

                break;
            }
        }

        public float CaptureProgress(float captureDelaySeconds)
        {
            if (captureDelaySeconds <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(UncontestedTimer / captureDelaySeconds);
        }
    }
}
