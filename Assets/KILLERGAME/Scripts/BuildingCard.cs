using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KillerGame
{
    public class BuildingCard : MonoBehaviour
    {
        public TextMeshProUGUI iconText;
        public Image           iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI statusText;
        public Image           progressBar;
        public Button          upgradeBtn;
        public TextMeshProUGUI upgradeCostText;
        public GameObject      progressGroup;
        public GameObject      upgradeGroup;

        private BuildingDef      _def;
        private BuildingState    _bld;
        private GameState        _state;
        private KillerGameManager _gm;

        public void Setup(BuildingDef def, BuildingState bld, GameState state, KillerGameManager gm, Sprite sprite = null)
        {
            _def   = def;
            _bld   = bld;
            _state = state;
            _gm    = gm;

            if (sprite != null && iconImage != null)
            {
                iconImage.sprite             = sprite;
                iconImage.preserveAspect     = true;
                iconImage.type               = Image.Type.Simple;
                iconImage.gameObject.SetActive(true);
                if (iconText) iconText.gameObject.SetActive(false);
                WOSVisualBootstrap.FixIconLayoutPublic(iconImage.transform);
            }
            else if (iconText)
            {
                iconText.text = def.icon;
                if (iconImage) iconImage.gameObject.SetActive(false);
            }
            if (nameText) nameText.text = def.name;
            if (descText) descText.text = def.desc;

            WOSVisualBootstrap.StyleCard(gameObject,
                new Color(0.96f, 0.55f, 0.10f),
                new Color(0.88f, 0.38f, 0.05f),
                "UpgradeBtn");

            Refresh();
            upgradeBtn?.onClick.AddListener(() => { _gm.UpgradeBuilding(_def.key); });
        }

        void Refresh()
        {
            if (levelText) levelText.text = $"Lv {_bld.level}";

            bool maxed     = _bld.level >= _def.maxLevel;
            bool upgrading = _bld.upgrading;

            if (progressGroup) progressGroup.SetActive(upgrading);
            if (upgradeGroup)  upgradeGroup.SetActive(!upgrading && !maxed);

            if (progressBar && upgrading)
                progressBar.fillAmount = _bld.progress / 100f;

            if (statusText)
            {
                if (upgrading)      { statusText.text = $"Upgrading {_bld.progress:0}%"; statusText.gameObject.SetActive(true); }
                else if (maxed)     { statusText.text = "MAX LEVEL";                      statusText.gameObject.SetActive(true); }
                else                { statusText.gameObject.SetActive(false); }
            }

            if (!upgrading && !maxed)
            {
                int next = _bld.level + 1;
                var cost = _def.UpgradeCost(next);
                if (upgradeCostText)
                    upgradeCostText.text = FormatCost(cost);

                bool canAfford = Defs.CanAfford(_state, cost);
                if (upgradeBtn) upgradeBtn.interactable = canAfford;
            }
        }

        static string FormatCost(ResourceCost c)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (c.food > 0) parts.Add($"Food {c.food:0}");
            if (c.wood > 0) parts.Add($"Wood {c.wood:0}");
            if (c.coal > 0) parts.Add($"Coal {c.coal:0}");
            if (c.iron > 0) parts.Add($"Iron {c.iron:0}");
            if (c.gold > 0) parts.Add($"Gold {c.gold:0}");
            return string.Join("  ", parts);
        }
    }
}
