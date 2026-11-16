using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChibiARPG
{
    public class GameHud : MonoBehaviour
    {
        public static GameHud Instance { get; private set; }

        public bool AttackHeld { get; private set; }

        RectTransform _root;
        Image _hpFill;
        Image _mpFill;
        Text _hpText;
        Text _mpText;
        Text _goldText;
        Text _questTitle;
        Text _questProg;
        Text _toast;
        Text _dlgName;
        Text _dlgBody;
        Text _skillCd;
        Image _skillDim;
        GameObject _talkBtn;
        GameObject _dlgPanel;
        GameObject _gameOver;
        GameObject _classPanel;
        GameObject _playHud;
        Button _atkBtn;
        Button _skBtn;
        string[] _dlgLines;
        int _dlgIndex;
        System.Action _dlgDone;
        string _pendingAcceptId;
        string _pendingTurnInId;
        int _selectedClassIndex;
        Image[] _classCards;
        Coroutine _toastCo;
        CharacterStats _playerStats;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindPlayer(CharacterStats stats)
        {
            if (_playerStats != null)
            {
                _playerStats.OnHpChanged -= OnHp;
                _playerStats.OnMpChanged -= OnMp;
                _playerStats.OnDied -= OnDied;
            }

            _playerStats = stats;
            if (stats != null)
            {
                stats.OnHpChanged += OnHp;
                stats.OnMpChanged += OnMp;
                stats.OnDied += OnDied;
                OnHp(stats.hp, stats.maxHp);
                OnMp(stats.mp, stats.maxMp);
            }
        }

        void OnHp(float hp, float max)
        {
            if (_hpFill != null)
                _hpFill.fillAmount = max <= 0f ? 0f : hp / max;
            if (_hpText != null)
                _hpText.text = "生命  " + Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(max);
        }

        void OnMp(float mp, float max)
        {
            if (_mpFill != null)
                _mpFill.fillAmount = max <= 0f ? 0f : mp / max;
            if (_mpText != null)
                _mpText.text = "法力  " + Mathf.CeilToInt(mp) + " / " + Mathf.CeilToInt(max);
        }

        void OnDied(CharacterStats s)
        {
            ShowGameOver(true);
        }

        public void Toast(string msg)
        {
            if (_toast == null)
                return;
            if (_toastCo != null)
                StopCoroutine(_toastCo);
            _toastCo = StartCoroutine(ToastRoutine(msg));
        }

        IEnumerator ToastRoutine(string msg)
        {
            _toast.text = msg;
            _toast.gameObject.SetActive(true);
            Color c = _toast.color;
            c.a = 1f;
            _toast.color = c;
            yield return new WaitForSecondsRealtime(2.2f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                c.a = 1f - t / 0.4f;
                _toast.color = c;
                yield return null;
            }

            _toast.gameObject.SetActive(false);
        }

        public void RefreshQuest()
        {
            if (QuestSystem.Instance == null)
                return;
            var q = QuestSystem.Instance.TrackedQuest();
            if (_questTitle != null)
                _questTitle.text = q == null ? (SaveGame.LoadOrNew().storyDone ? "暂无任务" : "青岚的访客") : q.title;
            if (_questProg != null)
                _questProg.text = QuestSystem.Instance.ActiveProgressText;
            if (_goldText != null)
                _goldText.text = "金币  " + SaveGame.LoadOrNew().gold + "    经验  " + SaveGame.LoadOrNew().xp;
            RefreshNpcMarkers();
        }

        static void RefreshNpcMarkers()
        {
            var list = NpcCharacter.All.Items;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i].RefreshMarker();
            }
        }

        public void SetTalkVisible(bool on)
        {
            if (_talkBtn != null)
                _talkBtn.SetActive(on);
        }

        public void ShowDialogue(string speaker, string[] lines, string acceptQuestId, string turnInQuestId, System.Action done)
        {
            _dlgLines = lines != null && lines.Length > 0 ? lines : new[] { "……" };
            _dlgIndex = 0;
            _dlgDone = done;
            _pendingAcceptId = acceptQuestId;
            _pendingTurnInId = turnInQuestId;
            if (_dlgPanel != null)
                _dlgPanel.SetActive(true);
            if (_dlgName != null)
                _dlgName.text = speaker;
            ShowDlgLine();
            if (GameManager.Instance != null)
                GameManager.Instance.SetTalkLock(true);
        }

        void ShowDlgLine()
        {
            if (_dlgBody != null)
                _dlgBody.text = _dlgLines[Mathf.Clamp(_dlgIndex, 0, _dlgLines.Length - 1)];
        }

        void AdvanceDialogue()
        {
            if (_dlgLines == null)
                return;
            if (_dlgIndex < _dlgLines.Length - 1)
            {
                _dlgIndex++;
                ShowDlgLine();
                return;
            }

            CloseDialogue(true);
        }

        void CloseDialogue(bool finish)
        {
            if (_dlgPanel != null)
                _dlgPanel.SetActive(false);
            if (GameManager.Instance != null)
                GameManager.Instance.SetTalkLock(false);
            if (finish)
            {
                if (!string.IsNullOrEmpty(_pendingAcceptId) && QuestSystem.Instance != null)
                    QuestSystem.Instance.Accept(QuestCatalog.Get(_pendingAcceptId));
                if (!string.IsNullOrEmpty(_pendingTurnInId) && QuestSystem.Instance != null)
                    QuestSystem.Instance.TurnIn(QuestCatalog.Get(_pendingTurnInId));
            }

            _pendingAcceptId = null;
            _pendingTurnInId = null;
            var cb = _dlgDone;
            _dlgDone = null;
            _dlgLines = null;
            if (cb != null)
                cb();
            RefreshQuest();
        }

        public void ShowGameOver(bool on)
        {
            if (_gameOver != null)
                _gameOver.SetActive(on);
        }

        public void ShowClassSelect(bool on)
        {
            if (_classPanel != null)
                _classPanel.SetActive(on);
            if (_playHud != null)
                _playHud.SetActive(!on);
            if (on)
                HighlightClass(IndexOfDefault());
        }

        public void ShowPlayHud(bool on)
        {
            if (_playHud != null)
                _playHud.SetActive(on);
        }

        int IndexOfDefault()
        {
            var all = PlayerClassCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].classId == PlayerClassCatalog.DefaultClassId || all[i].defaultHighlighted)
                    return i;
            }

            return 0;
        }

        void HighlightClass(int index)
        {
            var all = PlayerClassCatalog.All();
            _selectedClassIndex = Mathf.Clamp(index, 0, all.Count - 1);
            if (_classCards == null)
                return;
            for (int i = 0; i < _classCards.Length; i++)
            {
                if (_classCards[i] == null)
                    continue;
                bool sel = i == _selectedClassIndex;
                // 剑士默认更醒目的描边与放大
                _classCards[i].transform.localScale = sel ? Vector3.one * (all[i].classId == PlayerClassCatalog.SwordsmanId ? 1.08f : 1.04f) : Vector3.one;
                var outline = _classCards[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = sel;
                    outline.effectColor = all[i].primaryColor;
                    outline.effectDistance = sel && all[i].classId == PlayerClassCatalog.SwordsmanId
                        ? new Vector2(8f, -8f)
                        : new Vector2(5f, -5f);
                }
            }
        }

        void ConfirmClass(bool newGame)
        {
            var all = PlayerClassCatalog.All();
            var def = all[Mathf.Clamp(_selectedClassIndex, 0, all.Count - 1)];
            if (GameManager.Instance != null)
                GameManager.Instance.StartAdventure(def, newGame);
            ShowClassSelect(false);
            ShowPlayHud(true);
        }

        void Update()
        {
            AttackHeld = false;
            if (_atkBtn != null && _atkBtn.gameObject.activeInHierarchy)
            {
                // 由按钮事件处理
            }

            if (GameManager.Instance != null && GameManager.Instance.PlayerCombat != null && _skillDim != null)
            {
                var c = GameManager.Instance.PlayerCombat;
                float left = c.SkillCdLeft;
                float tot = Mathf.Max(0.01f, c.SkillCdTotal);
                if (_skillDim != null)
                    _skillDim.fillAmount = left > 0f ? left / tot : 0f;
                if (_skillCd != null)
                {
                    _skillCd.text = left > 0.05f ? Mathf.CeilToInt(left).ToString() : "";
                }
            }
        }

        public void Build(Transform canvas)
        {
            _root = canvas as RectTransform;
            Sprite white = SpriteUtil.White();
            Sprite round = SpriteUtil.RoundedRect(64, 64, 16);
            Sprite circle = SpriteUtil.Circle(128);

            _playHud = New("PlayHud", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildBars(_playHud.transform, white, round);
            BuildJoystick(_playHud.transform, circle);
            BuildButtons(_playHud.transform, circle, white);
            BuildQuest(_playHud.transform, white, round);
            BuildTalkAndDialog(_playHud.transform, white, round);
            BuildGameOver(_playHud.transform, white, round);
            BuildToast(_playHud.transform);
            _classPanel = BuildClassSelect(canvas, white, round);
            _playHud.SetActive(false);
        }

        GameObject New(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            if (aMin == Vector2.zero && aMax == Vector2.one && size == Vector2.zero)
            {
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            return go;
        }

        Image Img(Transform parent, string name, Sprite sp, Color col, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, bool raycast)
        {
            var go = New(name, parent, aMin, aMax, pos, size);
            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.color = col;
            img.raycastTarget = raycast;
            img.type = sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        Text Txt(Transform parent, string name, string content, int fontSize, Color col, TextAnchor anchor, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = New(name, parent, aMin, aMax, pos, size);
            var t = go.AddComponent<Text>();
            t.font = BuiltinFont.Get();
            t.fontSize = fontSize;
            t.color = col;
            t.alignment = anchor;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        void BuildBars(Transform parent, Sprite white, Sprite round)
        {
            var panel = Img(parent, "Bars", round, new Color(0.12f, 0.09f, 0.14f, 0.72f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -80f), new Vector2(480f, 130f), false);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _hpFill = Bar(panel.transform, "Hp", new Color(0.9f, 0.28f, 0.32f), 28f, white);
            _mpFill = Bar(panel.transform, "Mp", new Color(0.3f, 0.5f, 0.95f), -18f, white);
            _hpText = Txt(panel.transform, "HpT", "生命", 16, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, 28f), new Vector2(-40f, 24f));
            _mpText = Txt(panel.transform, "MpT", "法力", 16, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, -18f), new Vector2(-40f, 24f));
            _goldText = Txt(parent, "Gold", "金币  0    经验  0", 20, new Color(1f, 0.92f, 0.65f), TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-220f, -36f), new Vector2(400f, 40f));
        }

        Image Bar(Transform parent, string name, Color col, float y, Sprite white)
        {
            var bg = Img(parent, name + "Bg", white, new Color(0.08f, 0.06f, 0.08f, 0.9f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, y), new Vector2(-40f, 18f), false);
            bg.rectTransform.offsetMin = new Vector2(20f, bg.rectTransform.offsetMin.y);
            bg.rectTransform.offsetMax = new Vector2(-20f, bg.rectTransform.offsetMax.y);
            var fill = Img(bg.transform, "Fill", white, col, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, false);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            return fill;
        }

        void BuildJoystick(Transform parent, Sprite circle)
        {
            var bg = Img(parent, "Joystick", circle, new Color(1f, 1f, 1f, 0.28f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 180f), new Vector2(240f, 240f), true);
            var handle = Img(bg.transform, "Handle", circle, new Color(1f, 1f, 1f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f), false);
            var js = bg.gameObject.AddComponent<VirtualJoystick>();
            js.background = bg.rectTransform;
            js.handle = handle.rectTransform;
            js.handleRange = 70f;
        }

        void BuildButtons(Transform parent, Sprite circle, Sprite white)
        {
            _atkBtn = CircleBtn(parent, "攻击", "斩击", new Vector2(1f, 0f), new Vector2(-170f, 160f), 150f, new Color(0.95f, 0.42f, 0.28f, 0.92f), circle, () =>
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerCombat != null)
                    GameManager.Instance.PlayerCombat.TryAttack();
            });
            var ev = _atkBtn.gameObject.AddComponent<PointerHold>();
            ev.onDown = () => { AttackHeld = true; };
            ev.onUp = () => { AttackHeld = false; };

            _skBtn = CircleBtn(parent, "技能", "旋斩", new Vector2(1f, 0f), new Vector2(-310f, 230f), 120f, new Color(0.45f, 0.4f, 0.95f, 0.92f), circle, () =>
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerCombat != null)
                    GameManager.Instance.PlayerCombat.TrySkill();
            });
            _skillDim = Img(_skBtn.transform, "Cd", circle, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, false);
            _skillDim.rectTransform.offsetMin = Vector2.zero;
            _skillDim.rectTransform.offsetMax = Vector2.zero;
            _skillDim.type = Image.Type.Filled;
            _skillDim.fillMethod = Image.FillMethod.Radial360;
            _skillDim.fillOrigin = (int)Image.Origin360.Top;
            _skillDim.fillClockwise = true;
            _skillDim.fillAmount = 0f;
            _skillCd = Txt(_skBtn.transform, "CdT", "", 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }

        Button CircleBtn(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, float size, Color col, Sprite circle, UnityEngine.Events.UnityAction click)
        {
            var img = Img(parent, name, circle, col, anchor, anchor, pos, new Vector2(size, size), true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(click);
            Txt(img.transform, "L", label, 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            return btn;
        }

        public void RelabelButtons(PlayerClassDefinition def)
        {
            if (def == null)
                return;
            SetBtnLabel(_atkBtn, def.attackLabel);
            SetBtnLabel(_skBtn, def.skillLabel);
        }

        static void SetBtnLabel(Button b, string s)
        {
            if (b == null)
                return;
            var t = b.GetComponentInChildren<Text>();
            if (t != null)
                t.text = s;
        }

        void BuildQuest(Transform parent, Sprite white, Sprite round)
        {
            var panel = Img(parent, "QuestLog", round, new Color(0.12f, 0.09f, 0.14f, 0.72f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -140f), new Vector2(420f, 130f), false);
            Txt(panel.transform, "Tag", "任务", 16, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -14f), new Vector2(-20f, 24f));
            _questTitle = Txt(panel.transform, "Title", "青岚的访客", 22, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -40f), new Vector2(-20f, 30f));
            _questProg = Txt(panel.transform, "Prog", "去古井广场找镇长苏晚晴。", 18, new Color(0.9f, 0.88f, 0.8f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -78f), new Vector2(-20f, 56f));
        }

        void BuildTalkAndDialog(Transform parent, Sprite white, Sprite round)
        {
            var talk = Img(parent, "TalkBtn", round, new Color(1f, 0.82f, 0.3f, 0.95f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-170f, 330f), new Vector2(200f, 70f), true);
            var tbtn = talk.gameObject.AddComponent<Button>();
            tbtn.targetGraphic = talk;
            tbtn.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TryTalk();
            });
            Txt(talk.transform, "L", "交谈", 30, new Color(0.2f, 0.12f, 0.05f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _talkBtn = talk.gameObject;
            _talkBtn.SetActive(false);

            var dlg = Img(parent, "Dialogue", round, new Color(0.12f, 0.09f, 0.16f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(980f, 240f), true);
            _dlgName = Txt(dlg.transform, "Name", "苏晚晴", 26, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -24f), new Vector2(-40f, 32f));
            _dlgBody = Txt(dlg.transform, "Body", "", 24, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 16f), new Vector2(-40f, -70f));
            var next = Img(dlg.transform, "Next", round, new Color(0.95f, 0.7f, 0.25f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-110f, 36f), new Vector2(180f, 48f), true);
            var nb = next.gameObject.AddComponent<Button>();
            nb.targetGraphic = next;
            nb.onClick.AddListener(AdvanceDialogue);
            Txt(next.transform, "L", "继续", 22, new Color(0.2f, 0.1f, 0.05f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _dlgPanel = dlg.gameObject;
            _dlgPanel.SetActive(false);
        }

        void BuildGameOver(Transform parent, Sprite white, Sprite round)
        {
            var panel = Img(parent, "GameOver", white, new Color(0.05f, 0.03f, 0.07f, 0.82f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            Txt(panel.transform, "T", "你倒下了", 56, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(600f, 80f));
            Txt(panel.transform, "S", "蚀雾还在镇外徘徊。要在古井广场重新站起来吗？", 22, new Color(0.9f, 0.85f, 0.75f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 50f));
            var a = Img(panel.transform, "Retry", round, new Color(0.95f, 0.55f, 0.28f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-140f, -80f), new Vector2(240f, 70f), true);
            var ab = a.gameObject.AddComponent<Button>();
            ab.targetGraphic = a;
            ab.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ResurrectInTown();
            });
            Txt(a.transform, "L", "回到小镇", 28, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var b = Img(panel.transform, "Menu", round, new Color(0.4f, 0.35f, 0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, -80f), new Vector2(240f, 70f), true);
            var bb = b.gameObject.AddComponent<Button>();
            bb.targetGraphic = b;
            bb.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ReturnToTitle();
            });
            Txt(b.transform, "L", "返回标题", 28, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _gameOver = panel.gameObject;
            _gameOver.SetActive(false);
        }

        void BuildToast(Transform parent)
        {
            _toast = Txt(parent, "Toast", "", 26, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(900f, 60f));
            _toast.gameObject.SetActive(false);
        }

        GameObject BuildClassSelect(Transform canvas, Sprite white, Sprite round)
        {
            var panel = Img(canvas, "ClassSelect", white, new Color(0.07f, 0.06f, 0.12f, 0.96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            Txt(panel.transform, "Title", "萌域传说", 72, new Color(1f, 0.86f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 90f));
            Txt(panel.transform, "Sub", "Chibi Realm Legends    ·    青岚镇的风里，已经能闻到蚀雾的锈味", 22, new Color(0.85f, 0.82f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1100f, 40f));
            Txt(panel.transform, "Hint", "选择你的道路。剑士是推荐的开荒职业，连斩手感最为扎实。", 20, new Color(1f, 0.75f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1100f, 36f));

            var all = PlayerClassCatalog.All();
            _classCards = new Image[all.Count];
            float startX = -(all.Count - 1) * 0.5f * 340f;
            for (int i = 0; i < all.Count; i++)
            {
                int idx = i;
                var def = all[i];
                var card = Img(panel.transform, def.classId, round, new Color(0.16f, 0.13f, 0.22f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX + i * 340f, 10f), new Vector2(300f, 360f), true);
                var ol = card.gameObject.AddComponent<Outline>();
                ol.effectColor = def.primaryColor;
                ol.effectDistance = new Vector2(5f, -5f);
                ol.enabled = def.defaultHighlighted;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => HighlightClass(idx));
                Txt(card.transform, "N", def.displayName, 36, def.primaryColor, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -24f), new Vector2(-16f, 44f));
                Txt(card.transform, "E", def.englishName, 16, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(-16f, 24f));
                Txt(card.transform, "S", def.subtitle, 18, new Color(1f, 0.9f, 0.6f), TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-20f, 28f));
                Txt(card.transform, "D", def.description + "\n\n生命 " + def.maxHp + "    法力 " + def.maxMp, 16, new Color(0.92f, 0.9f, 0.88f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(0f, -20f), new Vector2(-28f, -150f));
                if (def.classId == PlayerClassCatalog.SwordsmanId)
                    Txt(card.transform, "Rec", "推荐", 18, new Color(1f, 0.85f, 0.3f), TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 16f), new Vector2(-10f, 28f));
                _classCards[i] = card;
            }

            var go = Img(panel.transform, "Go", round, new Color(0.95f, 0.5f, 0.25f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(280f, 72f), true);
            var gb = go.gameObject.AddComponent<Button>();
            gb.targetGraphic = go;
            gb.onClick.AddListener(() => ConfirmClass(true));
            Txt(go.transform, "L", "踏上旅途", 30, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            if (SaveGame.LoadOrNew().hasSave)
            {
                var cont = Img(panel.transform, "Continue", round, new Color(0.35f, 0.55f, 0.85f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 90f), new Vector2(240f, 72f), true);
                var cb = cont.gameObject.AddComponent<Button>();
                cb.targetGraphic = cont;
                cb.onClick.AddListener(() =>
                {
                    var data = SaveGame.LoadOrNew();
                    var def = PlayerClassCatalog.Get(data.classId);
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartAdventure(def, false);
                    ShowClassSelect(false);
                    ShowPlayHud(true);
                });
                Txt(cont.transform, "L", "继续冒险", 28, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            return panel.gameObject;
        }
    }

    public class PointerHold : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public System.Action onDown;
        public System.Action onUp;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (onDown != null)
                onDown();
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (onUp != null)
                onUp();
        }
    }
}
