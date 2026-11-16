using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    public static class PlayerClassCatalog
    {
        public const string SwordsmanId = "swordsman";
        public const string MageId = "mage";
        public const string ArcherId = "archer";
        public const string DefaultClassId = SwordsmanId;

        static List<PlayerClassDefinition> _all;

        public static List<PlayerClassDefinition> All()
        {
            if (_all != null && _all.Count > 0)
                return _all;

            _all = new List<PlayerClassDefinition>();
            var loaded = Resources.LoadAll<PlayerClassDefinition>("Data");
            if (loaded != null && loaded.Length > 0)
            {
                _all.AddRange(loaded);
                _all.Sort(CompareDefaultFirst);
                return _all;
            }

            _all.Add(CreateSwordsman());
            _all.Add(CreateMage());
            _all.Add(CreateArcher());
            return _all;
        }

        public static PlayerClassDefinition Get(string id)
        {
            var list = All();
            if (string.IsNullOrEmpty(id))
                id = DefaultClassId;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].classId == id)
                    return list[i];
            }

            return list.Count > 0 ? list[0] : CreateSwordsman();
        }

        static int CompareDefaultFirst(PlayerClassDefinition a, PlayerClassDefinition b)
        {
            if (a == null) return 1;
            if (b == null) return -1;
            if (a.classId == DefaultClassId) return -1;
            if (b.classId == DefaultClassId) return 1;
            return string.CompareOrdinal(a.classId, b.classId);
        }

        public static PlayerClassDefinition CreateSwordsman()
        {
            var c = ScriptableObject.CreateInstance<PlayerClassDefinition>();
            c.name = "Class_Swordsman";
            c.classId = SwordsmanId;
            c.displayName = "剑士";
            c.englishName = "Swordsman";
            c.subtitle = "近战连斩 · 高生命";
            c.description = "银蓝铠甲、鲜红披风。三段连击带顿帧与击退，旋斩可清退周围的蚀斑兽。最适合冲入雾石坡的前线。";
            c.primaryColor = new Color(0.55f, 0.7f, 0.88f);
            c.secondaryColor = new Color(0.77f, 0.16f, 0.19f);
            c.maxHp = 160f;
            c.maxMp = 50f;
            c.moveSpeed = 5.7f;
            c.attackStyle = AttackStyle.MeleeCombo;
            c.attackDamage = 18f;
            c.attackCooldown = 0.22f;
            c.comboHits = 3;
            c.skillStyle = SkillStyle.SpinSlash;
            c.skillMpCost = 16f;
            c.skillCooldown = 5.5f;
            c.skillDamage = 26f;
            c.attackLabel = "斩击";
            c.skillLabel = "旋斩";
            c.defaultHighlighted = true;
            return c;
        }

        public static PlayerClassDefinition CreateMage()
        {
            var c = ScriptableObject.CreateInstance<PlayerClassDefinition>();
            c.name = "Class_Mage";
            c.classId = MageId;
            c.displayName = "法师";
            c.englishName = "Mage";
            c.subtitle = "远程法术 · 高法力";
            c.description = "白袍浅紫、尖顶宽沿帽，金杖托着岚光蓝珠。普通攻击射出魔弹，技能在前方引发范围魔爆。";
            c.primaryColor = new Color(0.83f, 0.75f, 0.95f);
            c.secondaryColor = new Color(0.3f, 0.7f, 1f);
            c.maxHp = 90f;
            c.maxMp = 140f;
            c.moveSpeed = 5.2f;
            c.attackStyle = AttackStyle.MagicBolt;
            c.attackDamage = 20f;
            c.attackCooldown = 0.55f;
            c.comboHits = 1;
            c.projectileSpeed = 11f;
            c.projectileRange = 11f;
            c.skillStyle = SkillStyle.AoeBurst;
            c.skillMpCost = 28f;
            c.skillCooldown = 7f;
            c.skillDamage = 36f;
            c.attackLabel = "魔弹";
            c.skillLabel = "魔爆";
            return c;
        }

        public static PlayerClassDefinition CreateArcher()
        {
            var c = ScriptableObject.CreateInstance<PlayerClassDefinition>();
            c.name = "Class_Archer";
            c.classId = ArcherId;
            c.displayName = "弓箭手";
            c.englishName = "Archer";
            c.subtitle = "急速射击 · 均衡";
            c.description = "带耳尖的绿风帽、奶油色衬衣与棕色皮甲，背着箭袋与木弓。普攻快速射箭，技能扇形连射。";
            c.primaryColor = new Color(0.31f, 0.6f, 0.27f);
            c.secondaryColor = new Color(0.95f, 0.9f, 0.8f);
            c.maxHp = 120f;
            c.maxMp = 80f;
            c.moveSpeed = 5.9f;
            c.attackStyle = AttackStyle.BowShot;
            c.attackDamage = 12f;
            c.attackCooldown = 0.32f;
            c.comboHits = 1;
            c.projectileSpeed = 18f;
            c.projectileRange = 13f;
            c.skillStyle = SkillStyle.MultiShot;
            c.skillMpCost = 20f;
            c.skillCooldown = 6f;
            c.skillDamage = 11f;
            c.attackLabel = "射击";
            c.skillLabel = "连射";
            return c;
        }
    }
}
