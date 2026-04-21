// ============================================================
//  AbilityEffectSystem.cs
//  AbilitySystem/Effects/AbilityEffectSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Aplicar los efectos de una habilidad
//  sobre uno o más objetivos resueltos por AbilityTargetingSystem.
//
//  SOPORTA:
//  • Daño     → vía ApplyDamageRequestEvent (autoridad de servidor)
//  • Curación → vía PlayerHealth.Heal()
//  • Efectos  → vía StatusEffectManager.ApplyEffect()
// ============================================================

using Abilities.Config;
using Abilities.Events;
using Abilities.StatusEffects;
using Core.Events;
using Core.Debug;
using Player.Authority;
using Player.Health;
using UnityEngine;

namespace Abilities
{
    [DisallowMultipleComponent]
    public class AbilityEffectSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Prefabs de efectos de estado")]
        [Tooltip("Prefab de SlowEffect para instanciar en el objetivo.")]
        [SerializeField] private StatusEffects.SlowEffect   _slowPrefab;
        [SerializeField] private StatusEffects.FreezeEffect _freezePrefab;
        [SerializeField] private StatusEffects.BlindEffect  _blindPrefab;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Aplica los efectos definidos en la config al objetivo resuelto.
        /// </summary>
        public void ApplyEffects(AbilityConfig config, AbilityTarget target)
        {
            if (config == null || !target.Valid) return;

            switch (target.Type)
            {
                case TargetType.Self:
                    ApplyToSingle(config, target.TargetPlayerID, target.Position);
                    break;

                case TargetType.Target:
                    ApplyToSingle(config, target.TargetPlayerID, target.Position);
                    break;

                case TargetType.Area:
                    ApplyToArea(config, target);
                    break;

                case TargetType.Direction:
                    // Dirección: el proyectil aplica el daño al impactar
                    // Aquí solo aplicamos efectos visuales/FX si los hay
                    SpawnFX(config.ActivationFX, transform.position, target.Direction, config.FXDuration);
                    break;
            }
        }

        // ── Aplicar a objetivo único ──────────────────────────

        private void ApplyToSingle(AbilityConfig config, int targetID, Vector3 hitPoint)
        {
            if (targetID < 0) return;

            // Daño
            if (config.Damage > 0f)
            {
                EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
                {
                    AttackerID = _authority.PlayerID,
                    TargetID   = targetID,
                    Damage     = config.Damage,
                    HitPoint   = hitPoint,
                    HitNormal  = Vector3.up,
                    WeaponID   = config.AbilityID,
                    IsHeadshot = false,
                    Distance   = 0f
                });
            }

            // Curación (self o aliado)
            if (config.Heal > 0f)
            {
                var targetGO = FindPlayerGO(targetID);
                targetGO?.GetComponent<PlayerHealth>()?.Heal(config.Heal);
            }

            // Status Effects
            ApplyStatusEffects(config, targetID, hitPoint);

            // FX
            SpawnFX(config.ImpactFX, hitPoint, Vector3.up, config.FXDuration);

            CoreLogger.LogSystem("AbilityEffectSystem",
                $"[P{_authority.PlayerID}] '{config.AbilityID}' → P{targetID}");
        }

        // ── Aplicar a área ────────────────────────────────────

        private void ApplyToArea(AbilityConfig config, AbilityTarget target)
        {
            if (target.AreaTargets == null) return;

            SpawnFX(config.ActivationFX, target.Position, Vector3.up, config.FXDuration);

            foreach (var col in target.AreaTargets)
            {
                if (col == null) continue;

                var auth = col.GetComponentInParent<PlayerAuthority>();
                if (auth == null) continue;

                ApplyToSingle(config, auth.PlayerID, col.transform.position);
            }
        }

        // ── Status Effects ────────────────────────────────────

        private void ApplyStatusEffects(AbilityConfig config, int targetID, Vector3 pos)
        {
            if (config.StatusEffectIDs == null || config.StatusEffectIDs.Length == 0) return;

            var targetGO = FindPlayerGO(targetID);
            if (targetGO == null) return;

            var effectManager = targetGO.GetComponent<StatusEffectManager>();
            if (effectManager == null)
            {
                CoreLogger.LogWarning(
                    $"[AbilityEffects] P{targetID} no tiene StatusEffectManager.");
                return;
            }

            foreach (var effectID in config.StatusEffectIDs)
            {
                var prefab = GetEffectPrefab(effectID);
                if (prefab == null) continue;

                effectManager.ApplyEffect(
                    prefab, config.EffectDuration, config.EffectIntensity,
                    _authority.PlayerID);
            }
        }

        // ── Helpers ───────────────────────────────────────────

        private StatusEffectBase GetEffectPrefab(string id) => id switch
        {
            "slow"   => _slowPrefab,
            "freeze" => _freezePrefab,
            "blind"  => _blindPrefab,
            _        => null
        };

        private GameObject FindPlayerGO(int playerID)
        {
            var all = FindObjectsByType<PlayerAuthority>(FindObjectsSortMode.None);
            foreach (var a in all)
                if (a.PlayerID == playerID) return a.gameObject;
            return null;
        }

        private void SpawnFX(GameObject prefab, Vector3 pos, Vector3 dir, float lifetime)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, pos, Quaternion.LookRotation(dir));
            Destroy(go, lifetime);
        }
    }
}
