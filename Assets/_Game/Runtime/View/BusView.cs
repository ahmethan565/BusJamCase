using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.View
{
    // Visual of one bus: body + the passengers seated on its roof under SeatRoot (§10.7).
    // Trick: everyone is already in their seat, hidden; each boarding reveals the next one.
    // A dark chair under each seat (ChairRoot) stays visible, so the empty chairs show how many get on.
    public sealed class BusView : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform seatRoot;
        [SerializeField] private Renderer seatedPassengerPrefab;
        [SerializeField] private Transform chairRoot;
        [SerializeField] private GameObject seatChairPrefab;

        // Kept with the pooled bus; only grows when a bus with a bigger capacity shows up.
        private readonly List<Renderer> seats = new List<Renderer>();
        private readonly List<GameObject> chairs = new List<GameObject>();
        private AnimationSettings anim;
        private int capacity;

        public int Id { get; private set; }
        public Vector3 DoorPoint { get; private set; }

        // True from the end of the arrival until the departure starts. Passengers only walk to the door of a parked bus.
        public bool IsParked { get; private set; }
        public event Action<BusView> Parked;

        // Boardings run in parallel (§10.8): the bus leaves when nobody is left to get on, and
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
                seats.Add(Instantiate(seatedPassengerPrefab, seatRoot));
            while (chairs.Count < capacity)
                chairs.Add(Instantiate(seatChairPrefab, chairRoot));

            // Everyone is seated in the bus colour (passengers and buses share it) and hidden until they board.
            for (int i = 0; i < seats.Count; i++)
            {
                Renderer seat = seats[i];
                seat.gameObject.SetActive(false);
                seat.sharedMaterial = material;
                seat.transform.localPosition = SeatPosition(i);

                GameObject chair = chairs[i];
                chair.SetActive(i < capacity);
                chair.transform.localPosition = SeatPosition(i);
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
                seats[i].gameObject.SetActive(i < count);

            if (count < 1 || count > capacity) return;
            Transform newcomer = seats[count - 1].transform;
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
            foreach (Renderer seat in seats)
            {
                seat.transform.DOKill();
                seat.transform.localScale = Vector3.one;
                seat.gameObject.SetActive(false);
            }
            foreach (GameObject chair in chairs)
                chair.SetActive(false);
        }

        // Seat i along the roof, centred on the bus (passengers and chairs share it).
        private Vector3 SeatPosition(int i) => new Vector3((i - (capacity - 1) / 2f) * anim.SeatSpacing, 0f, 0f);
    }
}
