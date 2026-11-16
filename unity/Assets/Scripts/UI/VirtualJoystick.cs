using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChibiARPG
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        public RectTransform background;
        public RectTransform handle;
        public float handleRange = 70f;
        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }

        Canvas _canvas;
        Camera _uiCam;

        void Awake()
        {
            Instance = this;
            _canvas = GetComponentInParent<Canvas>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null)
                return;
            _uiCam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCam = _canvas.worldCamera;

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, _uiCam, out pos);
            pos = Vector2.ClampMagnitude(pos, handleRange);
            if (handle != null)
                handle.anchoredPosition = pos;
            Value = pos / handleRange;
            IsHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            IsHeld = false;
            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
        }
    }
}
