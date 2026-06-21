using UnityEngine;
using UnityEngine.UI;

namespace StrategyGame
{
    public class GameUI : MonoBehaviour
    {
        Text statusText;
        Text helpText;
        Text victoryText;
        Button endTurnButton;
        Button skipAttackButton;

        public void Build(Canvas canvas, GameManager gameManager)
        {
            statusText = CreateText(canvas.transform, "StatusText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), 28,
                TextAnchor.UpperLeft, "Blue's turn");
            helpText = CreateText(canvas.transform, "HelpText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -60f), 20,
                TextAnchor.UpperLeft, "Select a unit, move (blue), attack (red), then end turn.");
            victoryText = CreateText(canvas.transform, "VictoryText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48,
                TextAnchor.MiddleCenter, "");
            victoryText.gameObject.SetActive(false);

            endTurnButton = CreateButton(canvas.transform, "EndTurnButton", new Vector2(1f, 0f), new Vector2(-20f, 20f), "End Turn",
                () => gameManager.HandleEndTurnPressed());
            skipAttackButton = CreateButton(canvas.transform, "SkipAttackButton", new Vector2(1f, 0f), new Vector2(-20f, 80f), "Skip Attack",
                () => gameManager.HandleSkipAttackPressed());
        }

        public void UpdateStatus(string teamLabel, TurnPhase phase)
        {
            statusText.text = $"{teamLabel}'s turn — {PhaseLabel(phase)}";
            skipAttackButton.gameObject.SetActive(phase == TurnPhase.Attack);
        }

        public void ShowVictory(string message)
        {
            victoryText.text = message;
            victoryText.gameObject.SetActive(true);
            endTurnButton.interactable = false;
            skipAttackButton.interactable = false;
        }

        static string PhaseLabel(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.SelectUnit => "Select a unit",
                TurnPhase.Move => "Choose where to move",
                TurnPhase.Attack => "Choose a target",
                _ => "End turn"
            };
        }

        static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, int fontSize,
            TextAnchor alignment, string content)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(700f, 80f);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        static Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(160f, 44f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var text = CreateText(buttonObject.transform, "Label", Vector2.zero, Vector2.one, Vector2.zero, 20, TextAnchor.MiddleCenter, label);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
