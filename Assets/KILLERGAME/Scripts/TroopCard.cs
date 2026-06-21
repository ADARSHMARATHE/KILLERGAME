using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KillerGame
{
    public class TroopCard : MonoBehaviour
    {
        public TextMeshProUGUI iconText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI countText;
        public TextMeshProUGUI powerText;
        public Image           trainingBar;
        public TextMeshProUGUI statusText;
        public Button          trainBtn;
        public TextMeshProUGUI costText;
        public GameObject      trainingGroup;
        public GameObject      trainBtnGroup;

        private TroopDef         _def;
        private TroopState       _troop;
        private GameState        _state;
        private KillerGameManager _gm;

        public void Setup(TroopDef def, TroopState troop, GameState state, KillerGameManager gm)
        {
            _def   = def;
            _troop = troop;
            _state = state;
            _gm    = gm;

            if (iconText) iconText.text = def.icon;
            if (nameText) nameText.text = def.name;

            WOSVisualBootstrap.StyleCard(gameObject,
                new Color(0.12f, 0.65f, 0.68f),
                new Color(0.15f, 0.58f, 0.26f),
                "TrainBtn");

            Refresh();
            trainBtn?.onClick.AddListener(() => _gm.TrainTroop(_def.key));
        }

        void Refresh()
        {
            if (countText) countText.text = $"{_troop.count}";
            if (powerText) powerText.text = $"PWR {_troop.count * _def.power}";

            bool inTraining = _troop.training > 0f;
            if (trainingGroup) trainingGroup.SetActive(inTraining);
            if (trainBtnGroup) trainBtnGroup.SetActive(!inTraining);

            if (inTraining && trainingBar)
            {
                float pct = 1f - (_troop.training / _troop.trainTime);
                trainingBar.fillAmount = pct;
            }

            if (statusText && inTraining)
                statusText.text = $"Training {_troop.training:0}s remaining";

            if (!inTraining)
            {
                bool canAfford = Defs.CanAfford(_state, _def.cost);
                if (trainBtn) trainBtn.interactable = canAfford;

                if (costText)
                {
                    var c     = _def.cost;
                    var parts = new System.Collections.Generic.List<string>();
                    if (c.food > 0) parts.Add($"Food {c.food:0}");
                    if (c.wood > 0) parts.Add($"Wood {c.wood:0}");
                    if (c.iron > 0) parts.Add($"Iron {c.iron:0}");
                    if (c.gold > 0) parts.Add($"Gold {c.gold:0}");
                    costText.text = string.Join("  ", parts);
                }
            }
        }
    }
}
