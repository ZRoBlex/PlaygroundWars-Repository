using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    public static class AbilityExecutionSystem
    {
        // ============================================================
        // ENTRY POINT (LO QUE LLAMA EL MANAGER)
        // ============================================================

        public static void ExecuteAbility(AbilityInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                Debug.LogWarning("[AbilityExecution] Invalid instance");
                return;
            }

            AbilityEventData eventData = new AbilityEventData
            {
                SourceID = instance.OwnerID,
                AbilityID = instance.Definition.AbilityID
            };

            // ─────────────────────────────────────────────
            // VALIDATION
            // ─────────────────────────────────────────────

            if (!instance.IsReady)
            {
                AbilityEvents.RaiseFailed(eventData);
                return;
            }

            AbilityEvents.RaiseValidated(eventData);

            // ─────────────────────────────────────────────
            // TARGETING
            // ─────────────────────────────────────────────

            AbilityTargetData targetData = AbilityTargetingSystem.ResolveTargets(instance);

            eventData.Position = targetData.Position;
            eventData.Direction = targetData.Direction;

            // ─────────────────────────────────────────────
            // EFFECTS
            // ─────────────────────────────────────────────

            EffectContext context = new EffectContext
            {
                SourceID = instance.OwnerID,
                Targets = targetData.Targets,
                Origin = targetData.Position,
                Direction = targetData.Direction
            };

            foreach (var effect in instance.Definition.Effects)
            {
                if (effect == null) continue;

                effect.Execute(context);

                // 🔥 EVENTO POR EFECTO
                AbilityEvents.RaiseEffectApplied(eventData);
            }

            // ─────────────────────────────────────────────
            // EXECUTED
            // ─────────────────────────────────────────────

            AbilityEvents.RaiseExecuted(eventData);

            // ─────────────────────────────────────────────
            // COOLDOWN
            // ─────────────────────────────────────────────

            instance.TryActivate();
        }
    }
}