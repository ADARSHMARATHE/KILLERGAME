using UnityEngine;

namespace StrategyGame
{
    public class EventUnitAI : MonoBehaviour
    {
        Unit unit;
        GridManager grid;
        float moveCooldown;
        float moveInterval = 2.5f;

        void Start()
        {
            unit = GetComponent<Unit>();
            grid = FindAnyObjectByType<GridManager>();
        }

        void Update()
        {
            if (unit == null || grid == null || !unit.IsAlive || !unit.IsActiveInArena ||
                EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive)
            {
                return;
            }

            moveCooldown -= Time.deltaTime;
            if (moveCooldown > 0f)
            {
                return;
            }

            var target = ChooseTargetTile();
            if (target.HasValue && !target.Value.Equals(unit.Position) && grid.GetTile(target.Value)?.IsWalkable == true)
            {
                grid.PlaceUnit(unit, target.Value);
            }

            moveCooldown = GetMoveInterval() + Random.Range(0f, 1.5f);
        }

        float GetMoveInterval()
        {
            var buffs = unit.GetComponent<UnitBuffs>();
            var multiplier = buffs != null ? buffs.GetMoveSpeedMultiplier() : 1f;
            return moveInterval / multiplier;
        }

        GridCoordinates? ChooseTargetTile()
        {
            var manager = EventGameManager.Instance;
            CapturePointZone bestZone = null;
            var bestDistance = int.MaxValue;

            foreach (var zone in manager.CaptureZones)
            {
                var zoneCenter = zone.Tiles[0];
                var distance = zoneCenter.ManhattanDistance(unit.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestZone = zone;
                }
            }

            if (bestZone == null)
            {
                return null;
            }

            GridCoordinates? closestWalkable = null;
            var closestDistance = int.MaxValue;
            foreach (var tileCoord in bestZone.Tiles)
            {
                var tile = grid.GetTile(tileCoord);
                if (tile == null || !tile.IsWalkable)
                {
                    continue;
                }

                var distance = tileCoord.ManhattanDistance(unit.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWalkable = tileCoord;
                }
            }

            if (!closestWalkable.HasValue)
            {
                return null;
            }

            var direction = new GridCoordinates(
                Mathf.Clamp(closestWalkable.Value.X - unit.Position.X, -1, 1),
                Mathf.Clamp(closestWalkable.Value.Y - unit.Position.Y, -1, 1));

            var step = new GridCoordinates(unit.Position.X + direction.X, unit.Position.Y + direction.Y);
            return grid.IsInsideGrid(step) ? step : closestWalkable;
        }
    }
}
