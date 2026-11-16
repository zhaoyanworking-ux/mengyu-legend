using UnityEngine;

namespace ChibiARPG
{
    public class NpcCharacter : MonoBehaviour
    {
        public string npcId;
        public string displayName;
        public float talkRange = 2.6f;
        public QuestMarker marker;

        public static readonly ListBuffer All = new ListBuffer();

        public class ListBuffer
        {
            public readonly System.Collections.Generic.List<NpcCharacter> Items = new System.Collections.Generic.List<NpcCharacter>();
        }

        void OnEnable()
        {
            if (!All.Items.Contains(this))
                All.Items.Add(this);
        }

        void OnDisable()
        {
            All.Items.Remove(this);
        }

        public bool InRange(Transform player)
        {
            if (player == null)
                return false;
            Vector3 a = player.position;
            Vector3 b = transform.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b) <= talkRange;
        }

        public void RefreshMarker()
        {
            if (marker == null)
                return;
            char c = QuestSystem.Instance != null ? QuestSystem.Instance.MarkerForNpc(npcId) : ' ';
            marker.Set(c);
        }
    }
}
