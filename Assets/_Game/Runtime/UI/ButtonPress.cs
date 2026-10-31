using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // A pressed button shrinks a little while the finger is down.
    // The colour change on press is the Button's own ColorTint.
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private Button button;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button.IsInteractable()) Press();
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        public void Press() => ScaleTo(animationSettings.ButtonPressScale);
        public void Release() => ScaleTo(1f);

        private void ScaleTo(float scale)
        {
            transform.DOKill();
            transform.DOScale(scale, animationSettings.ButtonPressTime)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}
