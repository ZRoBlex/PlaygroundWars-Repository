// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityConfig.cs                               ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Config/          ║
// ║                                                          ║
// ║  CLASE ÚNICA: AbilityConfig (ScriptableObject)           ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Solo datos de una habilidad. Sin lógica.              ║
// ║    Una habilidad = un asset de este tipo.                ║
// ║                                                          ║
// ║  CREAR:                                                  ║
// ║    Assets → Right Click → Create →                       ║
// ║    Ability System → Ability Config                       ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • AbilityID vacío → eventos no se identifican         ║
// ║    • StatusEffectIDs con IDs que no coinciden con        ║
// ║      StatusEffectBase.EffectID → efecto no se aplica     ║
// ║    • HitLayers = 0 → raycast de targeting no detecta     ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace AbilitySystem
{
    public enum TargetType { Self, Target, Area, Direction }

    [CreateAssetMenu(
        fileName = "NewAbility",
        menuName = "Ability System/Ability Config")]
    public class AbilityConfig : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────

        [Header("Identidad")]
        [Tooltip("ID único. Ej: 'dash', 'slow_field', 'freeze_blast'.")]
        public string AbilityID   = "ability_id";
        public string DisplayName = "Habilidad";
        [TextArea(1, 3)]
        public string Description = "";
        public Sprite Icon;

        // ── Cooldown ──────────────────────────────────────────

        [Header("Cooldown")]
        [Range(0f, 60f)]
        public float Cooldown       = 5f;

        // ── Targeting ─────────────────────────────────────────

        [Header("Targeting")]
        public TargetType TargetType = TargetType.Self;

        [Tooltip("Alcance máximo (Target / Direction).")]
        [Range(0.5f, 50f)]
        public float Range           = 10f;

        [Tooltip("Radio del área (solo Area).")]
        [Range(0.5f, 20f)]
        public float AreaRadius      = 5f;

        [Tooltip("Capas válidas para detectar objetivos.")]
        public LayerMask HitLayers   = ~0;

        [Tooltip("¿Puede apuntarse a sí mismo?")]
        public bool CanTargetSelf    = true;

        // ── Efectos ───────────────────────────────────────────

        [Header("Efectos")]
        [Range(0f, 500f)]
        public float Damage          = 0f;

        [Range(0f, 500f)]
        public float Heal            = 0f;

        [Tooltip("IDs de efectos de estado a aplicar. " +
                 "Deben coincidir con StatusEffectBase.EffectID.")]
        public string[] StatusEffectIDs = System.Array.Empty<string>();

        [Tooltip("Prefabs de StatusEffectBase para instanciar en el objetivo.")]
        public StatusEffectBase[] StatusEffectPrefabs = System.Array.Empty<StatusEffectBase>();

        [Range(0.1f, 30f)]
        public float EffectDuration  = 3f;

        [Range(0f, 1f)]
        public float EffectIntensity = 0.5f;

        // ── Red ───────────────────────────────────────────────

        [Header("Red")]
        public bool UseNetworking    = false;

        // ── VFX ───────────────────────────────────────────────

        [Header("VFX (opcionales)")]
        public GameObject ActivationFX;
        public GameObject ImpactFX;
        [Range(0f, 5f)]
        public float FXDuration      = 1f;
    }
}