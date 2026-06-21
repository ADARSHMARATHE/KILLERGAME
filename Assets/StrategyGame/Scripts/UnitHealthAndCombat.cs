using System;
using UnityEngine;
using UnityEngine.AI;

namespace StrategyGame
{
    public class UnitHealthAndCombat : MonoBehaviour
    {
        public const float RespawnPenaltySeconds = 15f;
        public const float AttackIntervalSeconds = 1.25f;

        [SerializeField] float respawnPenaltySeconds = RespawnPenaltySeconds;
        [SerializeField] float attackIntervalSeconds = AttackIntervalSeconds;
        [SerializeField] int attackRange = 1;

        Unit unit;
        AIPlayerAgent navAgent;
        GridManager grid;
        NavMeshAgent navMeshAgent;
        GridCoordinates respawnPoint;
        Vector3 respawnWorldPosition;
        float attackCooldown;
        bool useNavMeshMode;

        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public int AttackPower { get; private set; }
        public bool IsActiveInArena { get; private set; }
        public float RespawnTimeRemaining { get; private set; }

        public void SetRespawnCountdown(float seconds) => RespawnTimeRemaining = seconds;

        public bool IsAlive => IsActiveInArena && CurrentHealth > 0;

        public event Action<UnitHealthAndCombat, int> Eliminated;

        public void Initialize(int maxHealth, int attackPower, GridCoordinates edgeRespawnPoint, int range = 1)
        {
            useNavMeshMode = false;
            unit = GetComponent<Unit>();
            grid = FindAnyObjectByType<GridManager>();
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackPower = attackPower;
            attackRange = range;
            respawnPoint = edgeRespawnPoint;
            IsActiveInArena = true;
            RespawnTimeRemaining = 0f;
        }

        public void InitializeNavAgent(AIPlayerAgent agent, int maxHealth, int attackPower, Vector3 spawnPosition)
        {
            useNavMeshMode = true;
            navAgent = agent;
            navMeshAgent = GetComponent<NavMeshAgent>();
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackPower = attackPower;
            respawnWorldPosition = spawnPosition;
            IsActiveInArena = true;
            RespawnTimeRemaining = 0f;
        }

        void Update()
        {
            if (useNavMeshMode || EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive || !IsActiveInArena)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f)
            {
                TryAutoAttack();
                attackCooldown = attackIntervalSeconds;
            }
        }

        public int GetEffectiveAttackPower()
        {
            var buffs = GetComponent<UnitBuffs>();
            var multiplier = buffs != null ? buffs.GetAttackPowerMultiplier() : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(AttackPower * multiplier));
        }

        public bool TryAttackUnit(Unit target)
        {
            if (!CanAttack(target))
            {
                return false;
            }

            var targetCombat = target.GetComponent<UnitHealthAndCombat>();
            if (targetCombat == null)
            {
                return false;
            }

            targetCombat.ReceiveDamage(GetEffectiveAttackPower(), unit.PlayerId);
            attackCooldown = attackIntervalSeconds * 0.5f;
            return true;
        }

        public void ReceiveDamage(int amount, int attackerPlayerId)
        {
            if (!IsActiveInArena || amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth <= 0)
            {
                HandleElimination(attackerPlayerId);
            }
        }

        void TryAutoAttack()
        {
            if (grid == null || unit == null || !IsAlive)
            {
                return;
            }

            Unit closestEnemy = null;
            var closestDistance = int.MaxValue;

            foreach (var other in EventGameManager.Instance.GetActiveUnits())
            {
                if (other == unit || other.PlayerId == unit.PlayerId)
                {
                    continue;
                }

                var otherCombat = other.GetComponent<UnitHealthAndCombat>();
                if (otherCombat == null || !otherCombat.IsAlive)
                {
                    continue;
                }

                var distance = other.Position.ManhattanDistance(unit.Position);
                if (distance > attackRange || distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestEnemy = other;
            }

            if (closestEnemy != null)
            {
                TryAttackUnit(closestEnemy);
            }
        }

        bool CanAttack(Unit target)
        {
            if (!IsAlive || target == null || unit == null || grid == null)
            {
                return false;
            }

            var targetCombat = target.GetComponent<UnitHealthAndCombat>();
            if (targetCombat == null || !targetCombat.IsAlive || target.PlayerId == unit.PlayerId)
            {
                return false;
            }

            return target.Position.ManhattanDistance(unit.Position) <= attackRange;
        }

        void HandleElimination(int killerPlayerId)
        {
            IsActiveInArena = false;
            CurrentHealth = 0;
            RespawnTimeRemaining = respawnPenaltySeconds;

            if (useNavMeshMode)
            {
                navAgent?.OnEliminated();
                if (navMeshAgent != null)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.enabled = false;
                }
            }
            else if (grid != null && unit != null && grid.TryGetTile(unit.Position, out var tile) && tile.Occupant == unit)
            {
                tile.SetOccupant(null);
            }

            gameObject.SetActive(false);
            Eliminated?.Invoke(this, killerPlayerId);
        }

        public void RespawnAtEdge()
        {
            if (useNavMeshMode)
            {
                RespawnAtWorldPosition();
                return;
            }

            if (grid == null || unit == null)
            {
                return;
            }

            var spawn = FindOpenEdgeSpawn();
            CurrentHealth = MaxHealth;
            IsActiveInArena = true;
            RespawnTimeRemaining = 0f;
            gameObject.SetActive(true);
            grid.PlaceUnit(unit, spawn);
        }

        void RespawnAtWorldPosition()
        {
            CurrentHealth = MaxHealth;
            IsActiveInArena = true;
            RespawnTimeRemaining = 0f;
            gameObject.SetActive(true);

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.Warp(respawnWorldPosition);
                navMeshAgent.isStopped = false;
            }
            else
            {
                transform.position = respawnWorldPosition;
            }

            navAgent?.OnRespawned();
        }

        GridCoordinates FindOpenEdgeSpawn()
        {
            if (grid.GetTile(respawnPoint)?.IsWalkable == true)
            {
                return respawnPoint;
            }

            foreach (var candidate in EventGameManager.Instance.GetEdgeSpawnCandidates(unit.PlayerId))
            {
                if (grid.GetTile(candidate)?.IsWalkable == true)
                {
                    return candidate;
                }
            }

            return respawnPoint;
        }
    }
}
