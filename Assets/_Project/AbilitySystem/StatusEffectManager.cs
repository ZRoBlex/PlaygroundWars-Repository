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
    //  STATUS EFFECT MANAGER
    //  Gestiona todos los efectos activos en una entidad.
    //  Añadir al prefab del jugador.
    // ════════════════════════════════════════════════════════

    [DisallowMultipleComponent]
    public class StatusEffectManager : MonoBehaviour
    {
        // ── Estado ────────────────────────────────────────────

        private readonly Dictionary<string, StatusEffectBase> _active = new();
        private PlayerAuthority _authority;

        public int ActiveCount => _active.Count;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake() => _authority = GetComponent<PlayerAuthority>();

        // Update solo se ejecuta si hay efectos activos
        private void Update()
        {
            if (_active.Count == 0) return;

            float dt      = Time.deltaTime;
            var   expired = new List<string>();

            foreach (var (id, fx) in _active)
            {
                if (!fx.IsActive) { expired.Add(id); continue; }
                fx.Tick(dt);
                if (fx.HasExpired) expired.Add(id);
            }

            foreach (var id in expired)
                RemoveEffect(id, expired: true);
        }

        // ── API ───────────────────────────────────────────────

        /// <summary>
        /// Aplica un efecto de estado.
        /// Si ya existe y CanStack es true, lo stackea.
        /// Si no, resetea la duración.
        /// </summary>
        public void Apply(StatusEffectBase prefab, float duration,
            float intensity, int sourceID)
        {
            if (prefab == null) return;
            string id = prefab.EffectID;

            if (_active.TryGetValue(id, out var existing))
            {
                existing.OnStack(duration, intensity, sourceID);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.Initialize(duration, intensity, sourceID);
            instance.Apply();
            _active[id] = instance;

            EventBus<OnEffectAppliedEvt>.Raise(new OnEffectAppliedEvt
            {
                TargetID     = _authority.PlayerID,
                EffectID     = id,
                SourceOwnerID = sourceID,
                Duration     = duration,
                Intensity    = intensity,
                StackCount   = instance.StackCount
            });
        }

        /// <summary>Remueve un efecto por ID.</summary>
        public void RemoveEffect(string effectID, bool expired = false)
        {
            if (!_active.TryGetValue(effectID, out var fx)) return;

            fx.Remove();
            Destroy(fx.gameObject);
            _active.Remove(effectID);

            EventBus<OnEffectRemovedEvt>.Raise(new OnEffectRemovedEvt
            {
                TargetID = _authority.PlayerID,
                EffectID = effectID,
                Expired  = expired
            });
        }

        public void RemoveAll()
        {
            foreach (var id in new List<string>(_active.Keys))
                RemoveEffect(id);
        }

        public bool Has(string effectID) => _active.ContainsKey(effectID);

        public StatusEffectBase Get(string effectID)
            => _active.TryGetValue(effectID, out var fx) ? fx : null;
    }
}