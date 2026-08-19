using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyClicker.UI
{
    public class HoldPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float holdSeconds = 0.45f;
        public Action onTap;
        public Action onHold;
        public Action onRelease;

        float _heldFor;
        bool _down;
        bool _firedHold;

        public static HoldPress Bind(GameObject go, Action tap, Action hold, Action release = null, float seconds = 0.45f)
        {
            var press = go.GetComponent<HoldPress>() ?? go.AddComponent<HoldPress>();
            press.onTap = tap;
            press.onHold = hold;
            press.onRelease = release;
            press.holdSeconds = seconds;
            return press;
        }

        void Update()
        {
            if (!_down || _firedHold)
                return;
            _heldFor += Time.unscaledDeltaTime;
            if (_heldFor < holdSeconds)
                return;
            _firedHold = true;
            onHold?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            _heldFor = 0f;
            _firedHold = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool tap = _down && !_firedHold;
            bool held = _firedHold;
            Cancel();
            if (held)
                onRelease?.Invoke();
            else if (tap)
                onTap?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            bool held = _firedHold;
            Cancel();
            if (held)
                onRelease?.Invoke();
        }

        void Cancel()
        {
            _down = false;
            _heldFor = 0f;
            _firedHold = false;
        }
    }
}
