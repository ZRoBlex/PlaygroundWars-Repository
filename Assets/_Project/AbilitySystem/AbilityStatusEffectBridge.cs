using UnityEngine;

namespace AbilitySystem
{
    // ─────────────────────────────────────────────
    // EFECTO: APLICAR STATUS EFFECT
    // ─────────────────────────────────────────────

    [System.Serializable]
    public class ApplyStatusEffectEffect : IAbilityEffect
    {
        // 🔹 Tipo de efecto (configurable)
        public StatusEffectType EffectType;

        public float Duration = 3f;
        public float Intensity = 1f;

        public void Apply(AbilityInstance instance, AbilityTargetData data)
        {
            if (data.Targets == null || data.Targets.Count == 0)
                return;

            foreach (var target in data.Targets)
            {
                if (target == null) continue;

                var manager = target.GetComponent<StatusEffectManager>();

                if (manager == null)
                {
                    Debug.LogWarning($"[StatusEffectBridge] {target.name} no tiene StatusEffectManager");
                    continue;
                }

                var effect = CreateEffect();

                if (effect == null) continue;

                manager.ApplyEffect(effect);
            }
        }

        // ─────────────────────────────────────────────
        // FACTORY DE EFECTOS
        // ─────────────────────────────────────────────

        private StatusEffectBase CreateEffect()
        {
            switch (EffectType)
            {
                case StatusEffectType.Slow:
                    return new SlowStatusEffect
                    {
                        EffectID = "Slow",
                        Duration = Duration,
                        SlowPercent = Intensity
                    };

                case StatusEffectType.Freeze:
                    return new FreezeStatusEffect
                    {
                        EffectID = "Freeze",
                        Duration = Duration
                    };

                case StatusEffectType.DamageOverTime:
                    return new DamageOverTimeEffect
                    {
                        EffectID = "DoT",
                        Duration = Duration,
                        DamagePerSecond = Intensity
                    };

                default:
                    Debug.LogWarning("[StatusEffectBridge] Tipo no soportado");
                    return null;
            }
        }
    }

    // ─────────────────────────────────────────────
    // ENUM DE TIPOS (simple por ahora)
    // ─────────────────────────────────────────────

    public enum StatusEffectType
    {
        Slow,
        Freeze,
        DamageOverTime
    }
}