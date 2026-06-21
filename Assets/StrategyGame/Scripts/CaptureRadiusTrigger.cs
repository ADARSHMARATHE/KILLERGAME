using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame
{
    public class CaptureRadiusTrigger : MonoBehaviour
    {
        [SerializeField] string zoneName;
        [SerializeField] CaptureZoneType zoneType = CaptureZoneType.Fortress;
        [SerializeField] float captureRadius = 8f;

        public float UncontestedTimer { get; private set; }
        public int? ControllingPlayerId { get; private set; }
        public int? CapturingPlayerId { get; private set; }
        public bool IsGeneratingPoints { get; private set; }

        Renderer zoneRenderer;
        Material zoneMaterial;
        Color baseColor;
        bool isContested;

        public string ZoneName => zoneName;
        public CaptureZoneType ZoneType => zoneType;
        public float CaptureRadius => captureRadius;
        public float PointMultiplier => zoneType == CaptureZoneType.Citadel ? 3f : 1f;

        public void Configure(string name, CaptureZoneType type, float radius)
        {
            zoneName = name;
            zoneType = type;
            captureRadius = radius;
            zoneRenderer = GetComponent<Renderer>();
            if (zoneRenderer != null)
            {
                zoneMaterial = zoneRenderer.material;
                baseColor = zoneMaterial.color;
            }
        }

        public bool ContainsPosition(Vector3 worldPosition)
        {
            var flatPosition = new Vector3(worldPosition.x, 0f, worldPosition.z);
            var flatCenter = new Vector3(transform.position.x, 0f, transform.position.z);
            return Vector3.Distance(flatPosition, flatCenter) <= captureRadius;
        }

        public void Tick(float deltaTime, IReadOnlyList<AIPlayerAgent> agents, float captureDelaySeconds)
        {
            var playersPresent = GetPlayersPresent(agents);

            isContested = playersPresent.Count > 1;

            if (playersPresent.Count == 0)
            {
                UncontestedTimer = 0f;
                IsGeneratingPoints = false;
                ControllingPlayerId = null;
                CapturingPlayerId = null;
                return;
            }

            if (isContested)
            {
                UncontestedTimer = 0f;
                IsGeneratingPoints = false;
                ControllingPlayerId = null;
                CapturingPlayerId = null;
                return;
            }

            foreach (var playerId in playersPresent)
            {
                CapturingPlayerId = playerId;
                UncontestedTimer += deltaTime;
                if (UncontestedTimer >= captureDelaySeconds)
                {
                    ControllingPlayerId = playerId;
                    IsGeneratingPoints = true;
                }

                break;
            }
        }

        public float CaptureProgress(float captureDelaySeconds)
        {
            if (captureDelaySeconds <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(UncontestedTimer / captureDelaySeconds);
        }

        public void UpdateVisual(float captureDelaySeconds)
        {
            if (zoneMaterial == null)
            {
                return;
            }

            var progress = CaptureProgress(captureDelaySeconds);
            var color = baseColor;

            if (IsGeneratingPoints && ControllingPlayerId.HasValue)
            {
                color = PlayerRegistry.GetColor(ControllingPlayerId.Value);
                color.a = 0.55f;
            }
            else if (CapturingPlayerId.HasValue)
            {
                color = PlayerRegistry.GetColor(CapturingPlayerId.Value);
                color.a = 0.25f + progress * 0.35f;
            }
            else if (isContested)
            {
                color = new Color(0.95f, 0.35f, 0.35f, 0.35f);
            }

            zoneMaterial.color = color;
        }

        HashSet<int> GetPlayersPresent(IReadOnlyList<AIPlayerAgent> agents)
        {
            var players = new HashSet<int>();

            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsAlive || !agent.IsActiveInArena)
                {
                    continue;
                }

                if (!ContainsPosition(agent.transform.position))
                {
                    continue;
                }

                players.Add(agent.PlayerId);
            }

            return players;
        }
    }
}
