using Game.Core;
using UnityEngine;

namespace Game.Runtime.Flow
{
    // Hands the elapsed time to the rules (§2.11). Core never reads a clock; this is the only place that does.
    // An allowed Update (§16.3): time naturally moves frame by frame. Enabled only while a level is played.
    // When the app is in the background Update does not run, so the time stops; Unity caps the first
    // deltaTime after returning (Maximum Allowed Timestep), so there is no jump.
    public sealed class SessionClock : MonoBehaviour
    {
        private GameSession session;

        public void Bind(GameSession gameSession) => session = gameSession;
        public void Unbind() => session = null;

        private void Update() => session?.Tick(Time.deltaTime);
    }
}
