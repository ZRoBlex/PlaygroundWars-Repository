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
    //  CHARACTER STAT DEFINITION
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Define una stat del proyecto (catálogo global).
    /// Permite saber qué stats existen, sus rangos y descripciones.
    /// CREAR: Assets → Create → Character System → Stat Definition
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewStatDef",
        menuName = "Character System/Stat Definition")]
    public class CharacterStatDef : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("ID único. Ej: 'hp', 'speed', 'damage', 'range'.")]
        public string StatID      = "stat_id";
        public string DisplayName = "Stat";

        [TextArea(1, 3)]
        public string Description = "";

        [Header("Rango")]
        public float MinValue  = 0f;
        public float MaxValue  = 1000f;
        public float BaseValue = 100f;

        [Header("Visualización")]
        [Tooltip("Si true, la UI mostrará esta stat como porcentaje.")]
        public bool  ShowAsPercent = false;
        public Color StatColor = Color.white;
    }
}