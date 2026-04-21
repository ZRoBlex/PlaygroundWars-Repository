// ============================================================
//  AbilityEvents.cs
//  AbilitySystem/Events/AbilityEvents.cs
//
//  Todos los event structs del sistema de habilidades.
//  Comunicación 100% desacoplada via EventBus<T>.
// ============================================================

using UnityEngine;

namespace Abilities.Events
{
    public enum TargetType  { Self, Target, Area, Direction }
    public enum AbilitySlot { Primary = 0, Secondary = 1, Ultimate = 2 }

    // ── Habilidades ───────────────────────────────────────────

    public struct OnAbilityActivatedEvent
    {
        public int        OwnerID;
        public string     AbilityID;
        public TargetType TargetType;
        public Vector3    TargetPosition;
        public int        TargetPlayerID;   // -1 si no apunta a jugador
    }

    public struct OnAbilityFailedEvent
    {
        public int    OwnerID;
        public string AbilityID;
        public string Reason;   // "OnCooldown", "NoTarget", "Blocked"
    }

    // ── Cooldown ──────────────────────────────────────────────

    public struct OnCooldownStartEvent
    {
        public int    OwnerID;
        public string AbilityID;
        public float  Duration;
    }

    public struct OnCooldownEndEvent
    {
        public int    OwnerID;
        public string AbilityID;
    }

    public struct OnCooldownProgressEvent
    {
        public int    OwnerID;
        public string AbilityID;
        public float  Remaining;
        public float  Total;
    }

    // ── Status Effects ────────────────────────────────────────

    public struct OnEffectAppliedEvent
    {
        public int    TargetID;
        public string EffectID;
        public float  Duration;
        public float  Intensity;
        public int    SourcePlayerID;
    }

    public struct OnEffectRemovedEvent
    {
        public int    TargetID;
        public string EffectID;
        public bool   Expired;   // true = expiró, false = removido manualmente
    }

    public struct OnEffectStackedEvent
    {
        public int    TargetID;
        public string EffectID;
        public int    NewStackCount;
    }

    // ── Targeting ─────────────────────────────────────────────

    public struct OnTargetAcquiredEvent
    {
        public int     OwnerID;
        public string  AbilityID;
        public int     TargetID;
        public Vector3 TargetPosition;
    }

    public struct OnTargetLostEvent
    {
        public int    OwnerID;
        public string AbilityID;
    }
}
