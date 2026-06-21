using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame
{
    public enum TurnPhase
    {
        SelectUnit,
        Move,
        Attack,
        EndTurn
    }

    public class TurnManager : MonoBehaviour
    {
        public Team CurrentTeam { get; private set; } = Team.Blue;
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.SelectUnit;
        public Unit SelectedUnit { get; private set; }

        public event Action<Team> OnTurnChanged;
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<Unit> OnUnitSelected;

        GridManager grid;
        readonly List<Unit> allUnits = new();

        public void Initialize(GridManager gridManager, IEnumerable<Unit> units)
        {
            grid = gridManager;
            allUnits.Clear();
            allUnits.AddRange(units);
            BeginTeamTurn(Team.Blue);
        }

        public void RegisterUnit(Unit unit)
        {
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
            }
        }

        public void SelectUnit(Unit unit)
        {
            if (unit == null || unit.Team != CurrentTeam || !unit.CanAct)
            {
                return;
            }

            SelectedUnit = unit;
            OnUnitSelected?.Invoke(unit);
            SetPhase(TurnPhase.Move);
            HighlightMoveOptions();
        }

        public bool TryMoveSelectedUnit(GridCoordinates target)
        {
            if (SelectedUnit == null || CurrentPhase != TurnPhase.Move || SelectedUnit.HasMoved)
            {
                return false;
            }

            if (target.ManhattanDistance(SelectedUnit.Position) > SelectedUnit.MoveRange)
            {
                return false;
            }

            var targetTile = grid.GetTile(target);
            if (targetTile == null || (!target.Equals(SelectedUnit.Position) && !targetTile.IsWalkable))
            {
                return false;
            }

            grid.PlaceUnit(SelectedUnit, target);
            SelectedUnit.MarkMoved();
            SetPhase(TurnPhase.Attack);
            HighlightAttackOptions();
            return true;
        }

        public bool TryAttack(GridCoordinates target)
        {
            if (SelectedUnit == null || CurrentPhase != TurnPhase.Attack || SelectedUnit.HasAttacked)
            {
                return false;
            }

            if (target.ManhattanDistance(SelectedUnit.Position) > SelectedUnit.AttackRange)
            {
                return false;
            }

            var targetTile = grid.GetTile(target);
            if (targetTile?.Occupant == null || targetTile.Occupant.Team == SelectedUnit.Team)
            {
                return false;
            }

            targetTile.Occupant.TakeDamage(SelectedUnit.AttackDamage);
            SelectedUnit.MarkAttacked();
            ClearSelection();
            return true;
        }

        public void SkipAttack()
        {
            if (CurrentPhase == TurnPhase.Attack)
            {
                ClearSelection();
            }
        }

        public void EndTurn()
        {
            ClearSelection();

            var nextTeam = CurrentTeam == Team.Blue ? Team.Red : Team.Blue;
            if (TeamHasAvailableUnits(nextTeam))
            {
                BeginTeamTurn(nextTeam);
                return;
            }

            BeginTeamTurn(CurrentTeam);
        }

        public bool TeamHasAvailableUnits(Team team)
        {
            foreach (var unit in allUnits)
            {
                if (unit.Team == team && unit.IsAlive && unit.CanAct)
                {
                    return true;
                }
            }

            return false;
        }

        public int CountAliveUnits(Team team)
        {
            var count = 0;
            foreach (var unit in allUnits)
            {
                if (unit.Team == team && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        void BeginTeamTurn(Team team)
        {
            CurrentTeam = team;
            foreach (var unit in allUnits)
            {
                if (unit.Team == team && unit.IsAlive)
                {
                    unit.ResetTurn();
                }
            }

            SetPhase(TurnPhase.SelectUnit);
            OnTurnChanged?.Invoke(CurrentTeam);
        }

        void SetPhase(TurnPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        void HighlightMoveOptions()
        {
            grid.ClearHighlights();
            grid.GetTile(SelectedUnit.Position)?.SetHighlight(TileHighlight.Selected);

            foreach (var tile in grid.GetTilesInMoveRange(SelectedUnit.Position, SelectedUnit.MoveRange, SelectedUnit))
            {
                tile.SetHighlight(TileHighlight.Move);
            }
        }

        void HighlightAttackOptions()
        {
            grid.ClearHighlights();
            grid.GetTile(SelectedUnit.Position)?.SetHighlight(TileHighlight.Selected);

            foreach (var tile in grid.GetTilesInAttackRange(SelectedUnit.Position, SelectedUnit.AttackRange))
            {
                if (tile.Occupant.Team != SelectedUnit.Team)
                {
                    tile.SetHighlight(TileHighlight.Attack);
                }
            }
        }

        void ClearSelection()
        {
            SelectedUnit = null;
            grid.ClearHighlights();
            SetPhase(TurnPhase.SelectUnit);
        }
    }
}
