// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: StatusEffectBase.cs                            ║
// ║  CARPETA: Assets/_Project/AbilitySystem/StatusEffects/   ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • StatusEffectBase    (MonoBehaviour abstracto)       ║
// ║    • StatusEffectManager (MonoBehaviour)                 ║
// ║                                                          ║
// ║  ⚠️ SEPARAR:                                            ║
// ║    StatusEffectManager → StatusEffectManager.cs          ║
// ║    Motivo: se añade al prefab del jugador independiente  ║
// ║    de los efectos concretos. Distinta responsabilidad.   ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace AbilitySystem
{
    // ════════════════════════════════════════════════════════
    //  STATUS EFFECT BASE
    //  Clase abstracta. Los efectos concretos (Slow, Freeze…)
    //  heredan de esta y son prefabs instanciados como hijos
    //  del jugador afectado.
    // ════════════════════════════════════════════════════════

    public abstract class StatusEffectBase : MonoBehaviour
    {
        // ── Datos ─────────────────────────────────────────────

        public string EffectID       { get; protected set; } = "effect_base";
        public float  Duration       { get; protected set; }
        public float  Intensity      { get; protected set; }
        public int    SourceOwnerID  { get; protected set; } = -1;
        public int    StackCount     { get; protected set; } = 1;
        public bool   CanStack       { get; protected set; } = false;
        public bool   IsActive       { get; private set; }

        internal float Elapsed { get; private set; }
        public bool HasExpired => Elapsed >= Duration;

        // ── Inicialización (llamada por StatusEffectManager) ──

        public void Initialize(float duration, float intensity, int sourceID)
        {
            Duration      = duration;
            Intensity     = intensity;
            SourceOwnerID = sourceID;
            Elapsed       = 0f;
            IsActive      = true;
        }

        // ── Contrato ──────────────────────────────────────────

        /// <summary>Aplicar el efecto al objetivo.</summary>
        public abstract void Apply();

        /// <summary>Lógica por frame (llamada por StatusEffectManager).</summary>
        public abstract void UpdateEffect(float dt);

        /// <summary>Restaurar el estado del objetivo al quitar el efecto.</summary>
        public abstract void Remove();

        // ── Stacking ──────────────────────────────────────────

        /// <summary>El efecto ya existe y se vuelve a aplicar.</summary>
        public virtual void OnStack(float duration, float intensity, int sourceID)
        {
            if (CanStack)
            {
                StackCount++;
                EventBus<OnEffectStackedEvt>.Raise(new OnEffectStackedEvt
                {
                    TargetID  = GetOwnerID(),
                    EffectID  = EffectID,
                    NewStacks = StackCount
                });
            }

            // Resetear duración al mayor valor
            Duration = Mathf.Max(Duration - Elapsed, duration);
            Elapsed  = 0f;
        }

        // ── Tick interno (llamado por StatusEffectManager) ────

        internal void Tick(float dt)
        {
            Elapsed += dt;
            UpdateEffect(dt);
        }

        // ── Helper ────────────────────────────────────────────

        protected int GetOwnerID()
        {
            var auth = GetComponentInParent<PlayerAuthority>();
            return auth != null ? auth.PlayerID : -1;
        }
    }
}