using UnityEngine;

namespace StrategyGame
{
    public enum UnitType
    {
        Warrior,
        Archer,
        Guardian
    }

    public class Unit : MonoBehaviour
    {
        public Team Team { get; private set; }
        public int PlayerId { get; private set; }
        public UnitType Type { get; private set; }
        public GridCoordinates Position { get; private set; }
        public int MaxHealth { get; private set; }
        public int Health { get; private set; }
        public int MoveRange { get; private set; }
        public int AttackRange { get; private set; }
        public int AttackDamage { get; private set; }
        public bool HasMoved { get; private set; }
        public bool HasAttacked { get; private set; }

        Renderer bodyRenderer;
        TextMesh label;

        public bool IsAlive
        {
            get
            {
                var combat = GetComponent<UnitHealthAndCombat>();
                if (combat != null)
                {
                    return combat.IsAlive;
                }

                return Health > 0;
            }
        }

        public bool IsActiveInArena
        {
            get
            {
                var combat = GetComponent<UnitHealthAndCombat>();
                return combat == null || combat.IsActiveInArena;
            }
        }

        public bool CanAct => IsAlive && IsActiveInArena && (!HasMoved || !HasAttacked);

        public void Initialize(
            Team team,
            UnitType type,
            GridCoordinates position,
            int maxHealth,
            int moveRange,
            int attackRange,
            int attackDamage)
        {
            Team = team;
            Type = type;
            Position = position;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MoveRange = moveRange;
            AttackRange = attackRange;
            AttackDamage = attackDamage;

            bodyRenderer = GetComponent<Renderer>();
            bodyRenderer.material.color = team == Team.Blue
                ? new Color(0.2f, 0.45f, 0.95f)
                : new Color(0.9f, 0.25f, 0.25f);

            label = CreateLabel(transform, Type.ToString()[0].ToString());
        }

        public void InitializeForEvent(
            int playerId,
            GridCoordinates position,
            int maxHealth,
            int moveRange,
            int attackRange,
            int attackDamage)
        {
            PlayerId = playerId;
            Team = playerId == 0 ? Team.Blue : Team.Red;
            Type = UnitType.Warrior;
            Position = position;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MoveRange = moveRange;
            AttackRange = attackRange;
            AttackDamage = attackDamage;

            bodyRenderer = GetComponent<Renderer>();
            bodyRenderer.material.color = PlayerRegistry.GetColor(playerId);

            label = CreateLabel(transform, PlayerRegistry.GetName(playerId)[0].ToString());
        }

        static TextMesh CreateLabel(Transform parent, string text)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            var labelMesh = labelObject.AddComponent<TextMesh>();
            labelMesh.text = text;
            labelMesh.fontSize = 48;
            labelMesh.characterSize = 0.08f;
            labelMesh.anchor = TextAnchor.MiddleCenter;
            labelMesh.alignment = TextAlignment.Center;
            labelMesh.color = Color.white;
            labelObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return labelMesh;
        }

        public void ResetTurn()
        {
            HasMoved = false;
            HasAttacked = false;
        }

        public void MarkMoved()
        {
            HasMoved = true;
        }

        public void MarkAttacked()
        {
            HasAttacked = true;
        }

        public void SetPosition(GridCoordinates position)
        {
            Position = position;
        }

        public void TakeDamage(int amount)
        {
            var combat = GetComponent<UnitHealthAndCombat>();
            if (combat != null)
            {
                return;
            }

            Health = Mathf.Max(0, Health - amount);
            if (!IsAlive)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
