// ============================================================
//  StatusEffectBase.cs
//  AbilitySystem/StatusEffects/StatusEffectBase.cs
//
//  RESPONSABILIDAD ÚNICA: Contrato base de todos los efectos de estado.
//  Crear efectos nuevos heredando de esta clase.
// ============================================================

using Abilities.Events;
using Core.Events;
using UnityEngine;

namespace Abilities.StatusEffects
{
    /// <summary>
    /// Clase base abstracta para efectos de estado (Slow, Freeze, Blind...).
    /// Cada efecto es un componente añadido/removido dinámicamente por StatusEffectManager.
    /// </summary>
    public abstract class StatusEffectBase : MonoBehaviour
    {
        // ── Datos ─────────────────────────────────────────────

        public string EffectID       { get; protected set; } = "base_effect";
        public float  Duration       { get; protected set; }
        public float  Intensity      { get; protected set; }
        public int    SourcePlayerID { get; protected set; } = -1;
        public int    StackCount     { get; protected set; } = 1;
        public bool   CanStack       { get; protected set; } = false;
        public bool   IsActive       { get; protected set; }

        protected float _elapsed;

        // ── Inicialización ────────────────────────────────────

        /// <summary>Llamado por StatusEffectManager al añadir el efecto.</summary>
        public virtual void Initialize(float duration, float intensity, int sourceID)
        {
            Duration       = duration;
            Intensity      = intensity;
            SourcePlayerID = sourceID;
            _elapsed       = 0f;
            IsActive       = true;
        }

        // ── Métodos del contrato ──────────────────────────────

        /// <summary>Aplicar el efecto al objetivo.</summary>
        public abstract void Apply();

        /// <summary>Actualizar efecto cada frame (llamado por StatusEffectManager).</summary>
        public abstract void UpdateEffect(float deltaTime);

        /// <summary>Remover el efecto del objetivo (restaurar estado).</summary>
        public abstract void Remove();

        // ── Stacking ──────────────────────────────────────────

        /// <summary>
        /// El efecto ya existe y se vuelve a aplicar.
        /// Comportamiento por defecto: resetear duración.
        /// Override para acumular stacks.
        /// </summary>
        public virtual void OnStack(float duration, float intensity, int sourceID)
        {
            if (!CanStack)
            {
                // Reset duración (el efecto más fuerte gana)
                Duration  = Mathf.Max(Duration - _elapsed, duration);
                Intensity = Mathf.Max(Intensity, intensity);
                _elapsed  = 0f;
                return;
            }

            StackCount++;
            Duration   = Mathf.Max(Duration - _elapsed, duration);
            _elapsed   = 0f;

            EventBus<OnEffectStackedEvent>.Raise(new OnEffectStackedEvent
            {
                TargetID       = GetComponentInParent<Player.Authority.PlayerAuthority>()?.PlayerID ?? -1,
                EffectID       = EffectID,
                NewStackCount  = StackCount
            });
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive) return;

            _elapsed += deltaTime;     // ⬅️ ahora el efecto controla su tiempo
            UpdateEffect(deltaTime);   // ⬅️ ejecuta su lógica
        }

        /// <summary>¿El efecto ha expirado?</summary>
        public bool HasExpired => _elapsed >= Duration;

        protected int GetOwnerID()
            => GetComponentInParent<Player.Authority.PlayerAuthority>()?.PlayerID ?? -1;
    }
}