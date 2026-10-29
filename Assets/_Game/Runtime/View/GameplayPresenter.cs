using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Animation;
using Game.Runtime.Data;
using Game.Runtime.Feedback;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Runtime.View
{
    // Turns Core events into animations: free animations or the sequential BusLane (§10.5).
    // Owns the passenger and bus pools; only this class gives those visuals back to their pools.
    public sealed class GameplayPresenter : MonoBehaviour, IGameListener
    {
        [SerializeField] private PassengerView passengerPrefab;
        [SerializeField] private BusView busPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int passengerPrewarm = 40;
        [SerializeField] private int busPrewarm = 4;

        private readonly BusLane lane = new BusLane();
        private readonly Dictionary<int, PassengerView> passengers = new Dictionary<int, PassengerView>();
        private readonly Dictionary<int, BusView> buses = new Dictionary<int, BusView>();
        private readonly HashSet<int> fromSlot = new HashSet<int>();

        private ColorPalette palette;
        private AnimationSettings anim;
        private LayoutSettings layoutSettings;
        private IFeedback feedback;
        private ObjectPool<PassengerView> passengerPool;
        private ObjectPool<BusView> busPool;
        private BoardLayout layout;

        private int lastExitedId = -1;
        private int walkingCount;
        private SessionResult pendingResult = SessionResult.Playing;
        private bool finishedRaised;

        public event Action<SessionResult> PresentationFinished;

        // Once, in the bootstrap.
        public void Init(ColorPalette colorPalette, AnimationSettings animationSettings,
                         LayoutSettings layoutSettingsAsset, IFeedback feedbackPlayer)
        {
            palette = colorPalette;
            anim = animationSettings;
            layoutSettings = layoutSettingsAsset;
            feedback = feedbackPlayer;

            passengerPool = new ObjectPool<PassengerView>(CreatePassenger,
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => { view.ResetForPool(); view.gameObject.SetActive(false); },
                collectionCheck: false);
            busPool = new ObjectPool<BusView>(CreateBus,
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => { view.ResetForPool(); view.gameObject.SetActive(false); },
                collectionCheck: false);
        }

        public void Prewarm()
        {
            var takenPassengers = new PassengerView[passengerPrewarm];
            for (int i = 0; i < takenPassengers.Length; i++) takenPassengers[i] = passengerPool.Get();
            foreach (PassengerView view in takenPassengers) passengerPool.Release(view);

            var takenBuses = new BusView[busPrewarm];
            for (int i = 0; i < takenBuses.Length; i++) takenBuses[i] = busPool.Get();
            foreach (BusView view in takenBuses) busPool.Release(view);
        }

        // Builds the passenger visuals and plays the intro. Visuals are bound to Ids, so this can run before the GameSession exists.
        public void Begin(LevelData level, BoardLayout boardLayout)
        {
            layout = boardLayout;
            foreach (PassengerSpawn spawn in level.Passengers)
            {
                PassengerView view = passengerPool.Get();
                view.Init(spawn, palette.GetMaterial(spawn.Color), layout.CellToWorld(spawn.Position));
                view.PlayIntro(spawn.Id * anim.IntroStagger);
                passengers[spawn.Id] = view;
            }
        }

        public void Clear()
        {
            lane.Clear();
            foreach (PassengerView view in passengers.Values) passengerPool.Release(view);
            passengers.Clear();
            foreach (BusView view in buses.Values) busPool.Release(view);
            buses.Clear();
            fromSlot.Clear();
            lastExitedId = -1;
            walkingCount = 0;   // killed walks never raise WalkFinished
            pendingResult = SessionResult.Playing;
            finishedRaised = false;
        }

        // Called by steps; removing from the dictionary prevents Clear() from releasing twice.
        public void ReleasePassenger(int passengerId)
        {
            if (passengers.Remove(passengerId, out PassengerView view)) passengerPool.Release(view);
        }

        public void ReleaseBus(int busId)
        {
            if (buses.Remove(busId, out BusView view)) busPool.Release(view);
        }

        // --- IGameListener

        public void OnBusArrived(Bus bus)
        {
            BusView view = busPool.Get();
            view.Init(bus.Id, palette.GetMaterial(bus.Color), bus.Capacity, layout.BusEnterPosition, layout.DoorPoint);
            buses[bus.Id] = view;
            lane.Enqueue(new BusArriveStep(view, layout.BusParkPosition, feedback));
        }

        public void OnPassengerBlocked(Passenger passenger)
        {
            PassengerView view = passengers[passenger.Id];
            view.PlayBlockedShake();
            feedback.PassengerBlocked(view.transform.position);
        }

        public void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path)
        {
            lastExitedId = passenger.Id;
            PassengerView view = passengers[passenger.Id];
            view.StopIdle();
            view.PlayTapSquash();
            feedback.PassengerTapped(view.transform.position);

            // path[0] is the cell the passenger stands on; walk the rest, then up to the road.
            var points = new Vector3[path.Count];
            for (int i = 1; i < path.Count; i++) points[i - 1] = layout.CellToWorld(path[i]);
            points[path.Count - 1] = layout.RoadPointAbove(path[path.Count - 1]);
            view.WalkAlong(points);
        }

        public void OnPassengerWaiting(Passenger passenger, int slot)
        {
            PassengerView view = passengers[passenger.Id];
            view.WalkAlong(new[] { layout.SlotPosition(slot) }, () => ArrivedAtSlot(view));
        }

        public void OnPassengerLeftWaitingArea(Passenger passenger, int slot) => fromSlot.Add(passenger.Id);

        public void OnPassengerBoarded(Passenger passenger, Bus bus, int seat)
        {
            PassengerView view = passengers[passenger.Id];
            bool comesFromSlot = fromSlot.Remove(passenger.Id);
            if (!comesFromSlot) view.WalkAlong(new[] { layout.DoorPoint });
            lane.Enqueue(new BoardStep(this, view, buses[bus.Id], seat, comesFromSlot, feedback));
        }

        public void OnBusDeparted(Bus bus)
        {
            lane.Enqueue(new BusDepartStep(this, buses[bus.Id], layout.BusExitPosition, feedback));
        }

        public void OnWon() => pendingResult = SessionResult.Won;

        // The shake is played by ArrivedAtSlot when the losing passenger reaches the last slot.
        public void OnLost() => pendingResult = SessionResult.Lost;

        private void ArrivedAtSlot(PassengerView view)
        {
            feedback.PassengerWaiting();
            // The passenger that took the last free slot lost the level (§2.8): it shakes in its slot.
            if (pendingResult == SessionResult.Lost && view.Id == lastExitedId) view.PlayBlockedShake();
        }

        // --- Per frame: the lane and the end-of-presentation check (an allowed Update, §16.3)

        private void Update()
        {
            lane.Tick();

            if (pendingResult != SessionResult.Playing && !finishedRaised
                && lane.IsIdle && walkingCount == 0)
            {
                finishedRaised = true;
                PresentationFinished?.Invoke(pendingResult);
            }
        }

        private PassengerView CreatePassenger()
        {
            PassengerView view = Instantiate(passengerPrefab, poolRoot);
            view.Configure(anim, layoutSettings.CellSize);
            view.WalkStarted += () => walkingCount++;
            view.WalkFinished += () => walkingCount--;
            return view;
        }

        private BusView CreateBus()
        {
            BusView view = Instantiate(busPrefab, poolRoot);
            view.Configure(anim);
            return view;
        }
    }
}
