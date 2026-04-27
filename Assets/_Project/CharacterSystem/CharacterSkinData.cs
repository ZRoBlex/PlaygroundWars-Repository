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
    // ── StatEntry ─────────────────────────────────────────────

    /// <summary>Par StatID → valor base. Serializable para Inspector.</summary>
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("ID de la stat. Ej: 'hp', 'speed', 'damage'.")]
        public string StatID;

        [Tooltip("Valor base de esta stat para este personaje.")]
        public float  Value;
    }

    // ════════════════════════════════════════════════════════
    //  CHARACTER SKIN DATA
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Datos de una skin alternativa para un personaje.
    /// CREAR: Assets → Create → Character System → Skin
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewSkin",
        menuName = "Character System/Skin Data")]
    public class CharacterSkinData : ScriptableObject
    {
        [Header("Identidad")]
        public string   SkinID      = "skin_default";
        public string   DisplayName = "Default";
        public Sprite   Icon;

        [Header("Visual")]
        [Tooltip("Prefab visual del personaje con esta skin. Si null, usa el del CharacterData.")]
        public GameObject SkinPrefab;

        [Tooltip("Materiales que reemplazan los del prefab base (por índice).")]
        public Material[] Materials = Array.Empty<Material>();

        [Header("Desbloqueo")]
        [Tooltip("Nivel mínimo para usar esta skin. 0 = desde el inicio.")]
        [Range(0, 100)]
        public int UnlockLevel = 0;

        public bool IsUnlockedByDefault => UnlockLevel == 0;
    }
}