// ============================================================
//  PlayerHealth.cs
//  PlayerSystem/Health/PlayerHealth.cs
//
//  RESPONSABILIDAD ÚNICA: Estado de salud del jugador.
//
//  REGLA DE AUTORIDAD:
//  • Solo la autoridad (host/server) aplica daño y curación.
//  • Los clientes SOLICITAN daño via ApplyDamageRequestEvent.
//  • En Offline: la autoridad es local, siempre procesa.
//
//  CARACTERÍSTICAS:
//  • Daño, curación, muerte
//  • Regeneración con delay configurable
//  • Invencibilidad post-daño
//  • Efectos de estado (DoT, slow, freeze) via StatusEffectEvent
//  • Eventos: OnDamaged, OnHealed, OnDied
//  • Separación total de lógica y visual
// ============================================================

using System.Collections;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using UnityEngine;

namespace Player.Health
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;

        // ── Estado ────────────────────────────────────────────

        public float CurrentHealth  { get; private set; }
        public float MaxHealth      { get; private set; }
        public bool  IsAlive        { get; private set; }
        public bool  IsInvincible   { get; private set; }

        public float HealthPercent  => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        // Regen
        private Coroutine _regenCoroutine;
        private Coroutine _invincibilityCoroutine;

        // DoT tracking (daño por tiempo)
        private float _dotAccumulator;
        private Coroutine _dotCoroutine;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        private void OnEnable()
        {
            // Escuchar solicitudes de daño (vienen de cualquier parte del juego)
            EventBus<ApplyDamageRequestEvent>.Subscribe(OnDamageRequested);
            EventBus<PlayerStatusEffectEvent>.Subscribe(OnStatusEffect);
        }

        private void OnDisable()
        {
            EventBus<ApplyDamageRequestEvent>.Unsubscribe(OnDamageRequested);
            EventBus<PlayerStatusEffectEvent>.Unsubscribe(OnStatusEffect);

            StopAllCoroutines();
        }

        // ── Inicialización ────────────────────────────────────

        public void Initialize()
        {
            MaxHealth     = _config != null ? _config.MaxHealth : 100f;
            CurrentHealth = MaxHealth;
            IsAlive       = true;
            IsInvincible  = false;

            CoreLogger.LogSystemDebug("PlayerHealth", $"[P{_authority.PlayerID}] HP={CurrentHealth}/{MaxHealth}");
        }

        // ── API Pública (usada por la autoridad) ──────────────

        /// <summary>
        /// Aplica daño. Solo procesa si esta instancia tiene autoridad.
        /// </summary>
        public void TakeDamage(float amount, int attackerID, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!_authority.HasAuthority) return;
            if (!IsAlive || IsInvincible) return;
            if (amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            CoreLogger.LogSystemDebug("PlayerHealth",
                $"[P{_authority.PlayerID}] Daño: -{amount} | HP={CurrentHealth}/{MaxHealth}"
            );

            // Interrumpir regeneración
            StopRegen();

            // Invincibilidad temporal
            if (_config != null && _config.InvincibilityDuration > 0f)
            {
                if (_invincibilityCoroutine != null) StopCoroutine(_invincibilityCoroutine);
                _invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
            }

            // Disparar evento de daño
            EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent
            {
                PlayerID        = _authority.PlayerID,
                AttackerID      = attackerID,
                Amount          = amount,
                RemainingHealth = CurrentHealth,
                HitPoint        = hitPoint,
                HitNormal       = hitNormal
            });

            if (CurrentHealth <= 0f)
                Die(attackerID);
            else
                StartRegenIfEnabled();
        }

        /// <summary>
        /// Cura al jugador. Solo procesa si esta instancia tiene autoridad.
        /// </summary>
        public void Heal(float amount)
        {
            if (!_authority.HasAuthority) return;
            if (!IsAlive) return;
            if (amount <= 0f) return;

            float prev    = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float actual  = CurrentHealth - prev;

            if (actual <= 0f) return;

            EventBus<PlayerHealedEvent>.Raise(new PlayerHealedEvent
            {
                PlayerID      = _authority.PlayerID,
                Amount        = actual,
                CurrentHealth = CurrentHealth
            });

            CoreLogger.LogSystemDebug("PlayerHealth",
                $"[P{_authority.PlayerID}] Curado: +{actual} | HP={CurrentHealth}/{MaxHealth}"
            );
        }

        /// <summary>Restaura la salud al máximo (para respawn).</summary>
        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            IsAlive       = true;
            IsInvincible  = false;
            StopRegen();
        }

        // ── Muerte ────────────────────────────────────────────

        private void Die(int killerID)
        {
            if (!IsAlive) return;

            IsAlive       = false;
            CurrentHealth = 0f;

            StopRegen();

            CoreLogger.LogSystem("PlayerHealth", $"[P{_authority.PlayerID}] Muerto por P{killerID}.");

            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent
            {
                PlayerID      = _authority.PlayerID,
                KillerID      = killerID,
                DeathPosition = transform.position
            });
        }

        // ── Regeneración ──────────────────────────────────────

        private void StartRegenIfEnabled()
        {
            if (_config == null || !_config.HealthRegenEnabled) return;
            if (Mathf.Approximately(CurrentHealth, MaxHealth)) return;

            StopRegen();
            _regenCoroutine = StartCoroutine(RegenRoutine());
        }

        private void StopRegen()
        {
            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
                _regenCoroutine = null;
            }
        }

        private IEnumerator RegenRoutine()
        {
            yield return new WaitForSeconds(_config.HealthRegenDelay);

            while (IsAlive && CurrentHealth < MaxHealth)
            {
                Heal(_config.HealthRegenRate * Time.deltaTime);
                yield return null;
            }
        }

        // ── Invencibilidad ────────────────────────────────────

        private IEnumerator InvincibilityRoutine()
        {
            IsInvincible = true;
            yield return new WaitForSeconds(_config.InvincibilityDuration);
            IsInvincible = false;
        }

        // ── Daño por tiempo (DoT) ─────────────────────────────

        /// <summary>Inicia daño continuo (veneno, fuego, etc.).</summary>
        public void ApplyDamageOverTime(float damagePerSecond, float duration, int sourceID)
        {
            if (!_authority.HasAuthority) return;
            if (!IsAlive) return;

            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);
            _dotCoroutine = StartCoroutine(DotRoutine(damagePerSecond, duration, sourceID));
        }

        private IEnumerator DotRoutine(float dps, float duration, int sourceID)
        {
            float elapsed = 0f;
            while (elapsed < duration && IsAlive)
            {
                TakeDamage(dps * Time.deltaTime, sourceID, transform.position, Vector3.up);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnDamageRequested(ApplyDamageRequestEvent e)
        {
            if (e.TargetID != _authority.PlayerID) return;
            if (!_authority.HasAuthority) return;

            TakeDamage(e.Damage, e.AttackerID, e.HitPoint, e.HitNormal);
        }

        private void OnStatusEffect(PlayerStatusEffectEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            // Aquí se pueden manejar efectos que afectan la vida (ej: freeze paraliza, DoT)
            // Los efectos de movimiento los maneja PlayerMovement
            CoreLogger.LogSystemDebug("PlayerHealth",
                $"[P{_authority.PlayerID}] Efecto: {e.EffectName} | Apply={e.IsApplied}"
            );
        }
    }
}
