using UnityEngine;

namespace AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability System/Effects/Heal")]
    public class HealEffectDefinition : EffectDefinition
    {
        public float HealAmount = 20f;

        public override void Execute(EffectContext context)
        {
            if (context.Targets == null) return;

            foreach (var target in context.Targets)
            {
                if (target == null) continue;

                var health = target.GetComponent<Player.Health.PlayerHealth>();
                if (health == null) continue;

                health.Heal(HealAmount);
            }
        }
    }
}