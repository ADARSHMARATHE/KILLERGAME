using UnityEngine;
using UnityEngine.AI;

namespace StrategyGame
{
    public class PlayerCharacterController : MonoBehaviour
    {
        public static PlayerCharacterController LocalPlayer { get; private set; }

        [SerializeField] float combatRange = 1.6f;
        [SerializeField] float attackInterval = 0.75f;
        [SerializeField] float moveSampleRadius = 8f;

        AIPlayerAgent playerAgent;
        UnitHealthAndCombat combat;
        NavMeshAgent navAgent;
        Camera mainCamera;
        AIPlayerAgent attackTarget;
        float attackCooldown;
        bool controlEnabled = true;
        GameObject selectionRing;

        public AIPlayerAgent Agent => playerAgent;
        public bool ControlEnabled => controlEnabled;

        void Awake()
        {
            playerAgent = GetComponent<AIPlayerAgent>();
            combat = GetComponent<UnitHealthAndCombat>();
            navAgent = GetComponent<NavMeshAgent>();
            LocalPlayer = this;
            CreateSelectionRing();
        }

        void OnDestroy()
        {
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
            }
        }

        void Start()
        {
            mainCamera = Camera.main;
            CreateNameLabel();
            EventGameManager.Instance?.LeaderboardUI?.UpdateStatus(
                $"You are {PlayerRegistry.GetName(playerAgent.PlayerId)} — move: left-click ground | fight: left-click enemies | F = follow you | G = full map.");
        }

        void Update()
        {
            if (!controlEnabled || combat == null || !combat.IsActiveInArena || !combat.IsAlive ||
                mainCamera == null || EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            if (attackTarget == null)
            {
                attackTarget = FindNearestEnemyInRange();
            }

            HandleInput();
            TickCombat();
        }

        AIPlayerAgent FindNearestEnemyInRange()
        {
            if (EventGameManager.Instance == null)
            {
                return null;
            }

            AIPlayerAgent closest = null;
            var closestDistance = combatRange;
            foreach (var other in EventGameManager.Instance.GetActiveNavAgents())
            {
                if (other == null || other == playerAgent || other.PlayerId == playerAgent.PlayerId || !other.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = other;
                }
            }

            return closest;
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            if (selectionRing != null)
            {
                selectionRing.SetActive(enabled);
            }

            if (!enabled && navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
            else if (enabled && navAgent != null)
            {
                navAgent.isStopped = false;
            }

            attackTarget = null;
        }

        void HandleInput()
        {
            if (StrategyInput.RightClickThisFrame)
            {
                attackTarget = null;
                if (navAgent != null)
                {
                    navAgent.isStopped = true;
                    navAgent.ResetPath();
                }

                return;
            }

            if (!StrategyInput.LeftClickThisFrame)
            {
                return;
            }

            if (!TryGetPointerWorldPoint(out var worldPoint))
            {
                return;
            }

            var enemy = FindEnemyAtPoint(worldPoint);
            if (enemy != null && enemy != playerAgent && enemy.PlayerId != playerAgent.PlayerId && enemy.IsAlive)
            {
                attackTarget = enemy;
                if (IsInCombatRange(enemy))
                {
                    navAgent.isStopped = true;
                    TryAttack(enemy);
                }
                else
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(enemy.transform.position);
                }

                return;
            }

            if (NavMesh.SamplePosition(worldPoint, out var navHit, moveSampleRadius, NavMesh.AllAreas))
            {
                attackTarget = null;
                navAgent.isStopped = false;
                navAgent.SetDestination(navHit.position);
            }
        }

        bool TryGetPointerWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = default;
            if (mainCamera == null)
            {
                return false;
            }

            var ray = mainCamera.ScreenPointToRay(StrategyInput.PointerScreenPosition);
            if (Physics.Raycast(ray, out var hit, 500f))
            {
                worldPoint = hit.point;
                return true;
            }

            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out var distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }

            return false;
        }

        AIPlayerAgent FindEnemyAtPoint(Vector3 worldPoint)
        {
            if (EventGameManager.Instance == null)
            {
                return null;
            }

            AIPlayerAgent closest = null;
            var closestDistance = combatRange;
            foreach (var other in EventGameManager.Instance.GetActiveNavAgents())
            {
                if (other == null || other == playerAgent || other.PlayerId == playerAgent.PlayerId || !other.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(
                    new Vector3(worldPoint.x, other.transform.position.y, worldPoint.z),
                    other.transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = other;
                }
            }

            return closest;
        }

        void TickCombat()
        {
            if (attackTarget == null || !attackTarget.IsAlive)
            {
                if (attackTarget != null)
                {
                    attackTarget = null;
                    navAgent.isStopped = false;
                }

                return;
            }

            if (!IsInCombatRange(attackTarget))
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(attackTarget.transform.position);
                return;
            }

            navAgent.isStopped = true;
            TryAttack(attackTarget);
        }

        bool IsInCombatRange(AIPlayerAgent target)
        {
            return Vector3.Distance(transform.position, target.transform.position) <= combatRange;
        }

        void TryAttack(AIPlayerAgent target)
        {
            if (attackCooldown > 0f || target == null || !target.IsAlive)
            {
                return;
            }

            var targetCombat = target.GetComponent<UnitHealthAndCombat>();
            if (targetCombat == null)
            {
                return;
            }

            targetCombat.ReceiveDamage(combat.GetEffectiveAttackPower(), playerAgent.PlayerId);
            attackCooldown = attackInterval;
        }

        void CreateSelectionRing()
        {
            selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selectionRing.name = "PlayerSelectionRing";
            selectionRing.transform.SetParent(transform, false);
            selectionRing.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            selectionRing.transform.localScale = new Vector3(1.4f, 0.02f, 1.4f);

            var renderer = selectionRing.GetComponent<Renderer>();
            renderer.sharedMaterial = ArenaMaterials.CreateTransparent(new Color(0.3f, 0.85f, 1f, 0.55f));

            var collider = selectionRing.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        void CreateNameLabel()
        {
            var labelObject = new GameObject("PlayerNameLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = PlayerRegistry.GetName(playerAgent != null ? playerAgent.PlayerId : EventGameManager.LocalPlayerId);
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }
    }
}
