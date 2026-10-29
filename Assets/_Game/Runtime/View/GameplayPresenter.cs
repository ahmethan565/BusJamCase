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
    //
    // Stop rule (§10.5): nobody waits at the door. A passenger walks to the door only when its bus is parked;
    // otherwise it sits on a free seat at the stop and boards from there. If every seat is taken on screen,
    // the passenger is held back on its cell until a seat frees up or its bus parks.
    public sealed class GameplayPresenter : MonoBehaviour, IGameListener
    {
        // A passenger that left the board but has no seat on screen yet.
        private sealed class HeldPassenger
        {
            public PassengerView View;
            public Vector3[] ExitWalk;
            public int PreferredSlot;
            public BusView Bus;   // set once Core has boarded it
        }

        [SerializeField] private PassengerView passengerPrefab;
        [SerializeField] private BusView busPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int passengerPrewarm = 40;
        [SerializeField] private int busPrewarm = 4;

        private readonly BusLane lane = new BusLane();
        private readonly Dictionary<int, PassengerView> passengers = new Dictionary<int, PassengerView>();
        private readonly Dictionary<int, BusView> buses = new Dictionary<int, BusView>();
        private readonly StopSeats seats = new StopSeats();
        private readonly List<HeldPassenger> held = new List<HeldPassenger>();
        private readonly List<BoardStep> boardings = new List<BoardStep>();   // in parallel, not in the lane (§10.8)

        private ColorPalette palette;
        private AnimationSettings anim;
        private LayoutSettings layoutSettings;
        private IFeedback feedback;
        private ObjectPool<PassengerView> passengerPool;
        private ObjectPool<BusView> busPool;
        private BoardLayout layout;
        private IReadOnlyList<PassengerColor> busColors;
        private int busCapacity;

        private int lastExitedId = -1;
        private Vector3[] exitWalk;   // the walk of the passenger that just left the board; the next event in the same tap uses it
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
            busColors = level.BusColors;
            busCapacity = level.BusCapacity;
            seats.Reset(level.WaitingSlots);
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
            seats.Reset(0);
            held.Clear();
            boardings.Clear();
            lastExitedId = -1;
            exitWalk = null;
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

        // --- For BoardStep

        public bool IsHeldBack(int passengerId) => FindHeld(passengerId) >= 0;

        // The passenger stands up from its seat (if it has one); the freed seat goes to the first held passenger.
        public bool LeaveSeat(int passengerId)
        {
            if (!seats.TryRelease(passengerId, out _)) return false;
            SeatFirstHeldPassenger();
            return true;
        }

        // --- IGameListener

        public void OnBusArrived(Bus bus)
        {
            // The bus may already be waiting in the queue; the one behind it (if any) moves up into the queue.
            if (!buses.TryGetValue(bus.Id, out BusView view)) view = SpawnBus(bus.Id, layout.BusEnterPosition);
            BusView follower = bus.Id + 1 < busColors.Count ? SpawnBus(bus.Id + 1, layout.BusQueueEnterPosition) : null;
            lane.Enqueue(new BusArriveStep(view, layout.BusParkPosition, follower, layout.BusQueuePosition, feedback));
        }

        public void OnPassengerBlocked(Passenger passenger)
        {
            PassengerView view = passengers[passenger.Id];
            view.PlayBlockedShake();
            feedback.PassengerBlocked(view.transform.position);
        }

        // The walk only starts with the next event of the same tap: it decides where the passenger goes.
        public void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path)
        {
            lastExitedId = passenger.Id;
            PassengerView view = passengers[passenger.Id];
            view.StopIdle();
            view.PlayTapSquash();
            feedback.PassengerTapped(view.transform.position);

            // path[0] is the cell the passenger stands on; walk the rest, then up to the road.
            exitWalk = new Vector3[path.Count];
            for (int i = 1; i < path.Count; i++) exitWalk[i - 1] = layout.CellToWorld(path[i]);
            exitWalk[path.Count - 1] = layout.RoadPointAbove(path[path.Count - 1]);
        }

        public void OnPassengerWaiting(Passenger passenger, int slot)
        {
            GoToStop(passengers[passenger.Id], TakeExitWalk(passenger.Id), slot, null);
        }

        // Nothing to animate: the passenger keeps its seat on screen until its BoardStep walks it to the bus.
        public void OnPassengerLeftWaitingArea(Passenger passenger, int slot) { }

        public void OnPassengerBoarded(Passenger passenger, Bus bus, int seat)
        {
            PassengerView view = passengers[passenger.Id];
            BusView busView = buses[bus.Id];
            Vector3[] walk = TakeExitWalk(passenger.Id);

            if (walk == null) AssignBusToHeld(passenger.Id, busView);           // already at the stop (seated or held)
            else if (busView.IsParked) view.WalkAlong(Append(walk, layout.DoorPoint));
            else GoToStop(view, walk, -1, busView);                              // the bus is not here yet: wait at the stop

            busView.ExpectBoarding();
            boardings.Add(new BoardStep(this, view, busView, feedback));
        }

        public void OnBusDeparted(Bus bus)
        {
            lane.Enqueue(new BusDepartStep(this, buses[bus.Id], layout.BusExitPosition, feedback));
        }

        public void OnWon() => pendingResult = SessionResult.Won;

        // The shake is played by ArrivedAtSlot when the losing passenger reaches the last slot.
        public void OnLost() => pendingResult = SessionResult.Lost;

        // --- Per frame: the lane and the end-of-presentation check (an allowed Update, §16.3)

        private void Update()
        {
            lane.Tick();
            for (int i = boardings.Count - 1; i >= 0; i--)   // each starts on its own as soon as it is ready
            {
                boardings[i].Tick();
                if (boardings[i].IsDone) boardings.RemoveAt(i);
            }

            if (pendingResult != SessionResult.Playing && !finishedRaised
                && lane.IsIdle && boardings.Count == 0 && walkingCount == 0 && held.Count == 0)
            {
                finishedRaised = true;
                PresentationFinished?.Invoke(pendingResult);
            }
        }

        // --- The stop

        private Vector3[] TakeExitWalk(int passengerId)
        {
            if (passengerId != lastExitedId || exitWalk == null) return null;
            Vector3[] walk = exitWalk;
            exitWalk = null;
            return walk;
        }

        private void GoToStop(PassengerView view, Vector3[] walk, int preferredSlot, BusView bus)
        {
            int seat = seats.Take(view.Id, preferredSlot);
            if (seat >= 0)
            {
                WalkToSeat(view, walk, seat);
                return;
            }
            held.Add(new HeldPassenger { View = view, ExitWalk = walk, PreferredSlot = preferredSlot, Bus = bus });
        }

        private void WalkToSeat(PassengerView view, Vector3[] walk, int seat)
        {
            Vector3[] points = walk == null ? new[] { layout.SlotPosition(seat) } : Append(walk, layout.SlotPosition(seat));
            view.WalkAlong(points, () => ArrivedAtSlot(view));
        }

        private void SeatFirstHeldPassenger()
        {
            if (held.Count == 0) return;
            HeldPassenger next = held[0];
            int seat = seats.Take(next.View.Id, next.PreferredSlot);
            if (seat < 0) return;
            held.RemoveAt(0);
            WalkToSeat(next.View, next.ExitWalk, seat);
        }

        private void AssignBusToHeld(int passengerId, BusView bus)
        {
            int index = FindHeld(passengerId);
            if (index < 0) return;
            held[index].Bus = bus;
            if (bus.IsParked) WalkHeldToDoor(index);
        }

        // A held passenger's bus has parked: it walks straight to the door, no seat needed any more.
        private void OnBusParked(BusView bus)
        {
            for (int i = held.Count - 1; i >= 0; i--)
                if (held[i].Bus == bus) WalkHeldToDoor(i);
        }

        private void WalkHeldToDoor(int index)
        {
            HeldPassenger passenger = held[index];
            held.RemoveAt(index);
            passenger.View.WalkAlong(passenger.ExitWalk == null
                ? new[] { layout.DoorPoint }
                : Append(passenger.ExitWalk, layout.DoorPoint));
        }

        private int FindHeld(int passengerId)
        {
            for (int i = 0; i < held.Count; i++)
                if (held[i].View.Id == passengerId) return i;
            return -1;
        }

        private static Vector3[] Append(Vector3[] points, Vector3 last)
        {
            var result = new Vector3[points.Length + 1];
            points.CopyTo(result, 0);
            result[points.Length] = last;
            return result;
        }

        private void ArrivedAtSlot(PassengerView view)
        {
            feedback.PassengerWaiting();
            // The passenger that took the last free slot lost the level (§2.8): it shakes in its slot.
            if (pendingResult == SessionResult.Lost && view.Id == lastExitedId) view.PlayBlockedShake();
        }

        // --- Pools

        private BusView SpawnBus(int busId, Vector3 position)
        {
            BusView view = busPool.Get();
            view.Init(busId, palette.GetMaterial(busColors[busId]), busCapacity, position, layout.DoorPoint);
            buses[busId] = view;
            return view;
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
            view.Parked += OnBusParked;
            return view;
        }
    }
}
