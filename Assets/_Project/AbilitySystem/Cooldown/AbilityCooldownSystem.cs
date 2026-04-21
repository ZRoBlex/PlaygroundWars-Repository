// ============================================================
//  AbilityCooldownSystem.cs
//  AbilitySystem/Cooldown/AbilityCooldownSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar cooldowns individuales por habilidad.
//
//  • Clase C# pura — sin MonoBehaviour (AbilityManager la posee)
//  • Un timer por habilidad (por AbilityID)
//  • Publica eventos OnCooldownStart / OnCooldownEnd
//  • Sin Update innecesario — usa coroutines controladas
// ============================================================

using System.Collections;
using System.Collections.Generic;
using Abilities.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Abilities
{
    public class AbilityCooldownSystem
    {
        // ── Estado ────────────────────────────────────────────

        // key = AbilityID, value = tiempo restante
        private readonly Dictionary<string, float> _remaining = new();
        private readonly Dictionary<string, Coroutine> _timers = new();

        // ── Config ────────────────────────────────────────────

        private readonly int          _ownerID;
        private readonly MonoBehaviour _runner;

        // ── Constructor ───────────────────────────────────────

        public AbilityCooldownSystem(int ownerID, MonoBehaviour coroutineRunner)
        {
            _ownerID = ownerID;
            _runner  = coroutineRunner;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Inicia el cooldown de una habilidad.
        /// </summary>
        public void StartCooldown(string abilityID, float duration)
        {
            if (string.IsNullOrEmpty(abilityID) || duration <= 0f) return;

            // Cancelar timer anterior si existía
            if (_timers.TryGetValue(abilityID, out var old) && old != null)
                _runner.StopCoroutine(old);

            _remaining[abilityID] = duration;
            _timers[abilityID]    = _runner.StartCoroutine(CooldownRoutine(abilityID, duration));

            EventBus<OnCooldownStartEvent>.Raise(new OnCooldownStartEvent
            {
                OwnerID   = _ownerID,
                AbilityID = abilityID,
                Duration  = duration
            });

            CoreLogger.LogSystemDebug("Cooldown",
                $"[P{_ownerID}] '{abilityID}' en cooldown {duration:F1}s");
        }

        /// <summary>¿La habilidad está disponible (no en cooldown)?</summary>
        public bool IsReady(string abilityID)
            => !_remaining.ContainsKey(abilityID) || _remaining[abilityID] <= 0f;

        /// <summary>Tiempo restante de cooldown. 0 si está lista.</summary>
        public float GetRemaining(string abilityID)
            => _remaining.TryGetValue(abilityID, out float t) ? Mathf.Max(0f, t) : 0f;

        /// <summary>Fracción de progreso (0 = just started, 1 = ready).</summary>
        public float GetProgress(string abilityID, float totalDuration)
        {
            float rem = GetRemaining(abilityID);
            return totalDuration > 0f ? 1f - (rem / totalDuration) : 1f;
        }

        /// <summary>Reinicia el cooldown a 0 (ej: por habilidad de reset).</summary>
        public void ResetCooldown(string abilityID)
        {
            if (_timers.TryGetValue(abilityID, out var c) && c != null)
                _runner.StopCoroutine(c);

            _remaining.Remove(abilityID);
            _timers.Remove(abilityID);

            EventBus<OnCooldownEndEvent>.Raise(new OnCooldownEndEvent
            {
                OwnerID   = _ownerID,
                AbilityID = abilityID
            });
        }

        /// <summary>Reinicia todos los cooldowns.</summary>
        public void ResetAll()
        {
            foreach (var id in new List<string>(_remaining.Keys))
                ResetCooldown(id);
        }

        // ── Coroutine ─────────────────────────────────────────

        private IEnumerator CooldownRoutine(string abilityID, float total)
        {
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed              += Time.deltaTime;
                _remaining[abilityID] = Mathf.Max(0f, total - elapsed);

                // Publicar progreso cada ~0.1s (no cada frame) para no saturar el bus
                if (Mathf.FloorToInt(elapsed * 10f) != Mathf.FloorToInt((elapsed - Time.deltaTime) * 10f))
                {
                    EventBus<OnCooldownProgressEvent>.Raise(new OnCooldownProgressEvent
                    {
                        OwnerID   = _ownerID,
                        AbilityID = abilityID,
                        Remaining = _remaining[abilityID],
                        Total     = total
                    });
                }

                yield return null;
            }

            _remaining.Remove(abilityID);
            _timers.Remove(abilityID);

            EventBus<OnCooldownEndEvent>.Raise(new OnCooldownEndEvent
            {
                OwnerID   = _ownerID,
                AbilityID = abilityID
            });

            CoreLogger.LogSystemDebug("Cooldown",
                $"[P{_ownerID}] '{abilityID}' listo.");
        }
    }
}
