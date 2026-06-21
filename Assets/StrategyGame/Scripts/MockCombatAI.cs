using UnityEngine;
using UnityEngine.AI;

namespace StrategyGame
{
    public class MockCombatAI : MonoBehaviour
    {
        enum AIState
        {
            SeekObjective,
            Capturing,
            Fighting
        }

        [SerializeField] float capturePauseSeconds = 3f;
        [SerializeField] float combatRange = 1.6f;
        [SerializeField] float combatAttackInterval = 0.8f;
        [SerializeField] float objectiveRecheckSeconds = 8f;

        AIPlayerAgent playerAgent;
        UnitHealthAndCombat combat;
        NavMeshAgent navAgent;
        ArenaLayoutGenerator arena;

        AIState state = AIState.SeekObjective;
        Transform currentObjective;
        float captureTimer;
        float attackCooldown;
        float objectiveTimer;
        AIPlayerAgent combatTarget;
        bool enabledAI = true;

        public void SetEnabled(bool value) => enabledAI = value;

        public void ResetBehavior()
        {
            state = AIState.SeekObjective;
            currentObjective = null;
            captureTimer = 0f;
            combatTarget = null;
            PickRandomObjective();
        }

        void Awake()
        {
            playerAgent = GetComponent<AIPlayerAgent>();
            combat = GetComponent<UnitHealthAndCombat>();
            navAgent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            arena = FindAnyObjectByType<ArenaLayoutGenerator>();
            PickRandomObjective();
        }

        void Update()
        {
            if (!enabledAI || combat == null || !combat.IsActiveInArena || !combat.IsAlive ||
                EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            objectiveTimer -= Time.deltaTime;

            var opponent = FindNearbyOpponent();
            if (opponent != null)
            {
                EnterCombat(opponent);
            }

            switch (state)
            {
                case AIState.Fighting:
                    TickCombat();
                    break;
                case AIState.Capturing:
                    TickCapture();
                    break;
                default:
                    TickSeek();
                    break;
            }
        }

        void TickSeek()
        {
            if (objectiveTimer <= 0f)
            {
                PickRandomObjective();
                objectiveTimer = objectiveRecheckSeconds;
            }

            if (currentObjective == null || navAgent == null)
            {
                return;
            }

            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.25f)
            {
                if (IsInsideCaptureZone())
                {
                    state = AIState.Capturing;
                    captureTimer = capturePauseSeconds;
                    navAgent.isStopped = true;
                }
                else
                {
                    PickRandomObjective();
                }
            }
        }

        void TickCapture()
        {
            navAgent.isStopped = true;
            captureTimer -= Time.deltaTime;
            if (captureTimer <= 0f)
            {
                state = AIState.SeekObjective;
                navAgent.isStopped = false;
                PickRandomObjective();
            }
        }

        void TickCombat()
        {
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }

            if (combatTarget == null || !combatTarget.IsAlive)
            {
                ExitCombat();
                return;
            }

            var distance = Vector3.Distance(transform.position, combatTarget.transform.position);
            if (distance > combatRange * 1.5f)
            {
                ExitCombat();
                return;
            }

            if (attackCooldown > 0f)
            {
                return;
            }

            var targetCombat = combatTarget.GetComponent<UnitHealthAndCombat>();
            if (targetCombat != null)
            {
                targetCombat.ReceiveDamage(combat.GetEffectiveAttackPower(), playerAgent.PlayerId);
            }

            attackCooldown = combatAttackInterval;
        }

        void EnterCombat(AIPlayerAgent opponent)
        {
            combatTarget = opponent;
            state = AIState.Fighting;
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
        }

        void ExitCombat()
        {
            combatTarget = null;
            state = AIState.SeekObjective;
            if (navAgent != null)
            {
                navAgent.isStopped = false;
            }

            PickRandomObjective();
        }

        AIPlayerAgent FindNearbyOpponent()
        {
            foreach (var other in EventGameManager.Instance.GetActiveNavAgents())
            {
                if (other == null || other == playerAgent || other.PlayerId == playerAgent.PlayerId || !other.IsAlive)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, other.transform.position) <= combatRange)
                {
                    return other;
                }
            }

            return null;
        }

        bool IsInsideCaptureZone()
        {
            foreach (var trigger in FindObjectsByType<CaptureRadiusTrigger>(FindObjectsInactive.Exclude))
            {
                var distance = Vector3.Distance(
                    new Vector3(transform.position.x, 0f, transform.position.z),
                    new Vector3(trigger.transform.position.x, 0f, trigger.transform.position.z));

                if (distance <= trigger.CaptureRadius)
                {
                    return true;
                }
            }

            if (arena != null && arena.CentralCitadel != null)
            {
                var citadelPos = arena.CentralCitadel.transform.position;
                var distance = Vector3.Distance(
                    new Vector3(transform.position.x, 0f, transform.position.z),
                    new Vector3(citadelPos.x, 0f, citadelPos.z));

                if (distance <= 5f)
                {
                    return true;
                }
            }

            return false;
        }

        void PickRandomObjective()
        {
            if (arena == null)
            {
                arena = FindAnyObjectByType<ArenaLayoutGenerator>();
            }

            if (arena == null)
            {
                return;
            }

            var options = new System.Collections.Generic.List<Transform>(5);
            if (arena.CentralCitadel != null)
            {
                options.Add(arena.CentralCitadel.transform);
            }

            foreach (var fortress in arena.PeripheralFortresses)
            {
                if (fortress != null)
                {
                    options.Add(fortress.transform);
                }
            }

            if (options.Count == 0)
            {
                return;
            }

            currentObjective = options[Random.Range(0, options.Count)];
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(currentObjective.position);
            }
        }
    }
}
