// ============================================================
//  HitDetectionSystem_Fixed2.cs
//  Combat/Systems/HitDetectionSystem_Fixed2.cs
//
//  ⚠️  REEMPLAZA: HitDetectionSystem_Fixed.cs (la versión anterior)
//
//  ERRORES QUE CORRIGE:
//  1. "OnHitEvent could not be found" → añadido 'using Combat.Events'
//  2. Campos de ApplyDamageRequestEvent actualizados a los canónicos
//     (AttackerID, TargetID, Damage en lugar de los campos viejos)
//
//  REGLA DE AUTORIDAD:
//  HitDetection SOLO emite ApplyDamageRequestEvent (una REQUEST).
//  ServerDamageProcessor la valida y aplica — nunca el cliente.
// ============================================================

using Combat.Events;          // OnHitEvent, OnShootEvent
using Core.Events;            // ApplyDamageRequestEvent (canónico)
using Core.Debug;
using Player.Authority;
using UnityEngine;

namespace Combat.Systems
{
    public static class HitDetectionSystem_Fixed
    {
        private const string TAG_PLAYER = "Player";
        private const string TAG_HEAD   = "Head";

        // ── HitScan ───────────────────────────────────────────

        /// <summary>
        /// Raycast desde la CÁMARA del jugador (no desde el cañón).
        /// Emite ApplyDamageRequestEvent — nunca aplica daño directo.
        /// </summary>
        public static void ProcessHitScan(
            WeaponConfig config,
            int          shooterID,
            Camera       playerCamera,
            Vector3      muzzlePosition)   // Solo para FX visuales
        {
            if (config == null || playerCamera == null)
            {
                CoreLogger.LogError("[HitDetection] WeaponConfig o Camera nulos.");
                return;
            }

            // ✅ FIX: LayerMask = 0 no detecta nada → usar ~0 como fallback
            LayerMask mask = config.HitLayers.value == 0 ? ~0 : config.HitLayers;

            int pellets = Mathf.Max(1, config.PelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplySpread(playerCamera.transform.forward, config.SpreadAngle);
                FireRay(config, shooterID, playerCamera.transform.position, dir, mask);
            }
        }

