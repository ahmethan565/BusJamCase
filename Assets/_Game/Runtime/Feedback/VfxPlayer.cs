using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Runtime.Feedback
{
    // One ParticleSystem pool per VfxId. Effects come back through PooledParticle when they stop.
    public sealed class VfxPlayer : MonoBehaviour
    {
        [SerializeField] private PooledParticle exhaustPuffPrefab;
        [SerializeField] private PooledParticle confettiPrefab;
        [SerializeField] private int exhaustPuffPrewarm = 4;
        [SerializeField] private int confettiPrewarm = 1;

        private ObjectPool<PooledParticle>[] pools;

        // Once, in the bootstrap.
        public void Init()
        {
            pools = new ObjectPool<PooledParticle>[2];
            pools[(int)VfxId.ExhaustPuff] = CreatePool(VfxId.ExhaustPuff, exhaustPuffPrefab);
            pools[(int)VfxId.Confetti] = CreatePool(VfxId.Confetti, confettiPrefab);
        }

        public void Prewarm()
        {
            Prewarm(pools[(int)VfxId.ExhaustPuff], exhaustPuffPrewarm);
            Prewarm(pools[(int)VfxId.Confetti], confettiPrewarm);
        }

        public PooledParticle Play(VfxId id, Vector3 at)
        {
            PooledParticle effect = pools[(int)id].Get();
            effect.PlayAt(at);
            return effect;
        }

        public void Release(PooledParticle effect) => pools[(int)effect.Id].Release(effect);

        private ObjectPool<PooledParticle> CreatePool(VfxId id, PooledParticle prefab) =>
            new ObjectPool<PooledParticle>(
                createFunc: () =>
                {
                    PooledParticle effect = Instantiate(prefab, transform);
                    effect.Bind(id, this);
                    return effect;
                },
                actionOnGet: effect => effect.gameObject.SetActive(true),
                actionOnRelease: effect => effect.gameObject.SetActive(false),
                collectionCheck: false);

        private static void Prewarm(ObjectPool<PooledParticle> pool, int count)
        {
            var taken = new PooledParticle[count];
            for (int i = 0; i < count; i++) taken[i] = pool.Get();
            foreach (PooledParticle effect in taken) pool.Release(effect);
        }
    }
}
