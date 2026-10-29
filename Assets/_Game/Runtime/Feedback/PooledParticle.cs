using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.Feedback
{
    // Goes back to its VfxPlayer when the particle system stops (stopAction = Callback); no per-frame polling.
    // ExecuteAlways: the stop message also arrives in edit mode (EditMode tests); without an owner it is ignored.
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticle : MonoBehaviour
    {
        [SerializeField] private ParticleSystem system;

        private VfxPlayer owner;

        public VfxId Id { get; private set; }

        // Once, in the pool's create function.
        public void Bind(VfxId id, VfxPlayer player)
        {
            Id = id;
            owner = player;
        }

        public void PlayAt(Vector3 position)
        {
            transform.position = position;
            system.Play(true);
        }

        // No owner: a prefab previewed in the editor, not a pooled instance.
        private void OnParticleSystemStopped()
        {
            if (owner != null) owner.Release(this);
        }
    }
}
