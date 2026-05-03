using System;
using UnityEngine;

namespace AbilitySystem
{
    // ============================================================
    // BASE EVENT DATA
    // ============================================================

    public struct AbilityEventData
    {
        public int SourceID;
        public string AbilityID;
        public Vector3 Position;
        public Vector3 Direction;
    }

    // ============================================================
    // EVENTS
    // ============================================================

    public static class AbilityEvents
    {
        // 🔹 CLIENT → SERVER (request)
        public static Action<AbilityEventData> OnAbilityRequested;

        // 🔹 SERVER VALIDATION
        public static Action<AbilityEventData> OnAbilityValidated;
        public static Action<AbilityEventData> OnAbilityFailed;

        // 🔹 EXECUTION
        public static Action<AbilityEventData> OnAbilityExecuted;

        // 🔹 EFFECTS
        public static Action<AbilityEventData> OnEffectApplied;
        public static Action<AbilityEventData> OnEffectRemoved;

        // ============================================================
        // SAFE INVOKE HELPERS
        // ============================================================

        public static void RaiseRequested(AbilityEventData data)
        {
            OnAbilityRequested?.Invoke(data);
        }

        public static void RaiseValidated(AbilityEventData data)
        {
            OnAbilityValidated?.Invoke(data);
        }

        public static void RaiseFailed(AbilityEventData data)
        {
            OnAbilityFailed?.Invoke(data);
        }

        public static void RaiseExecuted(AbilityEventData data)
        {
            OnAbilityExecuted?.Invoke(data);
        }

        public static void RaiseEffectApplied(AbilityEventData data)
        {
            OnEffectApplied?.Invoke(data);
        }

        public static void RaiseEffectRemoved(AbilityEventData data)
        {
            OnEffectRemoved?.Invoke(data);
        }
    }
}