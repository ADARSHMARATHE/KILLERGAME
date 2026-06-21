using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrategyGame
{
    public class WorldCaptureManager : MonoBehaviour
    {
        CaptureRadiusTrigger[] zones;
        EventGameManager eventManager;
        EventLeaderboardUI leaderboardUI;
        CaptureRadiusTrigger lastFeedbackZone;

        public void Initialize(EventGameManager manager, EventLeaderboardUI ui)
        {
            eventManager = manager;
            leaderboardUI = ui;
            zones = FindObjectsByType<CaptureRadiusTrigger>(FindObjectsInactive.Exclude)
                .OrderBy(z => z.ZoneType == CaptureZoneType.Citadel ? 0 : 1)
                .ThenBy(z => z.ZoneName)
                .ToArray();

            leaderboardUI.BuildWorldZoneProgressBars(zones);
            leaderboardUI.UpdateStatus(
                "Stand inside a colored circle for 10s alone to capture it and earn points. Citadel = 3× points.");
        }

        void Update()
        {
            if (eventManager == null || !eventManager.MatchActive || zones == null)
            {
                return;
            }

            var agents = eventManager.GetActiveNavAgents();
            var captureDelay = EventGameManager.CaptureDelaySeconds;

            foreach (var zone in zones)
            {
                zone.Tick(Time.deltaTime, agents, captureDelay);
                zone.UpdateVisual(captureDelay);
                AwardZonePoints(zone);
                leaderboardUI.UpdateWorldZoneProgress(zone, zone.CaptureProgress(captureDelay));
            }

            UpdateLocalPlayerFeedback(agents, captureDelay);
            eventManager.RefreshLeaderboardPublic();
        }

        void AwardZonePoints(CaptureRadiusTrigger zone)
        {
            if (!zone.IsGeneratingPoints || !zone.ControllingPlayerId.HasValue)
            {
                return;
            }

            var rate = EventGameManager.BasePointsPerSecond * zone.PointMultiplier;
            eventManager.AddCaptureScore(zone.ControllingPlayerId.Value, rate * Time.deltaTime);
        }

        void UpdateLocalPlayerFeedback(IReadOnlyList<AIPlayerAgent> agents, float captureDelay)
        {
            var local = PlayerCharacterController.LocalPlayer;
            if (local == null || local.Agent == null)
            {
                return;
            }

            CaptureRadiusTrigger currentZone = null;
            foreach (var zone in zones)
            {
                if (zone.ContainsPosition(local.transform.position))
                {
                    currentZone = zone;
                    break;
                }
            }

            if (currentZone == null)
            {
                if (lastFeedbackZone != null)
                {
                    lastFeedbackZone = null;
                    leaderboardUI.UpdateStatus(
                        "Stand in a capture circle alone for 10s to score. Move: left-click ground | Fight: left-click enemies.");
                }

                return;
            }

            lastFeedbackZone = currentZone;

            var progress = currentZone.CaptureProgress(captureDelay);
            var zoneLabel = EventLeaderboardUI.ShortWorldZoneName(currentZone.ZoneName);

            if (currentZone.IsGeneratingPoints && currentZone.ControllingPlayerId == local.Agent.PlayerId)
            {
                var rate = EventGameManager.BasePointsPerSecond * currentZone.PointMultiplier;
                leaderboardUI.UpdateStatus($"Scoring at {zoneLabel}! +{rate:0.#} pts/sec — keep enemies out.");
                return;
            }

            var enemiesInZone = 0;
            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsAlive || agent.PlayerId == local.Agent.PlayerId)
                {
                    continue;
                }

                if (currentZone.ContainsPosition(agent.transform.position))
                {
                    enemiesInZone++;
                }
            }

            if (enemiesInZone > 0)
            {
                leaderboardUI.UpdateStatus($"Contested at {zoneLabel}! Defeat nearby enemies to capture.");
                return;
            }

            var secondsLeft = Mathf.CeilToInt((1f - progress) * captureDelay);
            leaderboardUI.UpdateStatus(
                $"Capturing {zoneLabel} — stay in the circle! {secondsLeft}s until secured (+{currentZone.PointMultiplier}x points).");
        }
    }
}
