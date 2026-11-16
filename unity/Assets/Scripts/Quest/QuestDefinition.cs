using UnityEngine;

namespace ChibiARPG
{
    public enum QuestObjectiveType
    {
        TalkTo,
        Kill,
        Collect
    }

    public enum QuestState
    {
        Locked,
        Available,
        Active,
        ReadyToTurnIn,
        Completed
    }

    [CreateAssetMenu(fileName = "Quest", menuName = "萌域传说/任务", order = 1)]
    public class QuestDefinition : ScriptableObject
    {
        public string questId;
        public string title;
        public string journalText;
        public string giverNpcId;
        public string turnInNpcId;
        public QuestObjectiveType objectiveType;
        public string targetId;
        public int targetCount = 1;
        public int rewardGold;
        public int rewardXp;
        public string nextQuestId;
        public bool spawnsMiniBossOnComplete;
        public string[] greetLines;
        public string[] acceptLines;
        public string[] progressLines;
        public string[] turnInLines;
        public string[] completedIdleLines;
    }
}
