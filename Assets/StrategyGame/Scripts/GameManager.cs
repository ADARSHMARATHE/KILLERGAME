using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        GridManager grid;
        TurnManager turnManager;
        GameUI gameUI;
        bool gameOver;

        readonly List<Unit> units = new();

        void Awake()
        {
            Instance = this;
        }

        public void Initialize(GridManager gridManager, TurnManager turnManagerRef, GameUI ui)
        {
            grid = gridManager;
            turnManager = turnManagerRef;
            gameUI = ui;

            turnManager.OnTurnChanged += HandleTurnChanged;
            SpawnStartingUnits();
            turnManager.Initialize(grid, units);
            gameUI.UpdateStatus(CurrentTeamLabel(), TurnPhase.SelectUnit);
        }

        void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnChanged -= HandleTurnChanged;
            }
        }

        void SpawnStartingUnits()
        {
            units.Add(CreateUnit(Team.Blue, UnitType.Warrior, new GridCoordinates(1, 1), 12, 3, 1, 4));
            units.Add(CreateUnit(Team.Blue, UnitType.Archer, new GridCoordinates(1, 3), 8, 2, 3, 3));
            units.Add(CreateUnit(Team.Blue, UnitType.Guardian, new GridCoordinates(2, 2), 16, 2, 1, 3));

            units.Add(CreateUnit(Team.Red, UnitType.Warrior, new GridCoordinates(6, 6), 12, 3, 1, 4));
            units.Add(CreateUnit(Team.Red, UnitType.Archer, new GridCoordinates(6, 4), 8, 2, 3, 3));
            units.Add(CreateUnit(Team.Red, UnitType.Guardian, new GridCoordinates(5, 5), 16, 2, 1, 3));
        }

        Unit CreateUnit(Team team, UnitType type, GridCoordinates position, int health, int moveRange, int attackRange, int damage)
        {
            var unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = $"{team}_{type}";
            unitObject.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var unit = unitObject.AddComponent<Unit>();
            unit.Initialize(team, type, position, health, moveRange, attackRange, damage);
            grid.PlaceUnit(unit, position);
            turnManager.RegisterUnit(unit);
            return unit;
        }

        public void HandleTileClicked(GridTile tile)
        {
            if (gameOver || tile == null)
            {
                return;
            }

            switch (turnManager.CurrentPhase)
            {
                case TurnPhase.SelectUnit:
                    if (tile.Occupant != null)
                    {
                        turnManager.SelectUnit(tile.Occupant);
                        gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
                    }
                    break;

                case TurnPhase.Move:
                    if (turnManager.TryMoveSelectedUnit(tile.Coordinates))
                    {
                        gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
                    }
                    break;

                case TurnPhase.Attack:
                    if (turnManager.TryAttack(tile.Coordinates))
                    {
                        CheckWinCondition();
                        gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
                    }
                    break;
            }
        }

        public void HandleEndTurnPressed()
        {
            if (gameOver)
            {
                return;
            }

            turnManager.EndTurn();
            gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
            CheckWinCondition();
        }

        public void HandleSkipAttackPressed()
        {
            if (gameOver)
            {
                return;
            }

            turnManager.SkipAttack();
            gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
        }

        void HandleTurnChanged(Team team)
        {
            gameUI.UpdateStatus(CurrentTeamLabel(), turnManager.CurrentPhase);
        }

        void CheckWinCondition()
        {
            var blueAlive = turnManager.CountAliveUnits(Team.Blue);
            var redAlive = turnManager.CountAliveUnits(Team.Red);

            if (blueAlive == 0 || redAlive == 0)
            {
                gameOver = true;
                var winner = blueAlive > 0 ? "Blue" : redAlive > 0 ? "Red" : "Nobody";
                gameUI.ShowVictory($"{winner} wins!");
            }
        }

        string CurrentTeamLabel()
        {
            return turnManager.CurrentTeam == Team.Blue ? "Blue" : "Red";
        }
    }
}
