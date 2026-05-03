using System;
using UnityEngine;

namespace AbilitySystem
{
    // ============================================================
    //  ESTADO DE HABILIDAD
    // ============================================================

    public enum AbilityState
    {
        Ready,
        Cooldown,
        Casting
    }

    // ============================================================
    //  ABILITY INSTANCE (RUNTIME)
    // ============================================================

    [Serializable]
    public class AbilityInstance
    {
        // ── DATA ──────────────────────────────────────────────

        public AbilityDefinition Definition { get; private set; }

        // 🔥 Multiplayer-safe ID
        public int OwnerID { get; private set; }

        // 🔥 Runtime reference (NECESARIA para gameplay)
        public GameObject Owner { get; private set; }

        // ── ESTADO ────────────────────────────────────────────

        public AbilityState State { get; private set; } = AbilityState.Ready;

        public float CooldownRemaining { get; private set; }

        // ── PROPIEDADES USADAS POR OTROS SISTEMAS ─────────────

        public bool IsReady => State == AbilityState.Ready;

        public float CurrentCooldown => CooldownRemaining;

        // ── CONSTRUCTOR ───────────────────────────────────────

        public AbilityInstance(AbilityDefinition definition, int ownerID, GameObject owner)
        {
            Definition = definition;
            OwnerID = ownerID;
            Owner = owner;

            State = AbilityState.Ready;
            CooldownRemaining = 0f;
        }

        // ── TICK (CONTROLADO EXTERNAMENTE) ────────────────────

        public void Tick(float deltaTime)
        {
            if (State != AbilityState.Cooldown) return;

            CooldownRemaining -= deltaTime;

            if (CooldownRemaining <= 0f)
            {
                CooldownRemaining = 0f;
                State = AbilityState.Ready;
            }
        }

        // ── VALIDACIÓN ────────────────────────────────────────

        public bool CanActivate()
        {
            if (Definition == null) return false;
            if (State != AbilityState.Ready) return false;

            return true;
        }

        // ── ACTIVACIÓN ────────────────────────────────────────

        public bool TryActivate()
        {
            if (!CanActivate())
                return false;

            StartCooldown();
            return true;
        }

        // ── COOLDOWN ──────────────────────────────────────────

        // 🔥 AHORA ES PUBLIC → ExecutionSystem lo necesita
        public void StartCooldown()
        {
            if (Definition == null) return;

            State = AbilityState.Cooldown;
            CooldownRemaining = Definition.Cooldown;
        }

        // ── DEBUG ─────────────────────────────────────────────

        public float GetCooldownPercent()
        {
            if (Definition == null || Definition.Cooldown <= 0f)
                return 0f;

            return CooldownRemaining / Definition.Cooldown;
        }
    }
}