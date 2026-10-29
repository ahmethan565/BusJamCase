using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.View
{
    // Visual of one bus: body + fill indicators under IndicatorRoot (§10.7).
    public sealed class BusView : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform indicatorRoot;
        [SerializeField] private Renderer indicatorPrefab;
        [SerializeField] private Material emptyIndicatorMaterial;
        [SerializeField] private Material filledIndicatorMaterial;

        // Kept with the pooled bus; only grows when a bus with a bigger capacity shows up.
        private readonly List<Renderer> indicators = new List<Renderer>();
        private AnimationSettings anim;
        private int capacity;

        public int Id { get; private set; }
        public Vector3 DoorPoint { get; private set; }

        // True from the end of the arrival until the departure starts. Passengers only walk to the door of a parked bus.
        public bool IsParked { get; private set; }
        public event Action<BusView> Parked;

        // Once, in the pool's create function.
        public void Configure(AnimationSettings animationSettings) => anim = animationSettings;

        public void Init(int busId, Material material, int busCapacity, Vector3 enterPosition, Vector3 doorPoint)
        {
            gameObject.SetActive(true);
            Id = busId;
            DoorPoint = doorPoint;
            IsParked = false;
            capacity = busCapacity;
            bodyRenderer.sharedMaterial = material;
            transform.SetPositionAndRotation(enterPosition, Quaternion.identity);

            while (indicators.Count < capacity)
                indicators.Add(Instantiate(indicatorPrefab, indicatorRoot));

            for (int i = 0; i < indicators.Count; i++)
            {
                Renderer indicator = indicators[i];
                bool used = i < capacity;
                indicator.gameObject.SetActive(used);
                if (!used) continue;
                indicator.transform.localPosition = new Vector3((i - (capacity - 1) / 2f) * anim.IndicatorSpacing, 0f, 0f);
                indicator.sharedMaterial = emptyIndicatorMaterial;
            }
        }

        public void Arrive(Vector3 parkPosition, Action onComplete)
        {
            transform.DOKill();   // a queue move still running must not fight the arrival
            transform.DOMove(parkPosition, anim.BusArriveTime)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    IsParked = true;
                    Parked?.Invoke(this);
                    onComplete();
                });
        }

        // The next bus moving up to its place in the queue (not parked: nobody boards it yet).
        public void DriveTo(Vector3 position)
        {
            transform.DOMove(position, anim.BusArriveTime)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void Depart(Vector3 exitPosition, Action onComplete)
        {
            IsParked = false;
            transform.DOMove(exitPosition, anim.BusDepartTime)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => onComplete());
        }

        // The first `count` indicators are filled. The punch reuses the tap squash feel.
        public void SetFilledCount(int count)
        {
            for (int i = 0; i < capacity; i++)
                indicators[i].sharedMaterial = i < count ? filledIndicatorMaterial : emptyIndicatorMaterial;

            indicatorRoot.DOKill(complete: true);
            indicatorRoot.DOPunchScale(anim.TapSquash, anim.TapSquashTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ResetForPool()
        {
            IsParked = false;
            transform.DOKill();
            indicatorRoot.DOKill();
            indicatorRoot.localScale = Vector3.one;
            foreach (Renderer indicator in indicators)
                indicator.gameObject.SetActive(false);
        }
    }
}
