using Game.Core;
using UnityEngine;

namespace Game.Runtime.Flow
{
    // The only clock reader: hands the elapsed time to the rules. An allowed Update, enabled only while a level
    // is played. In the background Update stops, so time stops; Unity caps the first deltaTime after returning
    // (Maximum Allowed Timestep), so there is no jump.
    public sealed class SessionClock : MonoBehaviour
    {
        private GameSession session;

        public void Bind(GameSession gameSession) => session = gameSession;
        public void Unbind() => session = null;

        private void Update() => session?.Tick(Time.deltaTime);
    }
}
