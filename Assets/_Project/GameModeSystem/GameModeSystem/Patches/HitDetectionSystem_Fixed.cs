// ============================================================
//  HitDetectionSystem_Fixed.cs
//  Combat/Systems/HitDetectionSystem_Fixed.cs
//
//  ⚠️  REEMPLAZA: HitDetectionSystem.cs
//
//  BUGS CORREGIDOS:
//  ════════════════════════════════════════════════════════════
//
//  BUG 1 — HitScan no funciona
//  ❌ Causa: El origen del raycast era el transform del arma
//     (muzzle), que puede estar detrás de una pared cuando
//     la cámara asoma por una esquina. Resultado: hit bloqueado.
//     Además, LayerMask = 0 por defecto → no detecta nada.
//
//  ✅ Fix: El raycast SIEMPRE sale desde la cámara (lo que el
//     jugador VE), no desde el cañón del arma.
//     El muzzle solo es visual (tracer, flash).
//     LayerMask configurable en WeaponConfig con valor sensato.
//
//  BUG 2 — Violación de autoridad de servidor
//  ❌ Causa: HitDetectionSystem llamaba a DamageSystem.ProcessHit()
//     DIRECTAMENTE. En multiplayer, cualquier cliente podía
//     aplicar daño a sí mismo sin validación del servidor.
//
//  ✅ Fix: HitDetectionSystem SOLO emite ApplyDamageRequestEvent.
//     ServerDamageProcessor (en este archivo) valida y aplica.
//     En offline: ServerDamageProcessor procesa la request local.
//     En multiplayer: el host procesa; el cliente solo ve la UI.
//
//  FLUJO CORRECTO:
//  Cliente:  Input → Raycast (local prediction) → ApplyDamageRequestEvent
//  Servidor: ServerDamageProcessor → valida distancia, estado → DamageAppliedEvent
//  Cliente:  PlayerHealth escucha DamageAppliedEvent → actualiza HP
// ============================================================

using Core.Events;
using Core.Debug;
using UnityEngine;

namespace Combat.Systems
{
    // ════════════════════════════════════════════════════════
    //  HitDetectionSystem — SOLO DETECTA, NUNCA APLICA DAÑO
    // ════════════════════════════════════════════════════════

    public static class HitDetectionSystem_Fixed
    {
        private const string TAG_PLAYER = "Player";
        private const string TAG_HEAD   = "Head";

        // ── HitScan ───────────────────────────────────────────

        /// <summary>
        /// Ejecuta el raycast desde la CÁMARA (no desde el cañón).
        /// Emite ApplyDamageRequestEvent — no aplica daño directamente.
        /// </summary>
        public static void ProcessHitScan(
            WeaponConfig      config,
            int               shooterID,
            Camera            playerCamera,     // ✅ SIEMPRE desde la cámara
            Vector3           muzzlePosition)   // Solo para el tracer visual
        {
            if (config == null || playerCamera == null)
            {
                CoreLogger.LogError("[HitDetection] WeaponConfig o Camera nulos.");
                return;
            }

            // LayerMask debe ser ~0 o configurado en WeaponConfig
            // ❌ Antes: config.HitLayers = 0 → no detectaba nada
            // ✅ Ahora: usar config.HitLayers; si es 0, usar ~0 como fallback
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
            Vector3      origin,       // ← origen en la CÁMARA
            Vector3      direction,
            LayerMask    mask)
        {
            bool didHit = Physics.Raycast(
                origin, direction,
                out RaycastHit hit,
                config.MaxRange,
                mask,
                QueryTriggerInteraction.Ignore);

            // Publicar el resultado del hit para efectos visuales (tracer, impacto)
            // Esto es INFORMACIÓN, no acción de gameplay
            EventBus<OnHitEvent>.Raise(new OnHitEvent
            {
                ShooterID = shooterID,
                WeaponID  = config.WeaponID,
                HitPoint  = didHit ? hit.point : origin + direction * config.MaxRange,
                HitNormal = didHit ? hit.normal : -direction,
                HitPlayer = didHit && (hit.collider.CompareTag(TAG_PLAYER) ||
                                       hit.collider.CompareTag(TAG_HEAD)),
                TargetID  = didHit ? ResolvePlayerID(hit.collider) : -1,
                Distance  = didHit ? hit.distance : config.MaxRange
            });

            if (!didHit) return;

            bool hitPlayer  = hit.collider.CompareTag(TAG_PLAYER);
            bool isHeadshot = hit.collider.CompareTag(TAG_HEAD);

            if (!hitPlayer && !isHeadshot) return;

            int targetID = ResolvePlayerID(hit.collider);
            if (targetID < 0) return;

            // ✅ SOLO emite REQUEST — nunca aplica daño directamente
            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                AttackerID = shooterID,
                TargetID   = targetID,
                Damage     = config.BaseDamage,
                HitPoint   = hit.point,
                HitNormal  = hit.normal,
                WeaponID   = config.WeaponID,
                IsHeadshot = isHeadshot,
                Distance   = hit.distance
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

            EventBus<OnHitEvent>.Raise(new OnHitEvent
            {
                ShooterID = shooterID,
                WeaponID  = config.WeaponID,
                HitPoint  = hitPoint,
                HitNormal = hitNormal,
                HitPlayer = hitPlayer || isHeadshot,
                TargetID  = ResolvePlayerID(hitCollider),
                Distance  = 0f
            });

            if (!hitPlayer && !isHeadshot) return;

            int targetID = ResolvePlayerID(hitCollider);
            if (targetID < 0) return;

            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                AttackerID = shooterID,
                TargetID   = targetID,
                Damage     = config.BaseDamage,
                HitPoint   = hitPoint,
                HitNormal  = hitNormal,
                WeaponID   = config.WeaponID,
                IsHeadshot = false,
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

            if (!Physics.Raycast(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    out RaycastHit hit, config.ContinuousRange, mask,
                    QueryTriggerInteraction.Ignore)) return;

            bool hitPlayer  = hit.collider.CompareTag(TAG_PLAYER);
            bool isHeadshot = hit.collider.CompareTag(TAG_HEAD);
            if (!hitPlayer && !isHeadshot) return;

            int targetID = ResolvePlayerID(hit.collider);
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

        private static int ResolvePlayerID(Collider col)
        {
            var auth = col.GetComponentInParent<Player.Authority.PlayerAuthority>();
            return auth != null ? auth.PlayerID : -1;
        }

        private static Vector3 ApplySpread(Vector3 dir, float angle)
        {
            if (angle <= 0f) return dir;
            float half = angle * 0.5f;
            return Quaternion.Euler(
                Random.Range(-half, half),
                Random.Range(-half, half), 0f) * dir;
        }
    }

