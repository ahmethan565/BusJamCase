using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.View
{
    // Visual of one bus: body + roof seats under SeatRoot. A seat is a dark chair and its passenger.
    // Trick: everyone is already in their seat, hidden; each boarding reveals the next one.
    // The empty chairs stay visible, so they show how many get on.
    public sealed class BusView : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform seatRoot;
        [SerializeField] private Transform seatPrefab;

        // Kept with the pooled bus; only grows when a bus with a bigger capacity shows up.
        private readonly List<Transform> seats = new List<Transform>();
        private readonly List<Renderer> passengers = new List<Renderer>();   // seats[i]'s passenger
        private AnimationSettings anim;
        private int capacity;
        // Below 1 when the seats would not fit on the roof: spacing and seat size shrink together.
        private float seatFit = 1f;

        public int Id { get; private set; }
        public Vector3 DoorPoint { get; private set; }

        // True from the end of the arrival until departure starts; passengers only walk to a parked bus.
        public bool IsParked { get; private set; }
        public event Action<BusView> Parked;

        // Boardings run in parallel: the bus leaves when nobody is left to get on, and
        // the seats fill by how many are in, because passengers can get on out of seat order.
        public int PendingBoardings { get; private set; }
        private int boarded;

        // Once, in the pool's create function.
        public void Configure(AnimationSettings animationSettings) => anim = animationSettings;

        public void Init(int busId, Material material, int busCapacity, Vector3 enterPosition, Vector3 doorPoint)
        {
            gameObject.SetActive(true);
            Id = busId;
            DoorPoint = doorPoint;
            IsParked = false;
            PendingBoardings = 0;
            boarded = 0;
            capacity = busCapacity;
            bodyRenderer.sharedMaterial = material;
            transform.SetPositionAndRotation(enterPosition, Quaternion.identity);

            while (seats.Count < capacity)
            {
                Transform seat = Instantiate(seatPrefab, seatRoot);
                seats.Add(seat);
                passengers.Add(seat.Find("Passenger").GetComponent<Renderer>());
            }

            seatFit = Mathf.Min(1f, bodyRenderer.localBounds.size.x / (capacity * anim.SeatSpacing));

            // Everyone is seated in the bus colour (passengers and buses share it) and hidden until they board.
            // Scaling the seat scales its chair, its passenger and how deep the passenger sinks into the roof.
            for (int i = 0; i < seats.Count; i++)
            {
                Transform seat = seats[i];
                seat.gameObject.SetActive(i < capacity);
                seat.localPosition = SeatPosition(i);
                seat.localScale = Vector3.one * seatFit;
                passengers[i].gameObject.SetActive(false);
                passengers[i].sharedMaterial = material;
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

        public void ExpectBoarding() => PendingBoardings++;

        // A passenger is in: returns how many are in now.
        public int CompleteBoarding()
        {
            PendingBoardings--;
            return ++boarded;
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

        // The first `count` seats are taken. The one that just got its passenger pops in with the tap squash feel.
        public void SetFilledCount(int count)
        {
            for (int i = 0; i < capacity; i++)
                passengers[i].gameObject.SetActive(i < count);

            if (count < 1 || count > capacity) return;
            Transform newcomer = passengers[count - 1].transform;
            newcomer.DOKill(complete: true);
            newcomer.DOPunchScale(anim.TapSquash, anim.TapSquashTime)
                .SetLink(newcomer.gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ResetForPool()
        {
            IsParked = false;
            PendingBoardings = 0;
            boarded = 0;
            transform.DOKill();
            foreach (Renderer passenger in passengers)
            {
                passenger.transform.DOKill();
                passenger.transform.localScale = Vector3.one;
                passenger.gameObject.SetActive(false);
            }
            foreach (Transform seat in seats)
                seat.gameObject.SetActive(false);
        }

        // Seat i along the roof, centred on the bus.
        private Vector3 SeatPosition(int i) => new Vector3((i - (capacity - 1) / 2f) * anim.SeatSpacing * seatFit, 0f, 0f);
    }
}
