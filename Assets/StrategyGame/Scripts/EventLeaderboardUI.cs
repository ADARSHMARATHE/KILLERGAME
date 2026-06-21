using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace StrategyGame
{
    public class EventLeaderboardUI : MonoBehaviour
    {
        Text timerText;
        Text leaderboardText;
        Text statusText;
        Text finalText;
        RectTransform zonePanel;
        ZoneProgressRow[] zoneRows;
        CaptureRadiusTrigger[] worldZones;

        struct ZoneProgressRow
        {
            public Text Label;
            public Image Fill;
        }

        public void Build(Canvas canvas)
        {
            timerText = CreateText(canvas.transform, "EventTimerText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), 34, TextAnchor.UpperCenter, "10:00");

            statusText = CreateText(canvas.transform, "EventStatusText", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -20f), 22, TextAnchor.UpperLeft, "Capture zones to score points.");

            var panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-20f, 0f);
            panelRect.sizeDelta = new Vector2(280f, 420f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.88f);

            var title = CreateText(panel.transform, "LeaderboardTitle", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -12f), 24, TextAnchor.UpperCenter, "Live Leaderboard");
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(-20f, 40f);

            leaderboardText = CreateText(panel.transform, "LeaderboardBody", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, -10f), 18, TextAnchor.UpperLeft, "");
            var bodyRect = leaderboardText.GetComponent<RectTransform>();
            bodyRect.offsetMin = new Vector2(16f, 16f);
            bodyRect.offsetMax = new Vector2(-16f, -48f);

            finalText = CreateText(canvas.transform, "EventFinalText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 44, TextAnchor.MiddleCenter, "");
            finalText.gameObject.SetActive(false);
        }

        public void BuildWorldZoneProgressBars(CaptureRadiusTrigger[] zones)
        {
            worldZones = zones;
            var panel = new GameObject("ZoneProgressPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(statusText.transform.parent, false);
            zonePanel = panel.GetComponent<RectTransform>();
            zonePanel.anchorMin = new Vector2(0f, 0f);
            zonePanel.anchorMax = new Vector2(0f, 0f);
            zonePanel.pivot = new Vector2(0f, 0f);
            zonePanel.anchoredPosition = new Vector2(20f, 20f);
            zonePanel.sizeDelta = new Vector2(360f, 210f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.82f);

            var title = CreateText(panel.transform, "ZoneProgressTitle", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), 20, TextAnchor.UpperLeft, "Zone Capture Progress");
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(-20f, 28f);
            titleRect.offsetMin = new Vector2(12f, titleRect.offsetMin.y);

            zoneRows = new ZoneProgressRow[zones.Length];
            for (var i = 0; i < zones.Length; i++)
            {
                zoneRows[i] = CreateWorldZoneRow(panel.transform, zones[i], i);
            }
        }

        public void UpdateWorldZoneProgress(CaptureRadiusTrigger zone, float progress)
        {
            if (zoneRows == null || worldZones == null)
            {
                return;
            }

            var index = WorldZoneIndexOf(zone);
            if (index < 0)
            {
                return;
            }

            var row = zoneRows[index];
            row.Fill.fillAmount = progress;

            var owner = zone.IsGeneratingPoints && zone.ControllingPlayerId.HasValue
                ? PlayerRegistry.GetName(zone.ControllingPlayerId.Value)
                : zone.CapturingPlayerId.HasValue
                    ? $"{PlayerRegistry.GetName(zone.CapturingPlayerId.Value)} capturing"
                    : "Neutral";

            var suffix = zone.IsGeneratingPoints
                ? $" • +{EventGameManager.BasePointsPerSecond * zone.PointMultiplier:0.#}/s"
                : progress > 0f
                    ? $" • {Mathf.CeilToInt((1f - progress) * EventGameManager.CaptureDelaySeconds)}s"
                    : "";

            row.Label.text = $"{ShortWorldZoneName(zone.ZoneName)} — {owner}{suffix}";
            row.Fill.color = zone.IsGeneratingPoints && zone.ControllingPlayerId.HasValue
                ? PlayerRegistry.GetColor(zone.ControllingPlayerId.Value)
                : zone.CapturingPlayerId.HasValue
                    ? PlayerRegistry.GetColor(zone.CapturingPlayerId.Value)
                    : zone.ZoneType == CaptureZoneType.Citadel
                        ? new Color(0.95f, 0.75f, 0.15f)
                        : new Color(0.45f, 0.65f, 0.95f);
        }

        int WorldZoneIndexOf(CaptureRadiusTrigger zone)
        {
            if (worldZones == null)
            {
                return -1;
            }

            for (var i = 0; i < worldZones.Length; i++)
            {
                if (worldZones[i] == zone)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string ShortWorldZoneName(string zoneName)
        {
            return zoneName switch
            {
                "Central Citadel" => "Citadel",
                "PeripheralFortress_NW" => "NW Fort",
                "PeripheralFortress_NE" => "NE Fort",
                "PeripheralFortress_SW" => "SW Fort",
                "PeripheralFortress_SE" => "SE Fort",
                "Northwest Fortress" => "NW Fort",
                "Northeast Fortress" => "NE Fort",
                "Southwest Fortress" => "SW Fort",
                "Southeast Fortress" => "SE Fort",
                _ => zoneName
            };
        }

        ZoneProgressRow CreateWorldZoneRow(Transform parent, CaptureRadiusTrigger zone, int index)
        {
            var rowObject = new GameObject($"ZoneRow_{index}", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(12f, -38f - index * 26f);
            rowRect.sizeDelta = new Vector2(-24f, 22f);

            var label = CreateText(rowObject.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, 14, TextAnchor.MiddleLeft, $"{ShortWorldZoneName(zone.ZoneName)} — Neutral");
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(0f, 10f);
            labelRect.offsetMax = Vector2.zero;

            var barBackground = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barBackground.transform.SetParent(rowObject.transform, false);
            var bgRect = barBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0f, 0f);
            bgRect.sizeDelta = new Vector2(0f, 8f);
            barBackground.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.22f, 1f);

            var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barFill.transform.SetParent(barBackground.transform, false);
            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = barFill.GetComponent<Image>();
            fillImage.color = zone.ZoneType == CaptureZoneType.Citadel
                ? new Color(0.95f, 0.75f, 0.15f)
                : new Color(0.45f, 0.65f, 0.95f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;

            return new ZoneProgressRow { Label = label, Fill = fillImage };
        }

        public void BuildZoneProgressBars(CapturePointZone[] zones)
        {
            var panel = new GameObject("ZoneProgressPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(statusText.transform.parent, false);
            zonePanel = panel.GetComponent<RectTransform>();
            zonePanel.anchorMin = new Vector2(0f, 0f);
            zonePanel.anchorMax = new Vector2(0f, 0f);
            zonePanel.pivot = new Vector2(0f, 0f);
            zonePanel.anchoredPosition = new Vector2(20f, 20f);
            zonePanel.sizeDelta = new Vector2(360f, 170f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.82f);

            var title = CreateText(panel.transform, "ZoneProgressTitle", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), 20, TextAnchor.UpperLeft, "Zone Capture Progress");
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(-20f, 28f);
            titleRect.offsetMin = new Vector2(12f, titleRect.offsetMin.y);

            zoneRows = new ZoneProgressRow[zones.Length];
            for (var i = 0; i < zones.Length; i++)
            {
                zoneRows[i] = CreateZoneRow(panel.transform, zones[i], i);
            }
        }

        public void UpdateZoneProgress(CapturePointZone zone, float progress)
        {
            if (zoneRows == null || CaptureZonesIndexOf(zone) < 0)
            {
                return;
            }

            var row = zoneRows[CaptureZonesIndexOf(zone)];
            row.Fill.fillAmount = progress;

            var owner = zone.IsGeneratingPoints && zone.ControllingPlayerId.HasValue
                ? PlayerRegistry.GetName(zone.ControllingPlayerId.Value)
                : zone.CapturingPlayerId.HasValue
                    ? $"{PlayerRegistry.GetName(zone.CapturingPlayerId.Value)} capturing"
                    : "Neutral";

            var suffix = zone.IsGeneratingPoints ? " • scoring" : progress > 0f ? $" • {Mathf.CeilToInt((1f - progress) * EventGameManager.CaptureDelaySeconds)}s" : "";
            row.Label.text = $"{ShortZoneName(zone.ZoneName)} — {owner}{suffix}";
            row.Fill.color = zone.IsGeneratingPoints && zone.ControllingPlayerId.HasValue
                ? PlayerRegistry.GetColor(zone.ControllingPlayerId.Value)
                : zone.CapturingPlayerId.HasValue
                    ? PlayerRegistry.GetColor(zone.CapturingPlayerId.Value)
                    : zone.ZoneType == CaptureZoneType.Citadel
                        ? new Color(0.95f, 0.75f, 0.15f)
                        : new Color(0.45f, 0.65f, 0.95f);
        }

        int CaptureZonesIndexOf(CapturePointZone zone)
        {
            if (EventGameManager.Instance?.CaptureZones == null)
            {
                return -1;
            }

            for (var i = 0; i < EventGameManager.Instance.CaptureZones.Length; i++)
            {
                if (EventGameManager.Instance.CaptureZones[i] == zone)
                {
                    return i;
                }
            }

            return -1;
        }

        static string ShortZoneName(string zoneName)
        {
            return zoneName switch
            {
                "Central Citadel" => "Citadel",
                "Northwest Fortress" => "NW Fort",
                "Northeast Fortress" => "NE Fort",
                "Southwest Fortress" => "SW Fort",
                "Southeast Fortress" => "SE Fort",
                _ => zoneName
            };
        }

        ZoneProgressRow CreateZoneRow(Transform parent, CapturePointZone zone, int index)
        {
            var rowObject = new GameObject($"ZoneRow_{index}", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(12f, -38f - index * 26f);
            rowRect.sizeDelta = new Vector2(-24f, 22f);

            var label = CreateText(rowObject.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, 14, TextAnchor.MiddleLeft, $"{ShortZoneName(zone.ZoneName)} — Neutral");
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(0f, 10f);
            labelRect.offsetMax = Vector2.zero;

            var barBackground = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barBackground.transform.SetParent(rowObject.transform, false);
            var bgRect = barBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0f, 0f);
            bgRect.sizeDelta = new Vector2(0f, 8f);
            barBackground.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.22f, 1f);

            var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barFill.transform.SetParent(barBackground.transform, false);
            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = barFill.GetComponent<Image>();
            fillImage.color = zone.ZoneType == CaptureZoneType.Citadel
                ? new Color(0.95f, 0.75f, 0.15f)
                : new Color(0.45f, 0.65f, 0.95f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;

            return new ZoneProgressRow { Label = label, Fill = fillImage };
        }

        public void UpdateTimer(float remainingSeconds)
        {
            var minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            var seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void UpdateLeaderboard(IReadOnlyList<EventGameManager.PlayerScoreEntry> entries, int topCount = 10)
        {
            if (leaderboardText == null || entries == null)
            {
                return;
            }

            var builder = new StringBuilder();
            var count = Mathf.Min(topCount, entries.Count);

            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                var color = ColorUtility.ToHtmlStringRGB(PlayerRegistry.GetColor(entry.PlayerId));
                var elimLabel = entry.Eliminations > 0 ? $" ({entry.Eliminations}K)" : "";
                builder.AppendLine($"<color=#{color}>{i + 1}. {entry.PlayerName} — {entry.Score:0}{elimLabel}</color>");
            }

            leaderboardText.text = count > 0 ? builder.ToString() : "Waiting for players...";
        }

        public void UpdateStatus(string message)
        {
            statusText.text = message;
        }

        public void ShowFinalResults(string message)
        {
            finalText.text = message;
            finalText.gameObject.SetActive(true);
        }

        static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition,
            int fontSize, TextAnchor alignment, string content)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rect.anchoredPosition = anchoredPosition;
            if (Mathf.Approximately(anchorMin.x, anchorMax.x) && Mathf.Approximately(anchorMin.y, anchorMax.y))
            {
                rect.sizeDelta = new Vector2(260f, 400f);
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }
    }
}
