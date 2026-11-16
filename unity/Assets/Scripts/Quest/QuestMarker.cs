using UnityEngine;

namespace ChibiARPG
{
    public class QuestMarker : MonoBehaviour
    {
        TextMesh _text;

        public void Set(char ch)
        {
            if (_text == null)
                _text = GetComponent<TextMesh>();
            if (_text == null)
                return;
            if (ch == '!' || ch == '?')
            {
                _text.text = ch == '!' ? "!" : "?";
                _text.color = ch == '!' ? new Color(1f, 0.85f, 0.2f) : new Color(0.45f, 0.95f, 0.55f);
                gameObject.SetActive(true);
            }
            else
                gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            transform.localPosition = new Vector3(0f, 1.85f, 0f) + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.05f;
        }

        public static QuestMarker Create(Transform parent)
        {
            var go = new GameObject("QuestMarker");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = BuiltinFont.Get();
            tm.fontSize = 86;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.text = "!";
            tm.color = new Color(1f, 0.85f, 0.2f);
            if (tm.font != null)
            {
                var r = go.GetComponent<MeshRenderer>();
                if (r != null)
                    r.sharedMaterial = tm.font.material;
            }

            var m = go.AddComponent<QuestMarker>();
            m._text = tm;
            return m;
        }
    }
}
