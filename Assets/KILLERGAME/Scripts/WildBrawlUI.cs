using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KillerGame
{
    /// <summary>
    /// WOS-style Wild Brawl event modal + side HUD chip.
    /// </summary>
    public class WildBrawlUI : MonoBehaviour
    {
        static readonly Color Gold       = new Color(0.92f, 0.74f, 0.22f);
        static readonly Color GoldDim    = new Color(0.55f, 0.42f, 0.12f);
        static readonly Color PanelBg    = new Color(0.04f, 0.07f, 0.13f, 0.96f);
        static readonly Color PanelInner = new Color(0.06f, 0.10f, 0.18f, 0.98f);
        static readonly Color RedBanner  = new Color(0.72f, 0.14f, 0.10f, 0.95f);
        static readonly Color FightRed   = new Color(0.88f, 0.18f, 0.10f);
        static readonly Color FightRedD  = new Color(0.58f, 0.10f, 0.06f);
        static readonly Color SkipGrey   = new Color(0.22f, 0.26f, 0.34f);
        static readonly Color BarYou     = new Color(0.28f, 0.62f, 0.95f);
        static readonly Color BarEnemy   = new Color(0.92f, 0.28f, 0.18f);
        static readonly Color BarBg      = new Color(0.08f, 0.10f, 0.16f, 0.95f);
        static readonly Color WinGreen   = new Color(0.35f, 0.92f, 0.48f);
        static readonly Color LossRed    = new Color(0.95f, 0.32f, 0.28f);

        GameObject _root;
        GameObject _bannerChip;
        TextMeshProUGUI _chipTimer;
        TextMeshProUGUI _chipLabel;

        TextMeshProUGUI _enemyName;
        TextMeshProUGUI _yourPowerNum;
        TextMeshProUGUI _enemyPowerNum;
        TextMeshProUGUI _timerText;
        TextMeshProUGUI _rewardText;
        TextMeshProUGUI _resultText;
        Image _yourBarFill;
        Image _enemyBarFill;
        Image _timerBarFill;
        Image _enemyPortrait;
        Button _fightBtn;
        Button _skipBtn;
        Button _closeBtn;
        GameObject _resultPanel;

        KillerGameManager _gm;
        bool _showingResult;

        void Awake()
        {
            TryBuildUI();
            if (_root != null) _root.SetActive(false);
            if (_bannerChip != null) _bannerChip.SetActive(false);
        }

        void Start()
        {
            if (_root == null) TryBuildUI();
            if (_root == null) { Debug.LogWarning("WildBrawlUI: no Canvas found."); return; }

            _gm = KillerGameManager.Instance;
            if (_gm == null) { Debug.LogWarning("WildBrawlUI: KillerGameManager not found."); return; }

            _gm.OnWildBrawlStarted.AddListener(OnBrawlStarted);
            _gm.OnStateChanged.AddListener(OnStateChanged);
            _fightBtn.onClick.AddListener(OnFight);
            _skipBtn.onClick.AddListener(OnSkip);
            _closeBtn.onClick.AddListener(OnCloseModal);
            _bannerChip.GetComponent<Button>().onClick.AddListener(ReopenModal);
        }

        void OnDestroy()
        {
            if (_gm == null) return;
            _gm.OnWildBrawlStarted.RemoveListener(OnBrawlStarted);
            _gm.OnStateChanged.RemoveListener(OnStateChanged);
        }

        void OnBrawlStarted()
        {
            _showingResult = false;
            if (_enemyPortrait != null)
                _enemyPortrait.sprite = LoadEnemyPortrait();
            _resultPanel.SetActive(false);
            _resultText.gameObject.SetActive(false);
            _fightBtn.gameObject.SetActive(true);
            _skipBtn.gameObject.SetActive(true);
            _closeBtn.gameObject.SetActive(true);
            GetButtonLabel(_skipBtn).text = "Skip";
            RefreshDisplay();
            _root.SetActive(true);
            _bannerChip.SetActive(true);
        }

        void OnStateChanged()
        {
            if (_gm == null) return;
            var active = _gm.State.wildBrawl.active;
            _bannerChip.SetActive(active && !_showingResult);

            if (_root.activeSelf && !_showingResult)
                RefreshDisplay();

            if (_bannerChip.activeSelf)
                RefreshChip();
        }

        void RefreshChip()
        {
            var b = _gm.State.wildBrawl;
            _chipTimer.text = $"{Mathf.CeilToInt(b.timeRemaining)}s";
            _chipLabel.text = "WILD BRAWL";
        }

        void ReopenModal()
        {
            if (!_gm.State.wildBrawl.active || _showingResult) return;
            RefreshDisplay();
            _root.SetActive(true);
        }

        void RefreshDisplay()
        {
            var b = _gm.State.wildBrawl;
            int yourPower = _gm.CalcTroopPower();
            int enemyPower = b.enemyPower;

            _enemyName.text = b.enemyName;
            if (_enemyPortrait != null && _enemyPortrait.sprite == null)
                _enemyPortrait.sprite = LoadEnemyPortrait();
            _yourPowerNum.text = yourPower.ToString("N0");
            _enemyPowerNum.text = enemyPower.ToString("N0");

            float maxP = Mathf.Max(yourPower, enemyPower, 1);
            _yourBarFill.fillAmount = yourPower / maxP;
            _enemyBarFill.fillAmount = enemyPower / maxP;

            float dur = Mathf.Max(b.duration, 1f);
            _timerBarFill.fillAmount = b.timeRemaining / dur;
            _timerText.text = $"{Mathf.CeilToInt(b.timeRemaining)}s";

            _rewardText.text = $"Victory Reward:  +{b.previewGold} Gold   +{b.previewFood} Food";
        }

        void OnFight()
        {
            _showingResult = true;
            string outcome = _gm.FightWildBrawl();
            bool won = outcome.StartsWith("VICTORY");

            _resultText.text = outcome;
            _resultText.color = won ? WinGreen : LossRed;
            _resultText.gameObject.SetActive(true);
            _resultPanel.SetActive(true);
            _fightBtn.gameObject.SetActive(false);
            _skipBtn.gameObject.SetActive(true);
            _closeBtn.gameObject.SetActive(false);
            GetButtonLabel(_skipBtn).text = "Close";
            _bannerChip.SetActive(false);
        }

        void OnSkip()
        {
            if (_showingResult)
            {
                _root.SetActive(false);
                _showingResult = false;
                return;
            }
            if (_gm.State.wildBrawl.active)
                _gm.SkipWildBrawl();
            _root.SetActive(false);
            _bannerChip.SetActive(false);
        }

        void OnCloseModal()
        {
            if (_showingResult) return;
            _root.SetActive(false);
        }

        public void Hide()
        {
            _root?.SetActive(false);
            _bannerChip?.SetActive(false);
        }

        void TryBuildUI()
        {
            if (_root != null) return;
            BuildModal();
            BuildBannerChip();
        }

        void BuildModal()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _root = new GameObject("WildBrawlModal");
            _root.transform.SetParent(canvas.transform, false);
            Stretch(_root.AddComponent<RectTransform>());

            var dim = MakeImage(_root.transform, "Dim", new Color(0f, 0f, 0.05f, 0.72f));
            Stretch(dim.GetComponent<RectTransform>());

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.06f, 0.22f);
            panelRt.anchorMax = new Vector2(0.94f, 0.78f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = GoldDim;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(panel.transform, false);
            var innerRt = inner.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(4f, 4f);
            innerRt.offsetMax = new Vector2(-4f, -4f);
            inner.AddComponent<Image>().color = PanelInner;

            // Red top banner strip
            var banner = MakeImage(inner.transform, "Banner", RedBanner);
            var bannerRt = banner.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0f, 1f);
            bannerRt.anchorMax = new Vector2(1f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.sizeDelta = new Vector2(0f, 52f);

            MakeText(banner.transform, "BannerTitle", "WILD BRAWL", 30, Gold, FontStyles.Bold,
                Anchor(0.5f, 0.5f), new Vector2(400f, 44f));

            MakeText(inner.transform, "EventTag", "LIMITED TIME EVENT", 13,
                new Color(0.75f, 0.78f, 0.85f), FontStyles.Bold,
                Anchor(0.5f, 0.88f), new Vector2(300f, 22f));

            // Enemy portrait + name
            var portraitFrame = MakeImage(inner.transform, "PortraitFrame", GoldDim);
            var pfRt = portraitFrame.GetComponent<RectTransform>();
            pfRt.anchorMin = pfRt.anchorMax = Anchor(0.5f, 0.72f);
            pfRt.sizeDelta = new Vector2(88f, 88f);

            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(portraitFrame.transform, false);
            var pRt = portraitGo.AddComponent<RectTransform>();
            Stretch(pRt);
            pRt.offsetMin = pRt.offsetMax = new Vector2(4f, 4f);
            _enemyPortrait = portraitGo.AddComponent<Image>();
            _enemyPortrait.color = Color.white;
            _enemyPortrait.preserveAspect = true;
            _enemyPortrait.sprite = LoadEnemyPortrait();

            _enemyName = MakeText(inner.transform, "EnemyName", "Frost Raiders", 22, Gold, FontStyles.Bold,
                Anchor(0.5f, 0.60f), new Vector2(360f, 32f));

            MakeText(inner.transform, "ArenaLabel", "ARENA SHOWDOWN", 12,
                new Color(0.55f, 0.58f, 0.65f), FontStyles.Bold,
                Anchor(0.5f, 0.55f), new Vector2(200f, 20f));

            // Power bars — yours (left) vs enemy (right)
            BuildPowerRow(inner.transform, "YourSide", "YOUR ARMY", 0.42f, 0.28f, BarYou, out _yourBarFill, out _yourPowerNum);
            BuildPowerRow(inner.transform, "EnemySide", "ENEMY ARMY", 0.42f, 0.72f, BarEnemy, out _enemyBarFill, out _enemyPowerNum);

            MakeText(inner.transform, "VS", "VS", 34, new Color(0.95f, 0.25f, 0.15f), FontStyles.Bold,
                Anchor(0.5f, 0.42f), new Vector2(60f, 44f));

            // Countdown bar
            var timerRow = new GameObject("TimerRow");
            timerRow.transform.SetParent(inner.transform, false);
            var trRt = timerRow.AddComponent<RectTransform>();
            trRt.anchorMin = trRt.anchorMax = Anchor(0.5f, 0.28f);
            trRt.sizeDelta = new Vector2(420f, 36f);

            var timerBg = MakeImage(timerRow.transform, "TimerBG", BarBg);
            Stretch(timerBg.GetComponent<RectTransform>());
            timerBg.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 8f);
            timerBg.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 0f);

            var timerFillGo = MakeImage(timerRow.transform, "TimerFill", new Color(0.95f, 0.55f, 0.12f));
            var tfRt = timerFillGo.GetComponent<RectTransform>();
            tfRt.anchorMin = Vector2.zero;
            tfRt.anchorMax = Vector2.one;
            tfRt.offsetMin = new Vector2(2f, 10f);
            tfRt.offsetMax = new Vector2(-2f, -2f);
            _timerBarFill = timerFillGo.GetComponent<Image>();
            _timerBarFill.type = Image.Type.Filled;
            _timerBarFill.fillMethod = Image.FillMethod.Horizontal;
            _timerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            _timerText = MakeText(timerRow.transform, "TimerText", "60s", 16, Color.white, FontStyles.Bold,
                Anchor(0.5f, 0.5f), new Vector2(80f, 28f));

            MakeText(timerRow.transform, "TimerLabel", "TIME REMAINING", 11,
                new Color(0.6f, 0.65f, 0.72f), FontStyles.Normal,
                Anchor(0.5f, 1f), new Vector2(160f, 18f)).rectTransform.pivot = new Vector2(0.5f, 1f);

            // Reward preview
            _rewardText = MakeText(inner.transform, "Reward", "Victory Reward: +100 Gold", 15,
                new Color(0.96f, 0.78f, 0.22f), FontStyles.Bold,
                Anchor(0.5f, 0.20f), new Vector2(440f, 28f));

            // Buttons
            _fightBtn = MakeButton(inner.transform, "FightBtn", "FIGHT!", FightRed, FightRedD,
                Anchor(0.35f, 0.08f), new Vector2(160f, 50f));
            _skipBtn = MakeButton(inner.transform, "SkipBtn", "Skip", SkipGrey, SkipGrey * 0.8f,
                Anchor(0.68f, 0.08f), new Vector2(120f, 50f));

            _closeBtn = MakeButton(inner.transform, "CloseBtn", "X", SkipGrey, SkipGrey * 0.8f,
                Anchor(0.96f, 0.96f), new Vector2(36f, 36f));

            // Result overlay
            _resultPanel = new GameObject("ResultPanel");
            _resultPanel.transform.SetParent(inner.transform, false);
            var resRt = _resultPanel.AddComponent<RectTransform>();
            resRt.anchorMin = new Vector2(0.05f, 0.32f);
            resRt.anchorMax = new Vector2(0.95f, 0.68f);
            resRt.offsetMin = resRt.offsetMax = Vector2.zero;
            _resultPanel.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
            _resultPanel.SetActive(false);

            var resBorder = MakeImage(_resultPanel.transform, "ResBorder", Gold);
            var rbRt = resBorder.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0f, 1f);
            rbRt.anchorMax = new Vector2(1f, 1f);
            rbRt.pivot = new Vector2(0.5f, 1f);
            rbRt.sizeDelta = new Vector2(0f, 3f);

            _resultText = MakeText(_resultPanel.transform, "Result", "", 20, WinGreen, FontStyles.Bold,
                Anchor(0.5f, 0.5f), new Vector2(380f, 120f));
            _resultText.gameObject.SetActive(false);
        }

        void BuildPowerRow(Transform parent, string sideName, string label, float y, float x,
            Color fillColor, out Image fill, out TextMeshProUGUI powerNum)
        {
            var side = new GameObject(sideName);
            side.transform.SetParent(parent, false);
            var sRt = side.AddComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = Anchor(y, x);
            sRt.sizeDelta = new Vector2(160f, 70f);

            MakeText(side.transform, "Label", label, 11, new Color(0.65f, 0.68f, 0.75f), FontStyles.Bold,
                Anchor(1f, 0.5f), new Vector2(160f, 18f)).rectTransform.pivot = new Vector2(0.5f, 1f);

            var barBg = MakeImage(side.transform, "BarBG", BarBg);
            var bgRt = barBg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.35f);
            bgRt.anchorMax = new Vector2(1f, 0.65f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillGo = MakeImage(side.transform, "BarFill", fillColor);
            var fRt = fillGo.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2f, 2f);
            fRt.offsetMax = new Vector2(-2f, -2f);
            fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            powerNum = MakeText(side.transform, "Power", "0", 22, Color.white, FontStyles.Bold,
                Anchor(0f, 0.5f), new Vector2(160f, 30f));
            powerNum.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        void BuildBannerChip()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _bannerChip = new GameObject("WildBrawlBanner");
            _bannerChip.transform.SetParent(canvas.transform, false);
            var rt = _bannerChip.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.58f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(130f, 72f);
            rt.anchoredPosition = new Vector2(-8f, 0f);

            _bannerChip.AddComponent<Image>().color = new Color(0.72f, 0.14f, 0.10f, 0.92f);
            var btn = _bannerChip.AddComponent<Button>();
            var cb = btn.colors;
            cb.highlightedColor = new Color(0.85f, 0.22f, 0.14f);
            cb.pressedColor = new Color(0.55f, 0.10f, 0.06f);
            btn.colors = cb;

            var border = MakeImage(_bannerChip.transform, "ChipBorder", Gold);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.sizeDelta = new Vector2(0f, 3f);

            _chipLabel = MakeText(_bannerChip.transform, "ChipLabel", "WILD BRAWL", 13, Gold, FontStyles.Bold,
                Anchor(0.5f, 0.65f), new Vector2(120f, 22f));
            _chipTimer = MakeText(_bannerChip.transform, "ChipTimer", "60s", 20, Color.white, FontStyles.Bold,
                Anchor(0.5f, 0.30f), new Vector2(80f, 28f));

            MakeText(_bannerChip.transform, "TapHint", "TAP", 10, new Color(1f, 1f, 1f, 0.6f), FontStyles.Normal,
                Anchor(0.5f, 0.08f), new Vector2(60f, 14f));
        }

        static Sprite LoadEnemyPortrait()
        {
            var sprites = Resources.LoadAll<Sprite>("Sprites/hero_portraits");
            if (sprites != null && sprites.Length > 0)
                return sprites[Random.Range(0, sprites.Length)];

#if UNITY_EDITOR
            var sheet = AssetDatabase.LoadAllAssetsAtPath("Assets/KILLERGAME/Sprites/hero_portraits.png");
            foreach (var a in sheet)
                if (a is Sprite s) return s;
#endif
            return null;
        }

        static Vector2 Anchor(float y, float x) => new Vector2(x, y);

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static GameObject MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
            FontStyles style, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button MakeButton(Transform parent, string name, string label, Color face, Color pressed,
            Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<Image>().color = face;
            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = face;
            cb.highlightedColor = face * 1.12f;
            cb.pressedColor = pressed;
            cb.selectedColor = face;
            btn.colors = cb;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.AddComponent<RectTransform>());
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        static TextMeshProUGUI GetButtonLabel(Button btn)
        {
            return btn.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
