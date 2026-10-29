using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core.Levels;
using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.View
{
    // Visual of one passenger. Every transform layer has exactly one owner (§10.6):
    //   Passenger (root) -> position + rotation: walking (DOPath), hop into the bus (DOJump)
    //   └── Bounce       -> local position:      step bounce while walking, blocked shake
    //       └── Body     -> local scale:         intro pop, tap squash, shrink into the bus
    //           └── Mesh -> local scale:         idle "breathing"
    public sealed class PassengerView : MonoBehaviour
    {
        [SerializeField] private Transform bounce;
        [SerializeField] private Transform body;
        [SerializeField] private Transform mesh;
        [SerializeField] private Renderer meshRenderer;

        // Walks are queued as segments: tweens cannot be appended to a Sequence that is already running.
        private readonly Queue<(Vector3[] points, Action onArrived)> pending = new Queue<(Vector3[], Action)>();
        private AnimationSettings anim;
        private float cellSize;
        private bool idleStopped;

        public int Id { get; private set; }
        public bool IsWalking { get; private set; }
        public event Action WalkStarted, WalkFinished;

        // Once, in the pool's create function.
        public void Configure(AnimationSettings animationSettings, float cell)
        {
            anim = animationSettings;
            cellSize = cell;
        }

        public void Init(PassengerSpawn spawn, Material material, Vector3 position)
        {
            Id = spawn.Id;
            meshRenderer.sharedMaterial = material;
            transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        public void PlayIntro(float delay)
        {
            body.localScale = Vector3.zero;
            body.DOScale(Vector3.one, anim.IntroPopTime)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(PlayIdle);
        }

        public void PlayIdle()
        {
            if (idleStopped) return;
            mesh.DOScaleY(1f + anim.IdleBreathScale, anim.IdleBreathTime / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void StopIdle()
        {
            idleStopped = true;
            mesh.DOKill();
            mesh.localScale = Vector3.one;
        }

        public void PlayTapSquash()
        {
            body.DOKill(complete: true);
            body.DOPunchScale(anim.TapSquash, anim.TapSquashTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void PlayBlockedShake()
        {
            bounce.DOKill(complete: true);
            bounce.DOShakePosition(anim.BlockedShakeTime, anim.BlockedShakeStrength)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        // Adds a segment to the walk queue.
        public void WalkAlong(Vector3[] points, Action onArrived = null)
        {
            pending.Enqueue((points, onArrived));
            if (!IsWalking) StartNextSegment();
        }

        public void HopInto(Vector3 doorPoint, Action onComplete)
        {
            body.DOKill(complete: true);
            DOTween.Sequence()
                .Append(transform.DOJump(doorPoint, anim.BoardHopHeight, 1, anim.BoardHopTime))
                .Join(body.DOScale(Vector3.zero, anim.BoardHopTime))
                .SetTarget(transform)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => onComplete());
        }

        public void MoveFromSlotTo(Vector3 doorPoint, Action onComplete)
        {
            transform.DOMove(doorPoint, anim.BoardFromSlotTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => onComplete());
        }

        // Killed tweens do not call OnComplete, so every piece of state is reset by hand.
        // WalkStarted/WalkFinished subscriptions stay: the presenter subscribes once when the view is created.
        public void ResetForPool()
        {
            transform.DOKill();
            bounce.DOKill();
            body.DOKill();
            mesh.DOKill();

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            ResetLayer(bounce);
            ResetLayer(body);
            ResetLayer(mesh);

            pending.Clear();
            IsWalking = false;
            idleStopped = false;
        }

        private static void ResetLayer(Transform layer)
        {
            layer.localPosition = Vector3.zero;
            layer.localRotation = Quaternion.identity;
            layer.localScale = Vector3.one;
        }

        private void OnSegmentFinished(Action onArrived)
        {
            if (pending.Count == 0)
            {
                // Finish the walk first, THEN onArrived: the shake that onArrived may put on Bounce
                // must not be cancelled by StopStepBounce's DOKill.
                IsWalking = false;
                StopStepBounce();
                WalkFinished?.Invoke();
                onArrived?.Invoke();
                return;
            }
            onArrived?.Invoke();
            StartNextSegment();
        }

        private void StartNextSegment()
        {
            if (!IsWalking)
            {
                IsWalking = true;
                StartStepBounce();
                WalkStarted?.Invoke();
            }

            var (points, onArrived) = pending.Dequeue();
            float duration = PathLength(transform.position, points) / (anim.WalkSpeed * cellSize);

            transform.DOPath(points, duration, PathType.Linear)
                .SetLookAt(0.01f)
                .SetEase(Ease.Linear)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => OnSegmentFinished(onArrived));
        }

        private void StartStepBounce()
        {
            bounce.DOKill(complete: true);
            bounce.DOLocalMoveY(anim.StepHeight, anim.StepTime)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void StopStepBounce()
        {
            bounce.DOKill();
            bounce.localPosition = Vector3.zero;
        }

        // Measured from where the passenger IS, so single-point segments get the right duration too.
        private static float PathLength(Vector3 from, Vector3[] points)
        {
            float length = 0f;
            Vector3 previous = from;
            foreach (Vector3 p in points)
            {
                length += Vector3.Distance(previous, p);
                previous = p;
            }
            return length;
        }
    }
}
