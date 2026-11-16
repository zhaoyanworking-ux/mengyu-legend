using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChibiARPG
{
    [Serializable]
    public class QuestSaveEntry
    {
        public string id;
        public int state;
        public int progress;
    }

    [Serializable]
    public class SaveData
    {
        public string classId = PlayerClassCatalog.DefaultClassId;
        public int gold;
        public int xp;
        public int shards;
        public int kills;
        public bool hasSave;
        public bool storyDone;
        public List<QuestSaveEntry> quests = new List<QuestSaveEntry>();
    }

    public static class SaveGame
    {
        const string Key = "MengYuLegend_Save_v1";

        public static SaveData Current { get; private set; }

        public static SaveData LoadOrNew()
        {
            if (Current != null)
                return Current;
            if (PlayerPrefs.HasKey(Key))
            {
                try
                {
                    Current = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                    if (Current != null)
                    {
                        if (Current.quests == null)
                            Current.quests = new List<QuestSaveEntry>();
                        Current.hasSave = true;
                        if (string.IsNullOrEmpty(Current.classId))
                            Current.classId = PlayerClassCatalog.DefaultClassId;
                        return Current;
                    }
                }
                catch (Exception)
                {
                    Current = null;
                }
            }

            Current = NewGame(PlayerClassCatalog.DefaultClassId);
            Current.hasSave = false;
            return Current;
        }

        public static SaveData NewGame(string classId)
        {
            Current = new SaveData
            {
                classId = string.IsNullOrEmpty(classId) ? PlayerClassCatalog.DefaultClassId : classId,
                gold = 0,
                xp = 0,
                shards = 0,
                kills = 0,
                hasSave = false,
                storyDone = false,
                quests = new List<QuestSaveEntry>()
            };
            return Current;
        }

        public static void Persist()
        {
            if (Current == null)
                return;
            Current.hasSave = true;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            Current = null;
        }

        public static QuestSaveEntry GetQuest(string id)
        {
            var data = LoadOrNew();
            for (int i = 0; i < data.quests.Count; i++)
            {
                if (data.quests[i].id == id)
                    return data.quests[i];
            }

            var e = new QuestSaveEntry { id = id, state = (int)QuestState.Locked, progress = 0 };
            data.quests.Add(e);
            return e;
        }
    }
}
