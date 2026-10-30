using System;

namespace Game.Core
{
    // The level's countdown (§2.11). It never reads a clock: elapsed time is handed in with Tick.
    public sealed class LevelTimer
    {
        private readonly float limit;

        public LevelTimer(float limitSeconds)
        {
            limit = limitSeconds;
            Remaining = limitSeconds;
        }

        private LevelTimer(LevelTimer source)
        {
            limit = source.limit;
            Remaining = source.Remaining;
            IsRunning = source.IsRunning;
        }

        public bool HasLimit => limit > 0f;
        public bool IsRunning { get; private set; }
        public float Remaining { get; private set; }
        public int RemainingWholeSeconds => (int)MathF.Ceiling(Remaining);   // the value shown on screen

        // True if it started with this call.
        public bool StartIfNotRunning()
        {
            if (!HasLimit || IsRunning || Remaining <= 0f) return false;
            IsRunning = true;
            return true;
        }

        public void Stop() => IsRunning = false;

        // True if time ran out in THIS tick.
        public bool Tick(float deltaSeconds)
        {
            if (!IsRunning) return false;
            Remaining = MathF.Max(0f, Remaining - deltaSeconds);
            if (Remaining > 0f) return false;
            IsRunning = false;
            return true;
        }

        public LevelTimer Clone() => new LevelTimer(this);
    }
}
