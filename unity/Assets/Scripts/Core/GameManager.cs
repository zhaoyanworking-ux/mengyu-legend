using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChibiARPG
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameObject Player { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public PlayerClassDefinition CurrentClass { get; private set; }
        public bool IsPaused { get; private set; }
        public bool InMenu { get; private set; }
        public bool TalkLocked { get; private set; }
        public Transform PlazaPoint { get; set; }
        public EnemySpawner BossSpawner { get; set; }
        public bool MiniBossDefeated { get; private set; }

        NpcCharacter _nearNpc;
        float _mpTick;

        void Awake()
        {
            Instance = this;
            InMenu = true;
            SaveGame.LoadOrNew();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (InMenu || IsPaused)
                return;
            UpdateTalkPrompt();
            RegenMp();
        }

        void RegenMp()
        {
            if (Player == null)
                return;
            var s = Player.GetComponent<CharacterStats>();
            if (s == null || s.IsDead)
                return;
            _mpTick += Time.deltaTime;
            if (_mpTick >= 0.5f)
            {
                _mpTick = 0f;
                s.RestoreMp(2f);
            }
        }

        void UpdateTalkPrompt()
        {
            _nearNpc = null;
            if (Player == null || TalkLocked)
            {
                if (GameHud.Instance != null)
                    GameHud.Instance.SetTalkVisible(false);
                return;
            }

            float best = 999f;
            var list = NpcCharacter.All.Items;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n == null || !n.InRange(Player.transform))
                    continue;
                float d = Vector3.Distance(Player.transform.position, n.transform.position);
                if (d < best)
                {
                    best = d;
                    _nearNpc = n;
                }
            }

            if (GameHud.Instance != null)
                GameHud.Instance.SetTalkVisible(_nearNpc != null);
        }

        public void SetTalkLock(bool on)
        {
            TalkLocked = on;
            IsPaused = on;
            if (!on && GameHud.Instance != null && GameHud.Instance.GetComponentInChildren<Transform>() != null)
            {
                // 对话关闭后恢复，若已死亡则保持由复活界面处理
                var stats = Player != null ? Player.GetComponent<CharacterStats>() : null;
                if (stats == null || !stats.IsDead)
                    Time.timeScale = 1f;
            }
        }

        public void TryTalk()
        {
            if (TalkLocked || Player == null)
                return;
            if (_nearNpc == null)
                UpdateTalkPrompt();
            if (_nearNpc == null)
                return;

            var qs = QuestSystem.Instance;
            QuestDefinition q = qs != null ? qs.InteractableQuest(_nearNpc.npcId) : null;
            string acceptId = null;
            string turnInId = null;
            string[] lines;
            string speaker = _nearNpc.displayName;

            if (q != null)
            {
                var st = qs.StateOf(q.questId);
                if (st == QuestState.Available && q.giverNpcId == _nearNpc.npcId)
                {
                    lines = Concat(q.greetLines, q.acceptLines);
                    acceptId = q.questId;
                }
                else if (st == QuestState.ReadyToTurnIn && q.turnInNpcId == _nearNpc.npcId)
                {
                    lines = q.turnInLines;
                    turnInId = q.questId;
                }
                else if (st == QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo && q.targetId == _nearNpc.npcId)
                {
                    qs.NotifyTalk(_nearNpc.npcId);
                    if (q.turnInNpcId == _nearNpc.npcId)
                    {
                        lines = q.turnInLines;
                        turnInId = q.questId;
                    }
                    else
                        lines = Concat(q.turnInLines, q.progressLines);
                }
                else if (st == QuestState.Active)
                    lines = q.progressLines != null && q.progressLines.Length > 0 ? q.progressLines : qs.IdleLines(_nearNpc.npcId);
                else
                    lines = qs.IdleLines(_nearNpc.npcId);
            }
            else
                lines = qs != null ? qs.IdleLines(_nearNpc.npcId) : new[] { "……" };

            if (GameHud.Instance != null)
                GameHud.Instance.ShowDialogue(speaker, lines, acceptId, turnInId, null);
        }

        static string[] Concat(string[] a, string[] b)
        {
            int la = a != null ? a.Length : 0;
            int lb = b != null ? b.Length : 0;
            var r = new string[la + lb];
            if (la > 0) System.Array.Copy(a, r, la);
            if (lb > 0) System.Array.Copy(b, 0, r, la, lb);
            return r.Length > 0 ? r : new[] { "……" };
        }

        public void StartAdventure(PlayerClassDefinition def, bool newGame)
        {
            if (def == null)
                def = PlayerClassCatalog.Get(PlayerClassCatalog.DefaultClassId);
            CurrentClass = def;
            if (newGame)
            {
                SaveGame.NewGame(def.classId);
                SaveGame.Persist();
                MiniBossDefeated = false;
                if (QuestSystem.Instance != null)
                    QuestSystem.Instance.EnsureUnlock();
            }
            else
            {
                var data = SaveGame.LoadOrNew();
                data.classId = def.classId;
                SaveGame.Persist();
            }

            if (Player != null)
                Destroy(Player);

            Vector3 spawn = PlazaPoint != null ? PlazaPoint.position : new Vector3(0f, 0f, -2f);
            Player = DemoWorldBuilder.SpawnPlayer(spawn, def);
            PlayerCombat = Player.GetComponent<PlayerCombat>();
            var cam = Object.FindObjectOfType<FollowCamera>();
            if (cam != null)
            {
                cam.target = Player.transform;
                cam.Snap();
            }

            var stats = Player.GetComponent<CharacterStats>();
            if (GameHud.Instance != null)
            {
                GameHud.Instance.BindPlayer(stats);
                GameHud.Instance.RelabelButtons(def);
                GameHud.Instance.ShowGameOver(false);
                GameHud.Instance.RefreshQuest();
            }

            InMenu = false;
            IsPaused = false;
            Time.timeScale = 1f;
            if (GameHud.Instance != null)
                GameHud.Instance.Toast("欢迎来到青岚镇，" + def.displayName);
        }

        public void ResurrectInTown()
        {
            if (Player == null)
                return;
            var stats = Player.GetComponent<CharacterStats>();
            var cc = Player.GetComponent<CharacterController>();
            Vector3 pos = PlazaPoint != null ? PlazaPoint.position : new Vector3(0f, 0f, -2f);
            if (cc != null)
                cc.enabled = false;
            Player.transform.position = pos;
            if (cc != null)
                cc.enabled = true;
            if (stats != null)
            {
                stats.ResetStats();
                var anim = Player.GetComponent<ChibiAnim>();
                if (anim != null)
                    anim.dead = false;
                Player.transform.localScale = Vector3.one;
            }

            var cam = Object.FindObjectOfType<FollowCamera>();
            if (cam != null)
                cam.Snap();
            if (GameHud.Instance != null)
            {
                GameHud.Instance.ShowGameOver(false);
                GameHud.Instance.BindPlayer(stats);
            }

            IsPaused = false;
            Time.timeScale = 1f;
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            InMenu = true;
            IsPaused = false;
            if (Player != null)
            {
                Destroy(Player);
                Player = null;
                PlayerCombat = null;
            }

            if (GameHud.Instance != null)
            {
                GameHud.Instance.ShowGameOver(false);
                GameHud.Instance.ShowClassSelect(true);
            }
        }

        public void TrySpawnMiniBoss()
        {
            if (MiniBossDefeated)
                return;
            if (BossSpawner != null)
            {
                var go = BossSpawner.SpawnBoss();
                if (go != null && GameHud.Instance != null)
                    GameHud.Instance.Toast("雾石坡深处，蚀雾史莱姆显形了！");
            }
        }

        public void OnMiniBossDefeated()
        {
            MiniBossDefeated = true;
            if (GameHud.Instance != null)
                GameHud.Instance.Toast("蚀雾史莱姆倒下了。回古井广场向苏晚晴复命。");
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
