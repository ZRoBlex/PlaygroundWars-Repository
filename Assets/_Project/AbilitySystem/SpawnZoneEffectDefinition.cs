using UnityEngine;

namespace AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability System/Effects/Spawn Zone")]
    public class SpawnZoneEffectDefinition : EffectDefinition
    {
        public GameObject ZonePrefab;
        public float Duration = 5f;

        public override void Execute(EffectContext context)
        {
            if (ZonePrefab == null) return;

            var zone = GameObject.Instantiate(
                ZonePrefab,
                context.Origin,
                Quaternion.identity
            );

            GameObject.Destroy(zone, Duration);
        }
    }
}