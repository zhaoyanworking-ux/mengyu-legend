using UnityEngine;

namespace ChibiARPG
{
    public class PickupItem : MonoBehaviour
    {
        public string itemId;
        public string displayName;
        float _spin;

        public static PickupItem Spawn(Vector3 pos, string itemId, string displayName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = displayName;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(go.GetComponent<Collider>());
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.1f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = ToonMaterialUtil.Get(ChibiFactory.Hex("C45CFF"), 0.02f);
            var p = go.AddComponent<PickupItem>();
            p.itemId = itemId;
            p.displayName = displayName;
            var halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            halo.name = "Glow";
            halo.transform.SetParent(go.transform, false);
            halo.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            halo.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            Object.Destroy(halo.GetComponent<Collider>());
            var hr = halo.GetComponent<Renderer>();
            if (hr != null)
                hr.sharedMaterial = ToonMaterialUtil.Get(ChibiFactory.Hex("E0A0FF"), 0.004f);
            return p;
        }

        void Update()
        {
            _spin += Time.deltaTime * 120f;
            transform.rotation = Quaternion.Euler(15f, _spin, 15f);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * Time.deltaTime * 0.25f;
        }

        void OnTriggerEnter(Collider other)
        {
            var stats = other.GetComponentInParent<CharacterStats>();
            if (stats == null || !stats.isPlayer)
                return;
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.NotifyCollect(itemId, 1);
            if (GameHud.Instance != null)
                GameHud.Instance.Toast("获得 " + displayName);
            Destroy(gameObject);
        }
    }
}
