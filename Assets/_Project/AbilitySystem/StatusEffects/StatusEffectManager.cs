// ============================================================
//  StatusEffectManager.cs
//  AbilitySystem/StatusEffects/StatusEffectManager.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar efectos de estado activos en una entidad.
//
//  • Añadir / remover efectos dinámicamente
//  • Actualizar efectos activos (sin Update innecesario)
//  • Eliminar efectos expirados automáticamente
//  • Stacking configurable por efecto
// ============================================================

namespace Abilities.StatusEffects
{
    using System.Collections.Generic;
    using Abilities.Events;
    using Core.Debug;
    using Core.Events;
    using Player.Authority;
    using UnityEngine;

    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class StatusEffectManager : MonoBehaviour
    {
        // ── Estado ────────────────────────────────────────────

        private readonly Dictionary<string, StatusEffectBase> _active = new();
        private PlayerAuthority _authority;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            float dt      = Time.deltaTime;
            var   expired = new List<string>();

            foreach (var (id, effect) in _active)
            {
                if (!effect.IsActive) { expired.Add(id); continue; }

                // effect.UpdateEffect(dt);
                // effect._elapsed += dt;
                effect.Tick(dt);

                if (effect.HasExpired)
                    expired.Add(id);
            }

            foreach (var id in expired)
                RemoveEffect(id, expired: true);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Aplica un efecto de estado. Si ya existe y puede stackear, lo stackea.
        /// </summary>
        public void ApplyEffect(
            StatusEffectBase effectPrefab,
            float            duration,
            float            intensity,
            int              sourceID)
        {
            if (effectPrefab == null) return;

            string id = effectPrefab.EffectID;

            // Efecto ya activo
            if (_active.TryGetValue(id, out var existing))
            {
                existing.OnStack(duration, intensity, sourceID);

                CoreLogger.LogSystemDebug("StatusEffectManager",
                    $"[P{_authority.PlayerID}] Efecto '{id}' stackeado.");
                return;
            }

            // Instanciar y añadir
            var instance = Instantiate(effectPrefab, transform);
            instance.Initialize(duration, intensity, sourceID);
            instance.Apply();

            _active[id] = instance;

            CoreLogger.LogSystem("StatusEffectManager",
                $"[P{_authority.PlayerID}] Efecto '{id}' aplicado ({duration:F1}s i={intensity:F2})");

            EventBus<OnEffectAppliedEvent>.Raise(new OnEffectAppliedEvent
            {
                TargetID       = _authority.PlayerID,
                EffectID       = id,
                Duration       = duration,
                Intensity      = intensity,
                SourcePlayerID = sourceID
            });
        }

        /// <summary>Remueve un efecto activo manualmente.</summary>
        public void RemoveEffect(string effectID, bool expired = false)
        {
            if (!_active.TryGetValue(effectID, out var effect)) return;

            effect.Remove();
            Destroy(effect.gameObject);
            _active.Remove(effectID);

            EventBus<OnEffectRemovedEvent>.Raise(new OnEffectRemovedEvent
            {
                TargetID = _authority.PlayerID,
                EffectID = effectID,
                Expired  = expired
            });

            CoreLogger.LogSystemDebug("StatusEffectManager",
                $"[P{_authority.PlayerID}] Efecto '{effectID}' removido (expirado={expired})");
        }

        /// <summary>Remueve todos los efectos activos.</summary>
        public void RemoveAll()
        {
            foreach (var id in new List<string>(_active.Keys))
                RemoveEffect(id);
        }

        public bool HasEffect(string effectID) => _active.ContainsKey(effectID);
        public int  ActiveCount                => _active.Count;

        public StatusEffectBase GetEffect(string id)
            => _active.TryGetValue(id, out var e) ? e : null;
    }
}
