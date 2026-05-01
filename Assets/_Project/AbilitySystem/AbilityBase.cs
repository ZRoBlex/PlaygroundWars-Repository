// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityBase.cs                                 ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Core/            ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • AbilityBase        (MonoBehaviour abstracto)        ║
// ║    • AbilityEffectSystem (MonoBehaviour)                 ║
// ║                                                          ║
// ║  ⚠️ SEPARAR:                                            ║
// ║    AbilityEffectSystem → AbilityEffectSystem.cs          ║
// ║    Motivo: responsabilidad distinta y se añade al prefab ║
// ║    del jugador de forma independiente a AbilityBase.     ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Health;
using UnityEngine;

namespace AbilitySystem
{
    // ════════════════════════════════════════════════════════
    //  ABILITY BASE
    //  Clase base abstracta para todas las habilidades.
    //  Añadir como componente en un hijo del prefab del jugador
    //  junto con su AbilityConfig asignado en el Inspector.
    // ════════════════════════════════════════════════════════

    [RequireComponent(typeof(PlayerAuthority))]
    public abstract class AbilityBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] protected AbilityConfig _config;

        // ── Referencias (inyectadas por AbilityManager) ───────

        protected PlayerAuthority       _authority;
        protected AbilityTargetingSystem _targeting;
        protected AbilityEffectSystem    _effects;
        private   AbilityCooldownSystem  _cooldown;

        // ── Estado ────────────────────────────────────────────

        public AbilityConfig Config   => _config;
        public bool          IsActive { get; protected set; }

        public bool IsOnCooldown =>
            _cooldown != null && !_cooldown.IsReady(_config?.AbilityID ?? "");

        // ── Lifecycle ─────────────────────────────────────────

        protected virtual void Awake()
        {
            _authority = GetComponentInParent<PlayerAuthority>();
            _targeting = GetComponentInParent<AbilityTargetingSystem>();
            _effects   = GetComponentInParent<AbilityEffectSystem>();
        }

        // ── Inyección desde AbilityManager ───────────────────

        internal void InjectCooldown(AbilityCooldownSystem cd) => _cooldown = cd;

        // ── Activación ────────────────────────────────────────

        /// <summary>
        /// Punto de entrada para activar la habilidad.
        /// Valida cooldown + CanActivate + targeting antes de ejecutar.
        /// </summary>
        public bool TryActivate()
        {
            if (_config == null)
            {
                CoreLogger.LogWarning("[AbilityBase] AbilityConfig no asignado.");
                return false;
            }

            // 1. Cooldown
            if (IsOnCooldown)
            {
                Fail("OnCooldown");
                return false;
            }

            // 2. Condiciones custom de la subclase
            if (!CanActivate())
            {
                Fail("ConditionFailed");
                return false;
            }

            // 3. Resolver target
            TargetingResult target = new() { Valid = true };
            if (_targeting != null)
            {
                target = _targeting.Resolve(_config);
                if (!target.Valid)
                {
                    Fail("NoTarget");
                    return false;
                }
            }

            // 4. Aplicar efectos (solo autoridad)
            if (_authority.HasAuthority)
                _effects?.ApplyEffects(_config, target);

            // 5. Lógica específica de la habilidad
            OnActivate(target);

            // 6. Cooldown
            if (_config.Cooldown > 0f)
            {
                _cooldown?.StartCooldown(_config.AbilityID, _config.Cooldown);
                OnCooldownStart(_config.Cooldown);
            }

            IsActive = true;

            EventBus<OnAbilityActivatedEvt>.Raise(new OnAbilityActivatedEvt
            {
                OwnerID   = _authority.PlayerID,
                AbilityID = _config.AbilityID,
                TargetID  = target.TargetPlayerID,
                Position  = target.Position
            });

            return true;
        }

        // ── Contrato ──────────────────────────────────────────

        /// <summary>Lógica propia de la habilidad. Override en subclases.</summary>
        protected abstract void OnActivate(TargetingResult target);

        /// <summary>Condiciones extra para activar. Override para añadir restricciones.</summary>
        public virtual bool CanActivate() => true;

        /// <summary>Llamado cuando el cooldown empieza. Útil para VFX/animaciones.</summary>
        public virtual void OnCooldownStart(float duration) { }

        /// <summary>Llamado cuando el cooldown termina.</summary>
        public virtual void OnCooldownEnd() { }

        // ── Helpers ───────────────────────────────────────────

        private void Fail(string reason)
        {
            EventBus<OnAbilityFailedEvt>.Raise(new OnAbilityFailedEvt
            {
                OwnerID   = _authority?.PlayerID ?? -1,
                AbilityID = _config?.AbilityID ?? "",
                Reason    = reason
            });
        }
    }
}