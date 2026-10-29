using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Levels;
using Game.Core.Path;

namespace Game.Core
{
    // The conductor of the rules (§2). Resolves a tap instantly and reports what happened through IGameListener.
    public sealed class GameSession
    {
        private readonly Board board;
        private readonly BusQueue buses;
        private readonly WaitingArea waitingArea;
        private readonly IPathFinder pathFinder;
        private readonly IGameListener listener;
        private bool started;

        public GameSession(LevelData level, IPathFinder pathFinder, IGameListener listener)
        {
            board = Board.FromLevel(level);
            buses = CreateBuses(level);
            waitingArea = new WaitingArea(level.WaitingSlots);
            this.pathFinder = pathFinder;
            this.listener = listener;
        }

        private GameSession(GameSession source, IGameListener listener)
        {
            board = source.board.Clone();
            buses = source.buses.Clone();
            waitingArea = source.waitingArea.Clone();
            pathFinder = source.pathFinder;
            this.listener = listener;
            started = source.started;
            Result = source.Result;
        }

        public SessionResult Result { get; private set; }
        public bool IsOver => Result != SessionResult.Playing;
        public Board Board => board;              // read-only use in tests
        public BusQueue Buses => buses;
        public WaitingArea WaitingArea => waitingArea;

        public void Start()
        {
            if (started) throw new InvalidOperationException("Start() was already called.");
            started = true;
            if (buses.Current != null)
                listener.OnBusArrived(buses.Current);
        }

        public void Tap(GridPos cell)
        {
            if (!started) throw new InvalidOperationException("Call Start() before Tap().");
            if (IsOver) return;
            if (!board.TryGetPassenger(cell, out Passenger passenger)) return;

            if (!pathFinder.TryFindPathToExit(board, cell, out List<GridPos> path))
            {
                listener.OnPassengerBlocked(passenger);
                return;
            }

            board.RemovePassenger(cell);
            listener.OnPassengerExited(passenger, path);

            if (buses.Current != null && buses.Current.CanBoard(passenger))
                BoardPassenger(passenger);
            else
            {
                // The area is never left full while playing (§2.8), so a free slot always exists here.
                waitingArea.TryAdd(passenger, out int slot);
                listener.OnPassengerWaiting(passenger, slot);
                if (waitingArea.IsFull)
                {
                    Finish(SessionResult.Lost);
                    return;
                }
            }

            if (board.IsEmpty && waitingArea.IsEmpty)
                Finish(SessionResult.Won);
        }

        // Only for LevelSolver.
        internal GameSession Clone(IGameListener listener) => new GameSession(this, listener);

        // Only for LevelSolver (visited set): remaining passengers + slots + bus index + active bus fill.
        internal string StateKey
        {
            get
            {
                var key = new StringBuilder();
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                        key.Append(board.TryGetPassenger(new GridPos(x, y), out Passenger p) ? ColorChar(p.Color) : '.');
                key.Append('|');
                for (int slot = 0; slot < waitingArea.SlotCount; slot++)
                {
                    Passenger waiting = waitingArea.GetAt(slot);
                    key.Append(waiting != null ? ColorChar(waiting.Color) : '.');
                }
                key.Append('|').Append(buses.RemainingCount).Append(':').Append(buses.Current?.Count ?? 0);
                return key.ToString();
            }
        }

        private void BoardPassenger(Passenger passenger)
        {
            Bus bus = buses.Current;
            int seat = bus.Add(passenger);
            listener.OnPassengerBoarded(passenger, bus, seat);

            if (bus.IsFull)
                ReplaceFullBus(bus);
        }

        private void ReplaceFullBus(Bus fullBus)
        {
            listener.OnBusDeparted(fullBus);
            buses.Advance();
            if (buses.Current == null) return;

            listener.OnBusArrived(buses.Current);
            BoardWaitingPassengers();
        }

        private void BoardWaitingPassengers()
        {
            // If BoardPassenger() fills the bus, ReplaceFullBus -> BoardWaitingPassengers runs again;
            // this loop re-reads the current bus on every turn.
            while (buses.Current != null
                   && waitingArea.TryTake(buses.Current.Color, out Passenger waiting, out int slot))
            {
                listener.OnPassengerLeftWaitingArea(waiting, slot);
                BoardPassenger(waiting);
            }
        }

        private void Finish(SessionResult result)
        {
            Result = result;
            if (result == SessionResult.Won) listener.OnWon();
            else listener.OnLost();
        }

        private static BusQueue CreateBuses(LevelData level)
        {
            var list = new List<Bus>(level.BusColors.Count);
            for (int i = 0; i < level.BusColors.Count; i++)
                list.Add(new Bus(i, level.BusColors[i], level.BusCapacity));
            return new BusQueue(list);
        }

        private static char ColorChar(PassengerColor color) => (char)('a' + (int)color);
    }
}
