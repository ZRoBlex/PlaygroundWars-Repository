// ============================================================
//  AbilityBase.cs
//  AbilitySystem/Abilities/AbilityBase.cs
//
//  RESPONSABILIDAD ÚNICA: Contrato base para todas las habilidades.
//  Crear una habilidad nueva: heredar y sobreescribir OnActivate().
// ============================================================

using Abilities.Config;
using Abilities.Events;
using Core.Debug;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace Abilities
{
    public abstract class AbilityBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] protected AbilityConfig _config;

        // ── Referencias ───────────────────────────────────────

        protected PlayerAuthority      _authority;
        protected AbilityTargetingSystem _targeting;
        protected AbilityEffectSystem    _effects;

        // ── Estado ────────────────────────────────────────────

        public  AbilityConfig Config  => _config;
        private AbilityCooldownSystem _cooldown;

        // ── Lifecycle ─────────────────────────────────────────

        protected virtual void Awake()
        {
            _authority = GetComponentInParent<PlayerAuthority>();
            _targeting = GetComponentInParent<AbilityTargetingSystem>();
            _effects   = GetComponentInParent<AbilityEffectSystem>();
        }

        // ── API interna (AbilityManager la llama) ─────────────

        internal void SetCooldownSystem(AbilityCooldownSystem cd) => _cooldown = cd;

        // ── Activación pública ────────────────────────────────

        /// <summary>
        /// Intenta activar la habilidad.
        /// Valida cooldown y estado antes de llamar a OnActivate().
        /// </summary>
        public bool TryActivate()
        {
            if (!CanActivate()) return false;

            // Resolver objetivo
            var target = _targeting != null
                ? _targeting.Resolve(_config)
                : new AbilityTarget { Valid = true, Type = TargetType.Self };

            if (!target.Valid)
            {
                EventBus<OnAbilityFailedEvent>.Raise(new OnAbilityFailedEvent
                {
                    OwnerID   = _authority.PlayerID,
                    AbilityID = _config.AbilityID,
                    Reason    = "NoTarget"
                });
                return false;
            }

            // Aplicar efectos base (daño, curación, status)
            _effects?.ApplyEffects(_config, target);

            // Lógica específica de la habilidad
            OnActivate(target);

            // Iniciar cooldown
            float cd = _config.Cooldown * _config.CooldownScale;
            _cooldown?.StartCooldown(_config.AbilityID, cd);
            OnCooldownStart(cd);

            EventBus<OnAbilityActivatedEvent>.Raise(new OnAbilityActivatedEvent
            {
                OwnerID        = _authority.PlayerID,
                AbilityID      = _config.AbilityID,
                TargetType     = target.Type,
                TargetPosition = target.Position,
                TargetPlayerID = target.TargetPlayerID
            });

            CoreLogger.LogSystem("AbilityBase",
                $"[P{_authority.PlayerID}] '{_config.AbilityID}' activada.");
            return true;
        }

        // ── Contrato ──────────────────────────────────────────

        /// <summary>Lógica específica de la habilidad. Sobreescribir en subclases.</summary>
        protected abstract void OnActivate(AbilityTarget target);

        public virtual bool CanActivate()
        {
            if (_config == null) return false;
            if (_cooldown != null && !_cooldown.IsReady(_config.AbilityID))
            {
                EventBus<OnAbilityFailedEvent>.Raise(new OnAbilityFailedEvent
                {
                    OwnerID   = _authority.PlayerID,
                    AbilityID = _config.AbilityID,
                    Reason    = "OnCooldown"
                });
                return false;
            }
            return true;
        }

        public virtual void OnCooldownStart(float duration) { }
        public virtual void OnCooldownEnd()                 { }
    }
}