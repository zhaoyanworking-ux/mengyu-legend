using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        public event Action OnChanged;

        public string ActiveTitle
        {
            get
            {
                var q = TrackedQuest();
                return q == null ? "" : q.title;
            }
        }

        public string ActiveProgressText
        {
            get
            {
                var q = TrackedQuest();
                if (q == null)
                    return SaveGame.LoadOrNew().storyDone ? "蚀雾暂时被推退了。在青岚镇走走吧。" : "去古井广场找镇长苏晚晴。";
                var st = StateOf(q.questId);
                int prog = ProgressOf(q.questId);
                if (st == QuestState.Available)
                    return "与「" + NpcName(q.giverNpcId) + "」交谈。";
                if (st == QuestState.ReadyToTurnIn)
                    return "向「" + NpcName(q.turnInNpcId) + "」复命。";
                if (q.objectiveType == QuestObjectiveType.TalkTo)
                    return "去找「" + NpcName(q.targetId) + "」。";
                if (q.objectiveType == QuestObjectiveType.Kill)
                    return "击败蚀斑史莱姆  " + prog + " / " + q.targetCount;
                if (q.objectiveType == QuestObjectiveType.Collect)
                    return "收集蚀核碎片  " + prog + " / " + q.targetCount;
                return q.journalText;
            }
        }

        void Awake()
        {
            Instance = this;
            EnsureUnlock();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void EnsureUnlock()
        {
            var first = QuestCatalog.Get(QuestCatalog.QuestWelcome);
            if (first == null)
                return;
            var e = SaveGame.GetQuest(first.questId);
            if (e.state == (int)QuestState.Locked)
                e.state = (int)QuestState.Available;
        }

        public QuestDefinition TrackedQuest()
        {
            var all = QuestCatalog.All();
            QuestDefinition ready = null;
            QuestDefinition active = null;
            QuestDefinition avail = null;
            for (int i = 0; i < all.Count; i++)
            {
                var st = StateOf(all[i].questId);
                if (st == QuestState.ReadyToTurnIn && ready == null)
                    ready = all[i];
                if (st == QuestState.Active && active == null)
                    active = all[i];
                if (st == QuestState.Available && avail == null)
                    avail = all[i];
            }

            if (ready != null) return ready;
            if (active != null) return active;
            return avail;
        }

        public QuestState StateOf(string id)
        {
            return (QuestState)SaveGame.GetQuest(id).state;
        }

        public int ProgressOf(string id)
        {
            return SaveGame.GetQuest(id).progress;
        }

        public char MarkerForNpc(string npcId)
        {
            var all = QuestCatalog.All();
            bool turnIn = false, available = false, activeTalk = false;
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var st = StateOf(q.questId);
                if (st == QuestState.ReadyToTurnIn && q.turnInNpcId == npcId)
                    turnIn = true;
                if (st == QuestState.Available && q.giverNpcId == npcId)
                    available = true;
                if (st == QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo && q.targetId == npcId)
                    activeTalk = true;
            }

            if (turnIn || activeTalk)
                return '?';
            if (available)
                return '!';
            return ' ';
        }

        public QuestDefinition InteractableQuest(string npcId)
        {
            var all = QuestCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var st = StateOf(q.questId);
                if (st == QuestState.ReadyToTurnIn && q.turnInNpcId == npcId)
                    return q;
                if (st == QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo && q.targetId == npcId)
                    return q;
                if (st == QuestState.Available && q.giverNpcId == npcId)
                    return q;
                if ((st == QuestState.Active || st == QuestState.Completed) && (q.giverNpcId == npcId || q.turnInNpcId == npcId))
                {
                    // keep last as flavor
                }
            }

            return null;
        }

        public string[] IdleLines(string npcId)
        {
            var all = QuestCatalog.All();
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var q = all[i];
                if (q.giverNpcId != npcId && q.turnInNpcId != npcId)
                    continue;
                var st = StateOf(q.questId);
                if (st == QuestState.Completed && q.completedIdleLines != null && q.completedIdleLines.Length > 0)
                    return q.completedIdleLines;
                if (st == QuestState.Active && q.progressLines != null && q.progressLines.Length > 0 && q.giverNpcId == npcId)
                    return q.progressLines;
            }

            if (npcId == QuestCatalog.NpcMayor)
                return new[] { "青岚镇虽小，却不愿把灯先熄掉。" };
            if (npcId == QuestCatalog.NpcHerbalist)
                return new[] { "这些药草还撑得住。雾若再浓，花瓣就会先皱起来。" };
            return new[] { "镇门以内，你是客人；镇门以外，你得自己小心。" };
        }

        public void Accept(QuestDefinition q)
        {
            if (q == null)
                return;
            var e = SaveGame.GetQuest(q.questId);
            if (e.state != (int)QuestState.Available)
                return;
            e.state = (int)QuestState.Active;
            e.progress = 0;
            if (q.objectiveType == QuestObjectiveType.TalkTo)
            {
                // 与目标 NPC 对话时再推进
            }

            SaveGame.Persist();
            Raise();
            if (GameHud.Instance != null)
                GameHud.Instance.Toast("接受任务：" + q.title);
        }

        public void NotifyTalk(string npcId)
        {
            var all = QuestCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var e = SaveGame.GetQuest(q.questId);
                if (e.state == (int)QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo && q.targetId == npcId)
                {
                    e.progress = q.targetCount;
                    e.state = (int)QuestState.ReadyToTurnIn;
                    SaveGame.Persist();
                    Raise();
                }
            }
        }

        public void NotifyKill(string enemyId)
        {
            SaveGame.LoadOrNew().kills++;
            var all = QuestCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var e = SaveGame.GetQuest(q.questId);
                if (e.state != (int)QuestState.Active)
                    continue;
                if (q.objectiveType != QuestObjectiveType.Kill)
                    continue;
                if (q.targetId != enemyId && enemyId != QuestCatalog.MiniBossId)
                    continue;
                e.progress = Mathf.Min(q.targetCount, e.progress + 1);
                if (e.progress >= q.targetCount)
                    e.state = (int)QuestState.ReadyToTurnIn;
                SaveGame.Persist();
                Raise();
            }
        }

        public bool WantsItem(string itemId)
        {
            var all = QuestCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var e = SaveGame.GetQuest(q.questId);
                if (e.state == (int)QuestState.Active && q.objectiveType == QuestObjectiveType.Collect && q.targetId == itemId && e.progress < q.targetCount)
                    return true;
            }

            return false;
        }

        public void NotifyCollect(string itemId, int count)
        {
            SaveGame.LoadOrNew().shards += count;
            var all = QuestCatalog.All();
            for (int i = 0; i < all.Count; i++)
            {
                var q = all[i];
                var e = SaveGame.GetQuest(q.questId);
                if (e.state != (int)QuestState.Active)
                    continue;
                if (q.objectiveType != QuestObjectiveType.Collect || q.targetId != itemId)
                    continue;
                e.progress = Mathf.Min(q.targetCount, e.progress + count);
                if (e.progress >= q.targetCount)
                {
                    e.state = (int)QuestState.ReadyToTurnIn;
                    if (q.spawnsMiniBossOnComplete && GameManager.Instance != null)
                        GameManager.Instance.TrySpawnMiniBoss();
                }

                SaveGame.Persist();
                Raise();
            }
        }

        public void TurnIn(QuestDefinition q)
        {
            if (q == null)
                return;
            var e = SaveGame.GetQuest(q.questId);
            if (e.state != (int)QuestState.ReadyToTurnIn &&
                !(e.state == (int)QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo && e.progress >= q.targetCount))
            {
                if (e.state == (int)QuestState.Active && q.objectiveType == QuestObjectiveType.TalkTo)
                {
                    e.progress = q.targetCount;
                    e.state = (int)QuestState.ReadyToTurnIn;
                }
                else if (e.state != (int)QuestState.ReadyToTurnIn)
                    return;
            }

            e.state = (int)QuestState.Completed;
            e.progress = q.targetCount;
            var data = SaveGame.LoadOrNew();
            data.gold += q.rewardGold;
            data.xp += q.rewardXp;
            if (!string.IsNullOrEmpty(q.nextQuestId))
            {
                var n = SaveGame.GetQuest(q.nextQuestId);
                if (n.state == (int)QuestState.Locked)
                    n.state = (int)QuestState.Available;
            }
            else
                data.storyDone = true;

            SaveGame.Persist();
            Raise();
            if (GameHud.Instance != null)
                GameHud.Instance.Toast("完成：" + q.title + "  +" + q.rewardGold + " 金  +" + q.rewardXp + " 经验");
        }

        public static string NpcName(string id)
        {
            if (id == QuestCatalog.NpcMayor) return "苏晚晴";
            if (id == QuestCatalog.NpcHerbalist) return "叶青";
            if (id == QuestCatalog.NpcGuard) return "石川";
            return "居民";
        }

        void Raise()
        {
            if (OnChanged != null)
                OnChanged();
        }
    }
}
