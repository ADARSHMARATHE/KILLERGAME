using UnityEngine;

namespace StrategyGame
{
    public enum SupplyBuffType
    {
        MovementSpeed,
        AttackPower
    }

    public class UnitBuffs : MonoBehaviour
    {
        public const float BuffDurationSeconds = 15f;
        public const float MovementSpeedMultiplier = 1.5f;
        public const float AttackPowerMultiplier = 1.3f;

        float movementBuffEndTime;
        float attackBuffEndTime;

        public float GetMoveSpeedMultiplier() =>
            Time.time < movementBuffEndTime ? MovementSpeedMultiplier : 1f;

        public float GetAttackPowerMultiplier() =>
            Time.time < attackBuffEndTime ? AttackPowerMultiplier : 1f;

        public bool HasActiveBuff => Time.time < movementBuffEndTime || Time.time < attackBuffEndTime;

        public SupplyBuffType ApplyRandomSupplyBuff()
        {
            return Random.value < 0.5f ? ApplyMovementSpeedBuff() : ApplyAttackPowerBuff();
        }

        public SupplyBuffType ApplyMovementSpeedBuff()
        {
            movementBuffEndTime = Time.time + BuffDurationSeconds;
            return SupplyBuffType.MovementSpeed;
        }

        public SupplyBuffType ApplyAttackPowerBuff()
        {
            attackBuffEndTime = Time.time + BuffDurationSeconds;
            return SupplyBuffType.AttackPower;
        }

        public static string GetBuffLabel(SupplyBuffType buffType)
        {
            return buffType switch
            {
                SupplyBuffType.MovementSpeed => "+50% Speed (15s)",
                SupplyBuffType.AttackPower => "+30% Attack (15s)",
                _ => "Supply Buff"
            };
        }

        public static Color GetBuffColor(SupplyBuffType buffType)
        {
            return buffType switch
            {
                SupplyBuffType.MovementSpeed => new Color(0.35f, 0.95f, 0.55f),
                SupplyBuffType.AttackPower => new Color(0.95f, 0.45f, 0.35f),
                _ => Color.white
            };
        }
    }
}
