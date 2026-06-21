using UnityEngine;

namespace StrategyGame
{
    public class AIPlayerAgent : MonoBehaviour
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public Vector3 SpawnPosition { get; private set; }

        UnitHealthAndCombat combat;
        MockCombatAI combatAI;
        PlayerCharacterController playerController;

        public bool IsActiveInArena => combat != null && combat.IsActiveInArena;
        public bool IsAlive => combat != null && combat.IsAlive;
        public bool IsHumanControlled => playerController != null;

        void Awake()
        {
            combat = GetComponent<UnitHealthAndCombat>();
            combatAI = GetComponent<MockCombatAI>();
            playerController = GetComponent<PlayerCharacterController>();
        }

        public void Initialize(int playerId, Vector3 spawnPosition)
        {
            PlayerId = playerId;
            PlayerName = PlayerRegistry.GetName(playerId);
            SpawnPosition = spawnPosition;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ArenaMaterials.CreateOpaque(PlayerRegistry.GetColor(playerId));
            }
        }

        public void OnEliminated()
        {
            combatAI?.SetEnabled(false);
            playerController?.SetControlEnabled(false);
        }

        public void OnRespawned()
        {
            if (playerController != null)
            {
                playerController.SetControlEnabled(true);
                return;
            }

            if (combatAI == null)
            {
                return;
            }

            combatAI.SetEnabled(true);
            combatAI.ResetBehavior();
        }
    }
}
