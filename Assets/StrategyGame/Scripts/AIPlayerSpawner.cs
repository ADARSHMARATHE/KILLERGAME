using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace StrategyGame
{
    public class AIPlayerSpawner : MonoBehaviour
    {
        [SerializeField] float arenaSize = 100f;
        [SerializeField] float spawnRadius = 44f;
        [SerializeField] float agentHeight = 1f;
        [SerializeField] int maxHealth = 100;
        [SerializeField] int attackPower = 25;

        readonly List<AIPlayerAgent> spawnedAgents = new();

        public IReadOnlyList<AIPlayerAgent> SpawnedAgents => spawnedAgents;

        public void SpawnPlayers(ArenaLayoutGenerator arena, EventGameManager eventManager)
        {
            if (arena != null)
            {
                arenaSize = arena.ArenaSize;
            }

            for (var playerId = 0; playerId < PlayerRegistry.PlayerCount; playerId++)
            {
                var spawnPosition = GetEdgeSpawnPosition(playerId);
                spawnedAgents.Add(CreatePlayer(playerId, spawnPosition, eventManager));
            }
        }

        AIPlayerAgent CreatePlayer(int playerId, Vector3 spawnPosition, EventGameManager eventManager)
        {
            var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = $"{PlayerRegistry.GetName(playerId)}_Agent";
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = spawnPosition;
            var isHumanPlayer = playerId == EventGameManager.LocalPlayerId;
            playerObject.transform.localScale = isHumanPlayer
                ? new Vector3(1.05f, 1.05f, 1.05f)
                : new Vector3(0.9f, 0.9f, 0.9f);

            var collider = playerObject.GetComponent<CapsuleCollider>();
            collider.isTrigger = false;

            var rigidbody = playerObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var agent = playerObject.AddComponent<NavMeshAgent>();
            agent.height = agentHeight;
            agent.radius = 0.4f;
            agent.speed = 6f;
            agent.angularSpeed = 540f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.5f;
            agent.Warp(spawnPosition);

            var playerAgent = playerObject.AddComponent<AIPlayerAgent>();

            var combat = playerObject.AddComponent<UnitHealthAndCombat>();
            combat.InitializeNavAgent(playerAgent, maxHealth, attackPower, spawnPosition);
            if (eventManager != null)
            {
                combat.Eliminated += eventManager.HandleNavAgentEliminated;
                eventManager.RegisterNavAgent(playerAgent);
            }

            playerObject.AddComponent<UnitBuffs>();

            if (isHumanPlayer)
            {
                playerObject.AddComponent<PlayerCharacterController>();
            }
            else
            {
                playerObject.AddComponent<MockCombatAI>();
            }

            playerAgent.Initialize(playerId, spawnPosition);

            return playerAgent;
        }

        Vector3 GetEdgeSpawnPosition(int playerId)
        {
            var angle = playerId / (float)PlayerRegistry.PlayerCount * Mathf.PI * 2f;
            var x = Mathf.Cos(angle) * spawnRadius;
            var z = Mathf.Sin(angle) * spawnRadius;
            var spawn = new Vector3(x, agentHeight, z);

            if (NavMesh.SamplePosition(spawn, out var hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return spawn;
        }
    }
}
