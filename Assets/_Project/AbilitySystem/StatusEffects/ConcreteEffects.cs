// ============================================================
//  ConcreteEffects.cs
//  AbilitySystem/StatusEffects/ConcreteEffects.cs
//
//  Implementaciones concretas de efectos de estado.
//  • SlowEffect   — reduce velocidad
//  • FreezeEffect — inmovilización total
//  • BlindEffect  — reduce visión de la cámara
//
//  Para añadir un nuevo efecto: heredar StatusEffectBase,
//  implementar Apply() / UpdateEffect() / Remove().
// ============================================================

using Abilities.Events;
using Abilities.StatusEffects;
using Core.Events;
using Player.Movement;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
//  SLOW EFFECT
// ─────────────────────────────────────────────────────────────

namespace Abilities.StatusEffects
{
    /// <summary>
    /// Reduce la velocidad del jugador por la fracción Intensity.
    /// Intensity = 0.5 → 50% de reducción de velocidad.
    ///
    /// INTEGRACIÓN: Busca PlayerMovement en el padre y reduce su SpeedMultiplier.
    /// Si usas PlayerMovement_Fixed, añade un campo público SpeedMultiplier
    /// (o usa el patrón de modificadores que ya tenga tu movimiento).
    /// </summary>
    public class SlowEffect : StatusEffectBase
    {
        private PlayerMovement_Fixed _movement;

        protected virtual void Awake()
        {
            EffectID = "slow";
            CanStack = false;
            _movement = GetComponentInParent<PlayerMovement_Fixed>();
        }

        public override void Apply()
        {
            if (_movement == null) return;

            // ✅ Aplicar multiplicador de velocidad
            // Intensity = 0.5 → el jugador va al 50% de su velocidad
            float mult = 1f - Mathf.Clamp01(Intensity);
            _movement.SetSpeedMultiplier(mult);

            // CoreLogger.LogSystemDebug("SlowEffect",
            //     $"Slow aplicado: velocidad × {mult:F2}");
        }

        public override void UpdateEffect(float deltaTime) { /* Sin lógica frame-a-frame */ }

