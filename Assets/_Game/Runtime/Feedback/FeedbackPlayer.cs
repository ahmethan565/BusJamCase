using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.Feedback
{
    // Which sound and effect for which animation moment (§13.3).
    public sealed class FeedbackPlayer : IFeedback
    {
        private readonly AudioPlayer audioPlayer;
        private readonly VfxPlayer vfxPlayer;
        private readonly SoundLibrary sounds;

        public FeedbackPlayer(AudioPlayer audioPlayer, VfxPlayer vfxPlayer, SoundLibrary sounds)
        {
            this.audioPlayer = audioPlayer;
            this.vfxPlayer = vfxPlayer;
            this.sounds = sounds;
        }

        public void PassengerTapped(Vector3 at) => Play(SoundId.Pop);
        public void PassengerBlocked(Vector3 at) => Play(SoundId.Bonk);
        public void PassengerBoarded(int seat) => Play(SoundId.Board, 1f + seat * 0.1f);   // "do-re-mi" as the bus fills
        public void PassengerWaiting() => Play(SoundId.Seat);
        public void BusArrived() => Play(SoundId.Brake);
        public void Lost() => Play(SoundId.Lose);
        public void UiClick() => Play(SoundId.Click);

        public void BusDeparted(Vector3 at)
        {
            Play(SoundId.Horn);
            vfxPlayer.Play(VfxId.ExhaustPuff, at);
        }

        // The board centre is the world origin (§9.3); the confetti bursts above it.
        public void Won()
        {
            Play(SoundId.Win);
            vfxPlayer.Play(VfxId.Confetti, Vector3.zero);
        }

        private void Play(SoundId id, float pitch = 1f)
        {
            if (sounds.TryGet(id, out AudioClip clip, out float volume))
                audioPlayer.Play(id, clip, volume, pitch);
        }
    }
}
