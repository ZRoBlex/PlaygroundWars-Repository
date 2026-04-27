// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: CharacterData.cs                               ║
// ║  CARPETA: Assets/_Project/CharacterSystem/Data/          ║
// ║                                                          ║
// ║  CLASES (separar si es necesario):                       ║
// ║    • StatEntry           (struct serializable)           ║
// ║    • CharacterSkinData   (ScriptableObject)              ║
// ║    • CharacterStatDef    (ScriptableObject)              ║
// ║    • CharacterData       (ScriptableObject) ← principal  ║
// ║                                                          ║
// ║  ⚠️ SEPARAR en archivos individuales:                   ║
// ║    CharacterSkinData.cs, CharacterStatDef.cs             ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem.Data
{
    // ════════════════════════════════════════════════════════
    //  CHARACTER DATA — blueprint del personaje
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Define completamente un personaje. Solo datos, sin lógica.
    /// CREAR: Assets → Create → Character System → Character
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewCharacter",
        menuName = "Character System/Character Data")]
    public class CharacterData : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────

        [Header("Identidad")]
        [Tooltip("ID único usado en eventos. Ej: 'warrior', 'mage', 'scout'.")]
        public string CharacterID   = "character_id";
        public string DisplayName   = "Personaje";

        [TextArea(2, 5)]
        public string Description   = "";

        public Sprite Icon;

        // ── Visual ────────────────────────────────────────────

        [Header("Prefab Visual")]
        [Tooltip("Prefab del personaje que se instancia en la escena.")]
        public GameObject CharacterPrefab;

        [Tooltip("Punto de attachment en el player rig (opcional).")]
        public Vector3    PrefabOffset = Vector3.zero;
        public Vector3    PrefabRotation = Vector3.zero;

        // ── Stats base ────────────────────────────────────────

        [Header("Stats Base")]
        [Tooltip("Lista de StatID → valor para este personaje. " +
                 "Los IDs deben coincidir con CharacterStatDef assets.")]
        public List<StatEntry> BaseStats = new()
        {
            new StatEntry { StatID = "hp",    Value = 100f },
            new StatEntry { StatID = "speed", Value = 5f   },
            new StatEntry { StatID = "damage",Value = 25f  }
        };

        // ── Habilidades ───────────────────────────────────────

        [Header("Habilidades")]
        [Tooltip("IDs de AbilityDefinition que se añaden al AbilityManager al inicializar.")]
        public string[] AbilityIDs = Array.Empty<string>();

        [Tooltip("Assets de AbilityDefinition (referencia directa, alternativa a IDs).")]
        public UnityEngine.Object[] AbilityDefinitions = Array.Empty<UnityEngine.Object>();

        // ── Skins ─────────────────────────────────────────────

        [Header("Skins")]
        [Tooltip("Skin usada por defecto.")]
        public CharacterSkinData DefaultSkin;

        [Tooltip("Skins adicionales disponibles.")]
        public List<CharacterSkinData> AvailableSkins = new();

        // ── Progresión ────────────────────────────────────────

        [Header("Progresión (requiere CharacterProgression)")]
        [Tooltip("Nivel inicial del personaje.")]
        [Range(1, 100)]
        public int StartLevel = 1;

        [Tooltip("Desbloqueos por nivel. Index = nivel - 1.")]
        public List<LevelUnlock> LevelUnlocks = new();

        // ── Getters ───────────────────────────────────────────

        /// <summary>Obtiene el valor base de una stat. -1 si no existe.</summary>
        public float GetBaseStat(string statID)
        {
            foreach (var entry in BaseStats)
                if (entry.StatID == statID) return entry.Value;
            return -1f;
        }

        /// <summary>Retorna un diccionario de todas las stats base.</summary>
        public Dictionary<string, float> GetAllBaseStats()
        {
            var dict = new Dictionary<string, float>();
            foreach (var entry in BaseStats)
                dict[entry.StatID] = entry.Value;
            return dict;
        }
    }

    // ── LevelUnlock ───────────────────────────────────────────

    [Serializable]
    public class LevelUnlock
    {
        [Range(1, 100)] public int    Level;
        public string   AbilityID   = "";
        public string   SkinID      = "";
        [TextArea(1, 2)]
        public string   Description = "";
    }
}