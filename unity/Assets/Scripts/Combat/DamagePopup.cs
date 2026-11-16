using UnityEngine;

namespace ChibiARPG
{
    public class DamagePopup : MonoBehaviour
    {
        TextMesh _text;
        Color _color;
        float _life;
        float _maxLife = 0.7f;
        Vector3 _vel;

        public void Play(string content, Color color, float scale)
        {
            if (_text == null)
                _text = GetComponent<TextMesh>();
            _text.text = content;
            _text.color = color;
            _color = color;
            _life = _maxLife;
            _vel = new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, 0f);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _life -= dt;
            transform.position += _vel * dt;
            _vel.y -= 3.5f * dt;
            float t = 1f - Mathf.Clamp01(_life / _maxLife);
            if (_text != null)
            {
                Color c = _color;
                c.a = 1f - t;
                _text.color = c;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.7f, t) * (transform.localScale.x > 0 ? 1f : 1f);
            if (_life <= 0f)
                gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }
}
