// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: CharacterStats.cs                              ║
// ║  CARPETA: Assets/_Project/CharacterSystem/Runtime/       ║
// ║                                                          ║
// ║  CLASES:                                                 ║
// ║    • ModifierType   (enum)                               ║
// ║    • StatModifier   (struct)   ← separar si crece        ║
// ║    • CharacterStats (class C# puro)  ← sistema principal ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Data;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    // ════════════════════════════════════════════════════════
    //  MODIFIER TYPE
    // ════════════════════════════════════════════════════════

    public enum ModifierType
    {
        /// <summary>Suma valor plano: final = base + flat</summary>
        Flat,
        /// <summary>Suma porcentaje del base: final = base * (1 + percent)</summary>
        Percent,
        /// <summary>Sobreescribe el valor final directamente</summary>
        Override
    }

    // ════════════════════════════════════════════════════════
    //  STAT MODIFIER
    // ════════════════════════════════════════════════════════

    public struct StatModifier
    {
        public string       ModifierID;   // ID único del modificador
        public string       StatID;       // stat que afecta
        public float        Value;        // magnitud
        public ModifierType Type;
        public string       SourceID;     // quién lo aplicó (ability ID, item ID, etc.)
        public float        Duration;     // -1 = permanente, >0 = segundos

        public bool IsPermanent => Duration < 0f;

        public static StatModifier Flat(string id, string stat, float val, string source, float duration = -1f)
            => new() { ModifierID = id, StatID = stat, Value = val, Type = ModifierType.Flat,    SourceID = source, Duration = duration };

        public static StatModifier Percent(string id, string stat, float pct, string source, float duration = -1f)
            => new() { ModifierID = id, StatID = stat, Value = pct, Type = ModifierType.Percent, SourceID = source, Duration = duration };

        public static StatModifier Override(string id, string stat, float val, string source, float duration = -1f)
            => new() { ModifierID = id, StatID = stat, Value = val, Type = ModifierType.Override, SourceID = source, Duration = duration };
    }

    // ════════════════════════════════════════════════════════
    //  CHARACTER STATS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Stats dinámicas por entidad. C# puro — sin MonoBehaviour.
    /// CharacterManager la posee y la inyecta en los sistemas del jugador.
    ///
    /// CÁLCULO FINAL:
    ///   1. Empezar con base
    ///   2. Sumar todos los Flat
    ///   3. Sumar base × sum(Percent)
    ///   4. Si hay Override → usar ese valor directamente
    /// </summary>
    public class CharacterStats
    {
        // ── Estado ────────────────────────────────────────────

        private readonly int    _ownerID;
        private readonly MonoBehaviour _runner;   // para coroutines de duración

        private Dictionary<string, float>                     _base      = new();
        private Dictionary<string, List<StatModifier>>        _modifiers = new();

        // Cache de valores calculados (invalidado al cambiar modificadores)
        private Dictionary<string, float> _cache = new();
        private bool                      _cacheDirty = true;

        // ── Constructor ───────────────────────────────────────

        public CharacterStats(int ownerID, MonoBehaviour coroutineRunner)
        {
            _ownerID = ownerID;
            _runner  = coroutineRunner;
        }

        // ── Inicialización ────────────────────────────────────

        public void Initialize(CharacterData data)
        {
            _base.Clear();
            _modifiers.Clear();
            _cache.Clear();
            _cacheDirty = true;

            if (data == null) return;

            foreach (var entry in data.BaseStats)
                _base[entry.StatID] = entry.Value;

            CoreLogger.LogSystem("CharacterStats",
                $"P{_ownerID}: stats inicializadas ({_base.Count} stats) desde '{data.CharacterID}'");
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Valor final calculado de una stat.</summary>
        public float Get(string statID)
        {
            if (_cacheDirty) RebuildCache();
            return _cache.TryGetValue(statID, out float v) ? v : 0f;
        }

        /// <summary>Valor base sin modificadores.</summary>
        public float GetBase(string statID)
            => _base.TryGetValue(statID, out float v) ? v : 0f;

        /// <summary>Cambia el valor BASE de una stat (ej: al subir de nivel).</summary>
        public void SetBase(string statID, float value)
        {
            float old = GetBase(statID);
            _base[statID]   = value;
            _cacheDirty     = true;
            NotifyChange(statID, old);
        }

        // ── Modificadores ─────────────────────────────────────

        /// <summary>Aplica un modificador. Si tiene duración, lo remueve automáticamente.</summary>
        public void AddModifier(StatModifier mod)
        {
            if (!_modifiers.ContainsKey(mod.StatID))
                _modifiers[mod.StatID] = new List<StatModifier>();

            _modifiers[mod.StatID].Add(mod);
            _cacheDirty = true;

            EventBus<OnModifierAppliedEvt>.Raise(new OnModifierAppliedEvt
            {
                OwnerID    = _ownerID,
                StatID     = mod.StatID,
                ModifierID = mod.ModifierID,
                Value      = mod.Value,
                Duration   = mod.Duration
            });

            if (!mod.IsPermanent && _runner != null)
                _runner.StartCoroutine(RemoveAfterDelay(mod));

            CoreLogger.LogSystemDebug("CharacterStats",
                $"P{_ownerID}: mod '{mod.ModifierID}' ({mod.Type} {mod.Value:+0.##;-0.##}) " +
                $"en '{mod.StatID}' dur={mod.Duration:F1}s");
        }

        /// <summary>Remueve un modificador por su ID.</summary>
        public void RemoveModifier(string modifierID)
        {
            string removedStat = null;

            foreach (var (stat, list) in _modifiers)
            {
                int idx = list.FindIndex(m => m.ModifierID == modifierID);
                if (idx < 0) continue;

                var mod = list[idx];
                list.RemoveAt(idx);
                removedStat = stat;

                EventBus<OnModifierRemovedEvt>.Raise(new OnModifierRemovedEvt
                {
                    OwnerID    = _ownerID,
                    StatID     = stat,
                    ModifierID = modifierID
                });
                break;
            }

            if (removedStat != null)
                _cacheDirty = true;
        }

        /// <summary>Remueve todos los modificadores de una fuente (ej: al desactivar una habilidad).</summary>
        public void RemoveModifiersFromSource(string sourceID)
        {
            bool changed = false;

            foreach (var list in _modifiers.Values)
            {
                int before = list.Count;
                list.RemoveAll(m => m.SourceID == sourceID);
                if (list.Count != before) changed = true;
            }

            if (changed) _cacheDirty = true;
        }

        /// <summary>Remueve todos los modificadores temporales (duración > 0).</summary>
        public void ClearTemporaryModifiers()
        {
            bool changed = false;
            foreach (var list in _modifiers.Values)
            {
                int before = list.Count;
                list.RemoveAll(m => !m.IsPermanent);
                if (list.Count != before) changed = true;
            }
            if (changed) _cacheDirty = true;
        }

        public bool HasModifier(string modifierID)
        {
            foreach (var list in _modifiers.Values)
                if (list.Exists(m => m.ModifierID == modifierID)) return true;
            return false;
        }

        public IReadOnlyDictionary<string, float> GetAllBase() => _base;

        // ── Cálculo ───────────────────────────────────────────

        private void RebuildCache()
        {
            _cache.Clear();

            foreach (var statID in _base.Keys)
                _cache[statID] = Calculate(statID);

            // Incluir stats que solo existen en modificadores (sin base)
            foreach (var statID in _modifiers.Keys)
                if (!_cache.ContainsKey(statID))
                    _cache[statID] = Calculate(statID);

            _cacheDirty = false;
        }

        private float Calculate(string statID)
        {
            float baseVal = _base.TryGetValue(statID, out float b) ? b : 0f;

            if (!_modifiers.TryGetValue(statID, out var mods) || mods.Count == 0)
                return baseVal;

            // Verificar Override (el último gana)
            StatModifier? overrideMod = null;
            float flatSum = 0f, pctSum = 0f;

            foreach (var mod in mods)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:     flatSum    += mod.Value; break;
                    case ModifierType.Percent:  pctSum     += mod.Value; break;
                    case ModifierType.Override: overrideMod = mod;       break;
                }
            }

            if (overrideMod.HasValue) return overrideMod.Value.Value;
            return (baseVal + flatSum) * (1f + pctSum);
        }

        private void NotifyChange(string statID, float oldValue)
        {
            float newValue = Get(statID);
            if (Mathf.Approximately(oldValue, newValue)) return;

            EventBus<OnStatsChangedEvt>.Raise(new OnStatsChangedEvt
            {
                OwnerID  = _ownerID,
                StatID   = statID,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        private IEnumerator RemoveAfterDelay(StatModifier mod)
        {
            yield return new WaitForSeconds(mod.Duration);
            RemoveModifier(mod.ModifierID);
        }
    }
}