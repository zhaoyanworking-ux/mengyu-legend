using UnityEngine;

namespace ChibiARPG
{
    public enum AttackStyle
    {
        MeleeCombo,
        MagicBolt,
        BowShot
    }

    public enum SkillStyle
    {
        SpinSlash,
        AoeBurst,
        MultiShot
    }

    /// <summary>
    /// 职业数据（ScriptableObject）。编辑器可保存为资产，运行时也可 CreateInstance。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerClass", menuName = "萌域传说/玩家职业", order = 0)]
    public class PlayerClassDefinition : ScriptableObject
    {
        public string classId = "swordsman";
        public string displayName = "剑士";
        public string englishName = "Swordsman";
        public string subtitle = "近战连斩 · 高生命";
        public string description = "以剑开路的近战专家。三段连击手感扎实，旋斩可清退周围的蚀斑兽。";
        public Color primaryColor = new Color(0.55f, 0.7f, 0.88f);
        public Color secondaryColor = new Color(0.77f, 0.16f, 0.19f);
        public float maxHp = 160f;
        public float maxMp = 50f;
        public float moveSpeed = 5.6f;
        public AttackStyle attackStyle = AttackStyle.MeleeCombo;
        public float attackDamage = 16f;
        public float attackCooldown = 0.28f;
        public int comboHits = 3;
        public float projectileSpeed = 12f;
        public float projectileRange = 10f;
        public SkillStyle skillStyle = SkillStyle.SpinSlash;
        public float skillMpCost = 18f;
        public float skillCooldown = 6f;
        public float skillDamage = 22f;
        public string attackLabel = "斩击";
        public string skillLabel = "旋斩";
        public bool defaultHighlighted;
    }
}