        public override void Remove()
        {
            // Restaurar velocidad normal
            _movement?.SetSpeedMultiplier(1f);
            // CoreLogger.LogSystemDebug("SlowEffect", "Slow removido: velocidad restaurada.");
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  FREEZE EFFECT
// ─────────────────────────────────────────────────────────────

namespace Abilities.StatusEffects
{
    /// <summary>
    /// Inmoviliza totalmente al jugador (velocidad = 0 y bloquea input).
    /// Al expirar, restaura input y velocidad.
    /// </summary>
    public class FreezeEffect : StatusEffectBase
    {
        private PlayerMovement_Fixed     _movement;
        private Player.Input.PlayerInput _input;

        protected virtual void Awake()
        {
            EffectID = "freeze";
            CanStack = false;

            _movement = GetComponentInParent<PlayerMovement_Fixed>();
            _input    = GetComponentInParent<Player.Input.PlayerInput>();
        }

        public override void Apply()
        {
            // 1. Velocidad a cero
            _movement?.SetSpeedMultiplier(0f);

            // 2. Bloquear input del jugador
            _input?.DisableInput();

            // 3. Publicar estado para que otros sistemas reaccionen
            EventBus<OnEffectAppliedEvent>.Raise(new OnEffectAppliedEvent
            {
                TargetID       = GetOwnerID(),
                EffectID       = EffectID,
                Duration       = Duration,
                Intensity      = 1f,
                SourcePlayerID = SourcePlayerID
            });

            // CoreLogger.LogSystem("FreezeEffect", $"[P{GetOwnerID()}] Congelado por {Duration:F1}s");
        }

        public override void UpdateEffect(float deltaTime) { /* Sin lógica frame-a-frame */ }

        public override void Remove()
        {
            _movement?.SetSpeedMultiplier(1f);
            _input?.EnableInput();

            // CoreLogger.LogSystem("FreezeEffect", $"[P{GetOwnerID()}] Freeze expirado.");
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  BLIND EFFECT
// ─────────────────────────────────────────────────────────────

namespace Abilities.StatusEffects
{
    /// <summary>
    /// Reduce la visión del jugador.
    /// Intensity = 0.8 → 80% de la pantalla cubierta.
    ///
    /// INTEGRACIÓN CON UI:
    /// Este efecto publica OnEffectAppliedEvent con EffectID="blind".
    /// El HUD (BlindHUDController) escucha ese evento y muestra/oculta
    /// un overlay oscuro. Este script NO toca la UI directamente.
    ///
    /// ALTERNATIVA (sin HUD): Reducir el FOV de la cámara.
    /// </summary>
    public class BlindEffect : StatusEffectBase
    {
        [Header("Cámara (opcional — alternativa al HUD overlay)")]
        [Tooltip("Si asignas esto, reduce el FOV de la cámara como ceguera.")]
        [SerializeField] private Camera _targetCamera;

        private float _originalFOV;

        protected virtual void Awake()
        {
            EffectID = "blind";
            CanStack = false;

            if (_targetCamera == null)
                _targetCamera = GetComponentInParent<Player.Camera.PlayerCameraController>()
                                    ?.ActiveCamera;
        }

        public override void Apply()
        {
            // Opción A: Reducir FOV (efecto de visión de túnel)
            if (_targetCamera != null)
            {
                _originalFOV = _targetCamera.fieldOfView;
                float newFOV = Mathf.Lerp(_originalFOV, 20f, Intensity);
                _targetCamera.fieldOfView = newFOV;
            }

            // Opción B: Notificar al HUD para overlay (recomendado)
            EventBus<OnEffectAppliedEvent>.Raise(new OnEffectAppliedEvent
            {
                TargetID       = GetOwnerID(),
                EffectID       = EffectID,
                Duration       = Duration,
                Intensity      = Intensity,
                SourcePlayerID = SourcePlayerID
            });

            // CoreLogger.LogSystem("BlindEffect",
            //     $"[P{GetOwnerID()}] Cegado {Duration:F1}s intensidad={Intensity:F2}");
        }

        public override void UpdateEffect(float deltaTime)
        {
            // Efecto pulsante (opcional): variar intensidad sobre el tiempo
            if (_targetCamera == null) return;
            float t       = _elapsed / Duration;
            float pulse   = Mathf.Sin(t * Mathf.PI * 4f) * 0.05f;
            float baseFOV = Mathf.Lerp(_originalFOV, 20f, Intensity);
            _targetCamera.fieldOfView = baseFOV + pulse * _originalFOV;
        }

        public override void Remove()
        {
            if (_targetCamera != null)
                _targetCamera.fieldOfView = _originalFOV;

            EventBus<OnEffectRemovedEvent>.Raise(new OnEffectRemovedEvent
            {
                TargetID = GetOwnerID(),
                EffectID = EffectID,
                Expired  = true
            });

            // CoreLogger.LogSystem("BlindEffect", $"[P{GetOwnerID()}] Blind removido.");
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  EXTENSIÓN para PlayerMovement_Fixed (SetSpeedMultiplier)
// ─────────────────────────────────────────────────────────────

namespace Player.Movement
{
    // Añadir este método a PlayerMovement_Fixed.cs en el proyecto:
    // Aquí se muestra como extensión de ejemplo.
    //
    // En el PlayerMovement_Fixed.cs real, añadir:
    //
    //   private float _speedMultiplier = 1f;
    //   public void SetSpeedMultiplier(float mult) => _speedMultiplier = Mathf.Clamp01(mult);
    //
    // Y en UpdateHorizontal(), multiplicar la velocidad:
    //   float speed = (base_speed) * _speedMultiplier;
}
