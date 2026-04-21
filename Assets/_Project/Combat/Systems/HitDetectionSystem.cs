// ============================================================
//  HitDetectionSystem.cs
//  Combat/Systems/HitDetectionSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Detectar qué fue golpeado y enrutar al DamageSystem.
//
//  MODOS:
//  • HitScan:   Raycast/SphereCast desde el arma
//  • Projectile: Llamado por Projectile.cs al colisionar
//  • Continuous: Raycast cada frame (agua, láser)
//
//  IDENTIFICACIÓN:
//  • Tag "Player" → es un jugador
//  • Tag "Head"   → es un headshot
//  • Ambos usan PlayerAuthority para obtener el PlayerID
// ============================================================

using Combat.Events;
using Core.Events;
using UnityEngine;

namespace Combat.Systems
{
    public static class HitDetectionSystem
    {
        private const string TAG_PLAYER   = "Player";
        private const string TAG_HEAD     = "Head";

        // ── HitScan ───────────────────────────────────────────

        /// <summary>
        /// Ejecuta N raycast (uno por pellet) y procesa cada impacto.
        /// Llamado por HitScanWeapon.ExecuteShoot().
        /// </summary>
        public static void ProcessHitScan(
            WeaponConfig config,
            int          shooterID,
            Vector3      origin,
            Vector3      direction)
        {
            int pellets = Mathf.Max(1, config.PelletsPerShot);

            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplySpread(direction, config.SpreadAngle);
                FireRay(config, shooterID, origin, dir);
            }
        }

        private static void FireRay(
            WeaponConfig config,
            int          shooterID,
            Vector3      origin,
            Vector3      direction)
        {
            bool hit = Physics.Raycast(
                origin, direction,
                out RaycastHit info,
                config.MaxRange,
                config.HitLayers,
                QueryTriggerInteraction.Ignore);

            bool  hitPlayer  = hit && info.collider.CompareTag(TAG_PLAYER);
            bool  isHeadshot = hit && info.collider.CompareTag(TAG_HEAD);
            int   targetID   = hit ? ResolveID(info.collider) : -1;
            float distance   = hit ? info.distance : config.MaxRange;
            Vector3 point    = hit ? info.point    : origin + direction * config.MaxRange;
            Vector3 normal   = hit ? info.normal   : -direction;

            // Publicar hit para efectos visuales / audio
            EventBus<OnHitEvent>.Raise(new OnHitEvent
            {
                ShooterID = shooterID,
                WeaponID  = config.WeaponID,
                HitPoint  = point,
                HitNormal = normal,
                HitPlayer = hitPlayer || isHeadshot,
                TargetID  = targetID,
                Distance  = distance
            });

            if (hit && (hitPlayer || isHeadshot))
                DamageSystem.ProcessHit(config, shooterID, targetID,
                    info.distance, isHeadshot, info.point, info.normal);
        }

        // ── Proyectil ─────────────────────────────────────────

        /// <summary>
        /// Procesa la colisión de un proyectil.
        /// Llamado por Projectile.HandleImpact().
        /// </summary>
        public static void ProcessProjectileImpact(
            WeaponConfig config,
            int          shooterID,
            Collider     hitCollider,
            Vector3      hitPoint,
            Vector3      hitNormal)
        {
            if (config == null || hitCollider == null) return;

            bool  hitPlayer  = hitCollider.CompareTag(TAG_PLAYER);
            bool  isHeadshot = hitCollider.CompareTag(TAG_HEAD);
            int   targetID   = ResolveID(hitCollider);

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

            if (hitPlayer || isHeadshot)
                DamageSystem.ProcessHit(config, shooterID, targetID,
                    0f, isHeadshot, hitPoint, hitNormal);
        }

        // ── Continuo ──────────────────────────────────────────

        /// <summary>
        /// Raycast frame a frame para armas continuas.
        /// Llamado por ContinuousWeapon en su Update cuando está disparando.
        /// </summary>
        public static void ProcessContinuous(
            WeaponConfig config,
            int          shooterID,
            Vector3      origin,
            Vector3      direction,
            float        deltaTime)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit info,
                    config.ContinuousRange, config.HitLayers,
                    QueryTriggerInteraction.Ignore))
                return;

            bool hitPlayer  = info.collider.CompareTag(TAG_PLAYER);
            bool isHeadshot = info.collider.CompareTag(TAG_HEAD);
            if (!hitPlayer && !isHeadshot) return;

            int targetID = ResolveID(info.collider);
            var damage = config.DamagePerSecond * deltaTime;

             // Publicar hit para efectos visuales / audio

            // Daño es dps × deltaTime → no llama a CalculateDamage (distancia no aplica)
                 EventBus<ApplyDamageRequestEvent>.Raise(
                new ApplyDamageRequestEvent
                {
                    TargetID   = targetID,
                    AttackerID = shooterID,
                    Damage     = damage,
                    HitPoint   = direction,
                    HitNormal  = origin
                });
        }

        // ── Helpers ───────────────────────────────────────────

        private static int ResolveID(Collider col)
        {
            var auth = col.GetComponentInParent<Player.Authority.PlayerAuthority>();
            return auth != null ? auth.PlayerID : -1;
        }

        private static Vector3 ApplySpread(Vector3 dir, float angleDeg)
        {
            if (angleDeg <= 0f) return dir;
            float half = angleDeg * 0.5f;
            return Quaternion.Euler(
                Random.Range(-half, half),
                Random.Range(-half, half),
                0f) * dir;
        }
    }
}
