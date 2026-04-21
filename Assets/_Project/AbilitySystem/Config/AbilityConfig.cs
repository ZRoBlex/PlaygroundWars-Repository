// ============================================================
//  AbilityConfig.cs
//  AbilitySystem/Config/AbilityConfig.cs
//
//  ScriptableObject con todos los datos de una habilidad.
//  Un asset por habilidad. Cero hardcoding.
//
//  CREAR: Assets → Right Click → Create → Abilities → AbilityConfig
// ============================================================

using Abilities.Events;
using UnityEngine;

namespace Abilities.Config
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/AbilityConfig", order = 0)]
    public class AbilityConfig : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────

        [Header("Identidad")]
        [Tooltip("ID único usado en eventos. Ej: 'slow_field', 'freeze_blast'")]
        public string  AbilityID    = "ability_default";
        public string  DisplayName  = "Habilidad";
        [TextArea(2, 4)]
        public string  Description  = "";
        public Sprite  Icon;

        // ── Cooldown ──────────────────────────────────────────

        [Header("Cooldown")]
        [Range(0f, 60f)]
        public float Cooldown       = 5f;

        [Tooltip("Reducción de cooldown global (multiplicador). 1 = sin reducción.")]
        [Range(0.1f, 1f)]
        public float CooldownScale  = 1f;

        // ── Targeting ─────────────────────────────────────────

        [Header("Targeting")]
        public TargetType TargetType = TargetType.Self;

        [Tooltip("Radio del área (solo para TargetType.Area).")]
        [Range(0.5f, 20f)]
        public float AreaRadius      = 5f;

        [Tooltip("Alcance máximo para objetivos lejanos.")]
        [Range(1f, 50f)]
        public float Range           = 10f;

        [Tooltip("Capas válidas para detectar objetivos.")]
        public LayerMask TargetLayers = ~0;

        [Tooltip("Puede apuntar a aliados.")]
        public bool CanTargetAllies  = false;

        [Tooltip("Puede apuntarse a sí mismo.")]
        public bool CanTargetSelf    = true;

        // ── Efectos ───────────────────────────────────────────

        [Header("Efectos")]
        [Range(0f, 500f)]
        public float Damage          = 0f;

        [Range(0f, 500f)]
        public float Heal            = 0f;

        [Tooltip("ID de los status effects que aplica (ej: 'slow', 'freeze').")]
        public string[] StatusEffectIDs;

        [Tooltip("Duración de los status effects aplicados.")]
        [Range(0.5f, 20f)]
        public float EffectDuration  = 3f;

        [Tooltip("Intensidad del efecto (0-1 para porcentajes, valor absoluto para otros).")]
        [Range(0f, 1f)]
        public float EffectIntensity = 0.5f;

        // ── Red ───────────────────────────────────────────────

        [Header("Red")]
        public bool UseNetworking    = false;

        // ── FX ────────────────────────────────────────────────

        [Header("FX (opcionales)")]
        public GameObject ActivationFX;
        public GameObject ImpactFX;
        [Range(0f, 5f)]
        public float      FXDuration = 1f;
    }
}