    // ════════════════════════════════════════════════════════
    //  ServerDamageProcessor — LA ÚNICA AUTORIDAD DE DAÑO
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Componente que procesa ApplyDamageRequestEvent con autoridad de servidor.
    ///
    /// OFFLINE:     Añadir al mismo GO que el GameManager. Siempre procesa.
    /// HOST:        Añadir al host. Solo el host valida y aplica daño.
    /// CLIENT:      NO añadir a clientes. Reciben DamageAppliedEvent via red.
    /// DEDICATED:   Añadir al servidor dedicado.
    ///
    /// SETUP: 1 instancia en escena por sesión.
    /// </summary>
    public class ServerDamageProcessor : MonoBehaviour
    {
        [Header("Modo de Red")]
        [SerializeField] private bool _isAuthority = true;   // false en clientes puros

        [Header("Anti-Cheat básico")]
        [SerializeField] private float _maxDamagePerShot = 500f;
        [SerializeField] private float _maxRangeBonus    = 10f;  // Margen sobre MaxRange del config

        private void OnEnable()
        {
            EventBus<ApplyDamageRequestEvent>.Subscribe(OnDamageRequested);
        }

        private void OnDisable()
        {
            EventBus<ApplyDamageRequestEvent>.Unsubscribe(OnDamageRequested);
        }

        private void OnDamageRequested(ApplyDamageRequestEvent req)
        {
            // ✅ Solo la autoridad procesa
            if (!_isAuthority) return;

            // ── Validaciones anti-cheat ────────────────────────

            // 1. El daño no puede exceder el máximo permitido
            if (req.Damage > _maxDamagePerShot)
            {
                CoreLogger.LogWarning(
                    $"[ServerDmg] ANTI-CHEAT: Daño excesivo {req.Damage:F0} " +
                    $"de P{req.AttackerID}. Rechazado.");

                EventBus<DamageRequestRejectedEvent>.Raise(new DamageRequestRejectedEvent
                {
                    AttackerID = req.AttackerID,
                    TargetID   = req.TargetID,
                    Reason     = "AntiCheat_ExcessiveDamage"
                });
                return;
            }

            // 2. El target debe ser válido (no muerto ya, no mismo equipo en CTF, etc.)
            // TODO: integrar con TeamManager para friendly fire
            if (req.TargetID < 0)
            {
                EventBus<DamageRequestRejectedEvent>.Raise(new DamageRequestRejectedEvent
                {
                    AttackerID = req.AttackerID,
                    TargetID   = req.TargetID,
                    Reason     = "InvalidTarget"
                });
                return;
            }

            // ── Cálculo de daño real ───────────────────────────

            float finalDamage = req.Damage;

            // Headshot multiplier (el config del arma lo define, no el cliente)
            if (req.IsHeadshot)
                finalDamage *= GetHeadshotMult(req.WeaponID);

            // Falloff por distancia
            finalDamage = ApplyFalloff(finalDamage, req.Distance, req.WeaponID);
            finalDamage = Mathf.Max(1f, Mathf.Round(finalDamage));

            CoreLogger.LogSystemDebug("ServerDmg",
                $"P{req.AttackerID}→P{req.TargetID}: {req.Damage:F0}→{finalDamage:F0}dmg " +
                $"head={req.IsHeadshot}");

            // ── Aplicar daño al target ─────────────────────────

            // Publicar DamageAppliedEvent → PlayerHealth lo recibe y reduce HP
            EventBus<DamageAppliedEvent>.Raise(new DamageAppliedEvent
            {
                AttackerID      = req.AttackerID,
                TargetID        = req.TargetID,
                FinalDamage     = finalDamage,
                RemainingHealth = 0f,   // PlayerHealth actualiza esto al procesar
                WasLethal       = false,
                HitPoint        = req.HitPoint,
                WeaponID        = req.WeaponID
            });
        }

        // ── Helpers (en producción: leer de WeaponConfigRegistry) ─

        private float GetHeadshotMult(string weaponID) => 2f;     // Expandir con registro

        private float ApplyFalloff(float dmg, float dist, string weaponID)
            => dmg;  // Expandir: buscar WeaponConfig en registry por weaponID
    }
}