        private static void FireRay(
            WeaponConfig config,
            int          shooterID,
            Vector3      origin,
            Vector3      direction,
            LayerMask    mask)
        {
            bool hit = Physics.Raycast(origin, direction, out RaycastHit info,
                           config.MaxRange, mask, QueryTriggerInteraction.Ignore);

            bool  hitPlayer  = hit && info.collider.CompareTag(TAG_PLAYER);
            bool  isHeadshot = hit && info.collider.CompareTag(TAG_HEAD);
            int   targetID   = hit ? ResolveID(info.collider) : -1;

            // Evento de hit para efectos visuales / audio (NO gameplay)
            EventBus<OnHitEvent>.Raise(new OnHitEvent
            {
                ShooterID = shooterID,
                WeaponID  = config.WeaponID,
                HitPoint  = hit ? info.point : origin + direction * config.MaxRange,
                HitNormal = hit ? info.normal : -direction,
                HitPlayer = hitPlayer || isHeadshot,
                TargetID  = targetID,
                Distance  = hit ? info.distance : config.MaxRange
            });

            if (!hit || (!hitPlayer && !isHeadshot)) return;
            if (targetID < 0) return;

            // ✅ Solo emite REQUEST — campos canónicos
            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                AttackerID = shooterID,
                TargetID   = targetID,
                Damage     = config.BaseDamage,
                HitPoint   = info.point,
                HitNormal  = info.normal,
                WeaponID   = config.WeaponID,
                IsHeadshot = isHeadshot,
                Distance   = info.distance
            });
        }

        // ── Proyectil ─────────────────────────────────────────

        public static void ProcessProjectileImpact(
            WeaponConfig config,
            int          shooterID,
            Collider     hitCollider,
            Vector3      hitPoint,
            Vector3      hitNormal)
        {
            if (config == null || hitCollider == null) return;

            bool hitPlayer  = hitCollider.CompareTag(TAG_PLAYER);
            bool isHeadshot = hitCollider.CompareTag(TAG_HEAD);
            int  targetID   = ResolveID(hitCollider);

            EventBus<OnHitEvent>.Raise(new OnHitEvent
            {
                ShooterID = shooterID,
                WeaponID  = config.WeaponID,
                HitPoint  = hitPoint,
                HitNormal = hitNormal,
                HitPlayer = hitPlayer || isHeadshot,
                TargetID  = targetID,
                Distance  = 0f
            });

            if (!hitPlayer && !isHeadshot) return;
            if (targetID < 0) return;

            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                AttackerID = shooterID,
                TargetID   = targetID,
                Damage     = config.BaseDamage,
                HitPoint   = hitPoint,
                HitNormal  = hitNormal,
                WeaponID   = config.WeaponID,
                IsHeadshot = isHeadshot,
                Distance   = 0f
            });
        }

        // ── Continuo ──────────────────────────────────────────

        public static void ProcessContinuous(
            WeaponConfig config,
            int          shooterID,
            Camera       playerCamera,
            float        deltaTime)
        {
            if (config == null || playerCamera == null) return;

            LayerMask mask = config.HitLayers.value == 0 ? ~0 : config.HitLayers;

            if (!Physics.Raycast(playerCamera.transform.position,
                    playerCamera.transform.forward, out RaycastHit hit,
                    config.ContinuousRange, mask,
                    QueryTriggerInteraction.Ignore)) return;

            bool hitPlayer  = hit.collider.CompareTag(TAG_PLAYER);
            bool isHeadshot = hit.collider.CompareTag(TAG_HEAD);
            if (!hitPlayer && !isHeadshot) return;

            int targetID = ResolveID(hit.collider);
            if (targetID < 0) return;

            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                AttackerID = shooterID,
                TargetID   = targetID,
                Damage     = config.DamagePerSecond * deltaTime,
                HitPoint   = hit.point,
                HitNormal  = hit.normal,
                WeaponID   = config.WeaponID,
                IsHeadshot = false,
                Distance   = hit.distance
            });
        }

        // ── Helpers ───────────────────────────────────────────

        private static int ResolveID(Collider col)
        {
            var auth = col.GetComponentInParent<PlayerAuthority>();
            return auth != null ? auth.PlayerID : -1;
        }

        private static Vector3 ApplySpread(Vector3 dir, float angle)
        {
            if (angle <= 0f) return dir;
            float h = angle * 0.5f;
            return Quaternion.Euler(Random.Range(-h, h), Random.Range(-h, h), 0f) * dir;
        }
    }

    // ── ServerDamageProcessor ─────────────────────────────────

    /// <summary>
    /// Única autoridad de daño en el juego.
    /// Añadir al [GameManager] en modo offline o al Host en multiplayer.
    /// NO añadir a clientes remotos.
    /// </summary>
    public class ServerDamageProcessor : MonoBehaviour
    {
        [Header("Autoridad")]
        [SerializeField] private bool _isAuthority = true;

        [Header("Anti-Cheat")]
        [SerializeField] private float _maxDamagePerHit = 999f;

        private void OnEnable()
        {
            EventBus<ApplyDamageRequestEvent>.Subscribe(OnRequest);
        }

        private void OnDisable()
        {
            EventBus<ApplyDamageRequestEvent>.Unsubscribe(OnRequest);
        }

        private void OnRequest(ApplyDamageRequestEvent req)
        {
            if (!_isAuthority) return;

            // Validación anti-cheat básica
            if (req.Damage > _maxDamagePerHit || req.TargetID < 0)
            {
                EventBus<DamageRequestRejectedEvent>.Raise(new DamageRequestRejectedEvent
                {
                    AttackerID = req.AttackerID,
                    TargetID   = req.TargetID,
                    Reason     = req.TargetID < 0 ? "InvalidTarget" : "ExcessiveDamage"
                });
                return;
            }

            // Calcular daño final (headshot + falloff)
            float final = req.Damage;
            if (req.IsHeadshot) final *= 2f;

            EventBus<DamageAppliedEvent>.Raise(new DamageAppliedEvent
            {
                AttackerID  = req.AttackerID,
                TargetID    = req.TargetID,
                FinalDamage = Mathf.Max(1f, Mathf.Round(final)),
                HitPoint    = req.HitPoint,
                WeaponID    = req.WeaponID
            });
        }
    }
}
