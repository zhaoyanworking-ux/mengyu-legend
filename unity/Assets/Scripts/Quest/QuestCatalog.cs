using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    /// <summary>
    /// 《萌域传说》开场任务链：青岚镇遭遇渐渐逼近的蚀雾。原创高幻想，与任何现有作品无关。
    /// </summary>
    public static class QuestCatalog
    {
        public const string NpcMayor = "mayor_su";
        public const string NpcHerbalist = "herbalist_ye";
        public const string NpcGuard = "guard_shi";

        public const string QuestWelcome = "q_qinglan_visitor";
        public const string QuestCull = "q_mist_claws";
        public const string QuestSource = "q_cut_the_source";

        public const string MonsterFodder = "blightbeast";
        public const string ItemShard = "blight_shard";
        public const string MiniBossId = "eclipse_vanguard";

        static List<QuestDefinition> _all;

        public static List<QuestDefinition> All()
        {
            if (_all != null && _all.Count > 0)
                return _all;

            _all = new List<QuestDefinition>();
            var loaded = Resources.LoadAll<QuestDefinition>("Data");
            if (loaded != null && loaded.Length > 0)
            {
                _all.AddRange(loaded);
                return _all;
            }

            _all.Add(Welcome());
            _all.Add(Cull());
            _all.Add(Source());
            return _all;
        }

        public static QuestDefinition Get(string id)
        {
            var list = All();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].questId == id)
                    return list[i];
            }

            return null;
        }

        static QuestDefinition Welcome()
        {
            var q = ScriptableObject.CreateInstance<QuestDefinition>();
            q.name = "Quest_Welcome";
            q.questId = QuestWelcome;
            q.title = "青岚的访客";
            q.journalText = "青岚镇镇长苏晚晴似乎有话要说。先去古井广场听听镇里的消息。";
            q.giverNpcId = NpcMayor;
            q.turnInNpcId = NpcGuard;
            q.objectiveType = QuestObjectiveType.TalkTo;
            q.targetId = NpcGuard;
            q.targetCount = 1;
            q.rewardGold = 20;
            q.rewardXp = 40;
            q.nextQuestId = QuestCull;
            q.greetLines = new[]
            {
                "旅人，你来得正好。我是青岚镇的镇长，苏晚晴。",
                "往年这个时节，雾石坡还开着浅金色的小花。可最近，一股叫「蚀雾」的黑气顺着谷风爬了过来。",
                "草木失了颜色，野兽的眼睛也变成了浊黄。镇民虽未惊慌，却已经不敢走远门。"
            };
            q.acceptLines = new[]
            {
                "劳烦你先去镇门口，见见守门的石川。他把守着通往雾石坡的关口，比我更清楚外面的情形。",
                "告诉他：镇里已经有人愿意把剑指向那片雾。"
            };
            q.progressLines = new[]
            {
                "石川就在镇门边。顺着石板路一直往北走就到。"
            };
            q.turnInLines = new[]
            {
                "……镇长让你来的？好。至少说明镇里还没有把希望锁在门闩后面。",
                "我是石川，守这道门已经六年。门以外的事，从今天起，也算你的了。"
            };
            q.completedIdleLines = new[]
            {
                "门还开着。雾却比清晨更近了一步。"
            };
            return q;
        }

        static QuestDefinition Cull()
        {
            var q = ScriptableObject.CreateInstance<QuestDefinition>();
            q.name = "Quest_Cull";
            q.questId = QuestCull;
            q.title = "雾边的爪痕";
            q.journalText = "在雾石坡击败 5 只蚀斑史莱姆，减轻它们对青岚镇门的压迫，然后向石川复命。";
            q.giverNpcId = NpcGuard;
            q.turnInNpcId = NpcGuard;
            q.objectiveType = QuestObjectiveType.Kill;
            q.targetId = MonsterFodder;
            q.targetCount = 5;
            q.rewardGold = 55;
            q.rewardXp = 90;
            q.nextQuestId = QuestSource;
            q.greetLines = new[]
            {
                "你若出了镇门，就会看见那些被蚀雾浸透的野兽——我们叫它们「蚀斑史莱姆」。",
                "它们不太敢冲进石板路，却在坡上转圈，把路旁的灌木抓得全是黑印。"
            };
            q.acceptLines = new[]
            {
                "去击败 5 只蚀斑史莱姆。不必穷追到谷底，把镇门前这一圈清一清就好。",
                "活着回来，比多砍一只更重要。"
            };
            q.progressLines = new[]
            {
                "还不够。蚀雾未散的时候，少一只，门前就少一分抓挠声。"
            };
            q.turnInLines = new[]
            {
                "刀上还有雾气凝成的水珠……看来你真的去过了。",
                "镇门今夜可以松半口气。不过，药师叶青托我传话：她在等你。"
            };
            q.completedIdleLines = new[]
            {
                "清过一轮，它们还会再聚。蚀雾不走，兽就不会停。"
            };
            return q;
        }

        static QuestDefinition Source()
        {
            var q = ScriptableObject.CreateInstance<QuestDefinition>();
            q.name = "Quest_Source";
            q.questId = QuestSource;
            q.title = "切断蚀源";
            q.journalText = "从蚀斑史莱姆身上收集 3 枚蚀核碎片交给药师叶青。她会凭此感应雾中的源头——可能要面对蚀影先锋。";
            q.giverNpcId = NpcHerbalist;
            q.turnInNpcId = NpcMayor;
            q.objectiveType = QuestObjectiveType.Collect;
            q.targetId = ItemShard;
            q.targetCount = 3;
            q.rewardGold = 120;
            q.rewardXp = 180;
            q.spawnsMiniBossOnComplete = true;
            q.greetLines = new[]
            {
                "我是叶青，管着镇里那一棚还没被雾熏黑的药草。",
                "蚀雾不是普通的潮气。它会在兽骨里凝成深紫色的硬块，我们叫它「蚀核碎片」。",
                "三枚碎片就能让我看清雾里那道最浓的影子落在何处。"
            };
            q.acceptLines = new[]
            {
                "去雾石坡收集 3 枚蚀核碎片，带回来给我。",
                "若雾突然收紧、地面发颤——不要怕，那只说明源头愿意露面。把它斩断，再回古井广场找镇长。"
            };
            q.progressLines = new[]
            {
                "碎片还不够。每枚都冰凉，像含着一口没散尽的夜。"
            };
            q.turnInLines = new[]
            {
                "苏晚晴：碎片的纹路……与镇外那道黑影同出一源。你已经让「蚀影先锋」退了。",
                "青岚镇今夜能点灯到很晚。蚀雾并未消失，可它第一次被推了回去。",
                "旅人，青岚记住你的剑、你的光，还有你没有在雾门口转身离开。这只是萌域边境的第一页——但第一页，已经写好了。"
            };
            q.completedIdleLines = new[]
            {
                "风里还带着一丝锈味。可广场的井水，又清了些。"
            };
            return q;
        }
    }
}
