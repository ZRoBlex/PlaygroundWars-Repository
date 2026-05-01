// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityCooldownSystem.cs                       ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Core/            ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • AbilityCooldownSystem  (C# puro)   ← principal      ║
// ║    • AbilityTargetingSystem (MonoBehaviour)              ║
// ║                                                          ║
// ║  ⚠️ SEPARAR:                                            ║
// ║    AbilityTargetingSystem → AbilityTargetingSystem.cs    ║
// ║    Motivo: es un MonoBehaviour que va en el prefab del   ║
// ║    jugador. Conviene tenerlo en su propio archivo para   ║
// ║    añadirlo/quitarlo independientemente.                 ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace AbilitySystem
{
    // ════════════════════════════════════════════════════════
    //  ABILITY COOLDOWN SYSTEM
    //  C# puro. AbilityManager lo posee (uno por jugador).
    // ════════════════════════════════════════════════════════

    public class AbilityCooldownSystem
    {
        private readonly Dictionary<string, float>    _remaining  = new();
        private readonly Dictionary<string, Coroutine> _coroutines = new();
        private readonly int          _ownerID;
        private readonly MonoBehaviour _runner;

        public AbilityCooldownSystem(int ownerID, MonoBehaviour runner)
        {
            _ownerID = ownerID;
            _runner  = runner;
        }

        // ── API ───────────────────────────────────────────────

        public void StartCooldown(string abilityID, float duration)
        {
            if (string.IsNullOrEmpty(abilityID) || duration <= 0f) return;

            if (_coroutines.TryGetValue(abilityID, out var old) && old != null)
                _runner.StopCoroutine(old);

            _remaining[abilityID]  = duration;
            _coroutines[abilityID] = _runner.StartCoroutine(Routine(abilityID, duration));

            EventBus<OnCooldownStartEvt>.Raise(new OnCooldownStartEvt
            {
                OwnerID   = _ownerID,
                AbilityID = abilityID,
                Duration  = duration
            });
        }

        public bool  IsReady(string id)
            => !_remaining.ContainsKey(id) || _remaining[id] <= 0f;

        public float GetRemaining(string id)
            => _remaining.TryGetValue(id, out float t) ? Mathf.Max(0f, t) : 0f;

        public float GetProgress(string id, float total)
            => total > 0f ? 1f - GetRemaining(id) / total : 1f;

        public void Reset(string id)
        {
            if (_coroutines.TryGetValue(id, out var c) && c != null)
                _runner.StopCoroutine(c);
            _remaining.Remove(id);
            _coroutines.Remove(id);
            EventBus<OnCooldownEndEvt>.Raise(new OnCooldownEndEvt
                { OwnerID = _ownerID, AbilityID = id });
        }

        public void ResetAll()
        {
            foreach (var id in new List<string>(_remaining.Keys)) Reset(id);
        }

        // ── Coroutine ─────────────────────────────────────────

        private IEnumerator Routine(string id, float total)
        {
            float elapsed = 0f, tick = 0f;

            while (elapsed < total)
            {
                yield return null;
                elapsed          += Time.deltaTime;
                tick             -= Time.deltaTime;
                _remaining[id]    = Mathf.Max(0f, total - elapsed);

                if (tick <= 0f)
                {
                    tick = 0.1f;
                    EventBus<OnCooldownTickEvt>.Raise(new OnCooldownTickEvt
                    {
                        OwnerID   = _ownerID,
                        AbilityID = id,
                        Remaining = _remaining[id],
                        Total     = total
                    });
                }
            }

            _remaining.Remove(id);
            _coroutines.Remove(id);
            EventBus<OnCooldownEndEvt>.Raise(new OnCooldownEndEvt
                { OwnerID = _ownerID, AbilityID = id });
        }
    }
}