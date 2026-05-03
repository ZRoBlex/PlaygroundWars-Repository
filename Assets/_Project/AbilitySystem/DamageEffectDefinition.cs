using UnityEngine;

namespace AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability System/Effects/Damage")]
    public class DamageEffectDefinition : EffectDefinition
    {
        public float Damage = 25f;

        public override void Execute(EffectContext context)
        {
            if (context.Targets == null) return;

            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                var health = target.GetComponent<Player.Health.PlayerHealth>();
                if (health == null) continue;

                health.TakeDamage(
                    Damage,
                    context.SourceID,
                    context.Origin,
                    Vector3.up
                );
            }
        }
    }
}