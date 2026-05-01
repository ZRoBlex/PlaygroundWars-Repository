// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityBase.cs                                 ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Core/            ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • AbilityBase        (MonoBehaviour abstracto)        ║
// ║    • AbilityEffectSystem (MonoBehaviour)                 ║
// ║                                                          ║
// ║  ⚠️ SEPARAR:                                            ║
// ║    AbilityEffectSystem → AbilityEffectSystem.cs          ║
// ║    Motivo: responsabilidad distinta y se añade al prefab ║
// ║    del jugador de forma independiente a AbilityBase.     ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Health;
using UnityEngine;

namespace AbilitySystem
{
    // ════════════════════════════════════════════════════════
    //  ABILITY EFFECT SYSTEM
    //  Aplica daño, curación y status effects sobre el target.
    //  Añadir al prefab del jugador (junto a AbilityManager).
    // ════════════════════════════════════════════════════════

    [DisallowMultipleComponent]
    public class AbilityEffectSystem : MonoBehaviour
    {
        private PlayerAuthority _authority;

        private void Awake() => _authority = GetComponent<PlayerAuthority>();

        // ── API ───────────────────────────────────────────────

        public void ApplyEffects(AbilityConfig cfg, TargetingResult target)
        {
            if (cfg == null || !target.Valid) return;

            switch (target.TargetType(cfg))
            {
                case EffectTargetMode.Single:
                    ApplyToSingle(cfg, target.TargetPlayerID, target.Position);
                    break;

                case EffectTargetMode.Area:
                    if (target.AreaColliders != null)
                        foreach (var col in target.AreaColliders)
                        {
                            if (col == null) continue;
                            var auth = col.GetComponentInParent<PlayerAuthority>();
                            if (auth != null)
                                ApplyToSingle(cfg, auth.PlayerID, col.transform.position);
                        }
                    SpawnFX(cfg.ActivationFX, target.Position, cfg.FXDuration);
                    break;
            }
        }

        // ── Aplicar a un objetivo ─────────────────────────────

        private void ApplyToSingle(AbilityConfig cfg, int targetID, Vector3 hitPoint)
        {
            if (targetID < 0) return;

            // Daño → server authority
            if (cfg.Damage > 0f)
            {
                EventBus<Core.Events.ApplyDamageRequestEvent>.Raise(
                    new Core.Events.ApplyDamageRequestEvent
                    {
                        AttackerID = _authority.PlayerID,
                        TargetID   = targetID,
                        Damage     = cfg.Damage,
                        HitPoint   = hitPoint,
                        HitNormal  = Vector3.up,
                        WeaponID   = cfg.AbilityID
                    });
            }

            // Curación → directo al PlayerHealth del target
            if (cfg.Heal > 0f)
            {
                var targetGO = FindPlayer(targetID);
                targetGO?.GetComponent<PlayerHealth>()?.Heal(cfg.Heal);
            }

            // Status effects
            if (cfg.StatusEffectPrefabs?.Length > 0)
            {
                var targetGO = FindPlayer(targetID);
                if (targetGO != null)
                {
                    var sem = targetGO.GetComponent<StatusEffectManager>();
                    if (sem != null)
                        foreach (var prefab in cfg.StatusEffectPrefabs)
                            if (prefab != null)
                                sem.Apply(prefab, cfg.EffectDuration,
                                    cfg.EffectIntensity, _authority.PlayerID);
                }
            }

            SpawnFX(cfg.ImpactFX, hitPoint, cfg.FXDuration);
        }

        // ── Helpers ───────────────────────────────────────────

        private GameObject FindPlayer(int playerID)
        {
            var all = FindObjectsByType<PlayerAuthority>(FindObjectsSortMode.None);
            foreach (var a in all)
                if (a.PlayerID == playerID) return a.gameObject;
            return null;
        }

        private void SpawnFX(GameObject prefab, Vector3 pos, float lifetime)
        {
            if (prefab == null) return;
            Destroy(Instantiate(prefab, pos, Quaternion.identity), lifetime);
        }
    }

    // ── Extension helper para TargetingResult ────────────────

    public enum EffectTargetMode { Single, Area }

    public static class TargetingResultExtensions
    {
        public static EffectTargetMode TargetType(
            this TargetingResult result, AbilityConfig cfg)
        {
            return cfg.TargetType == AbilitySystem.TargetType.Area
                ? EffectTargetMode.Area
                : EffectTargetMode.Single;
        }
    }
}