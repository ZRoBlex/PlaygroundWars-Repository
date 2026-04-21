// ============================================================
//  DamageSystem.cs
//  Combat/Systems/DamageSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Calcular y distribuir daño.
//
//  ARQUITECTURA:
//  DamageSystem CALCULA y emite eventos.
//  PlayerHealth (u otro receptor) APLICA el daño al recibir
//  el evento ApplyDamageRequestEvent (del Core/Player system).
//
//  Separación: el sistema de combate nunca toca directamente
//  la vida del jugador — solo habla a través de eventos.
// ============================================================

using Combat.Events;
using Core.Debug;
using Core.Events;
using Player.Events;
using UnityEngine;

namespace Combat.Systems
{
    public static class DamageSystem
    {
        // ── API Principal ─────────────────────────────────────

        /// <summary>
        /// Procesa un hit: calcula el daño con modificadores y
        /// emite OnDamageDealtEvent + ApplyDamageRequestEvent.
        /// </summary>
        public static void ProcessHit(
            WeaponConfig config,
            int          sourceID,
            int          targetID,
            float        distance,
            bool         isHeadshot,
            Vector3      hitPoint,
            Vector3      hitNormal)
        {
            if (config == null)
            {
                CoreLogger.LogError("[DamageSystem] WeaponConfig nulo.");
                return;
            }

            float rawDamage   = config.CalculateDamage(distance, isHeadshot);
            float finalDamage = Mathf.Max(1f, Mathf.Round(rawDamage));

            CoreLogger.LogSystemDebug("DamageSystem",
                $"[P{sourceID}→P{targetID}] {rawDamage:F1}→{finalDamage:F0}dmg " +
                $"dist={distance:F1}m head={isHeadshot}");

            // Evento para UI, stats, kill feed
            EventBus<OnDamageDealtEvent>.Raise(new OnDamageDealtEvent
            {
                SourceID   = sourceID,
                TargetID   = targetID,
                Amount     = finalDamage,
                IsCritical = isHeadshot,
                WeaponID   = config.WeaponID,
                HitPoint   = hitPoint
            });

            // Solicitud a PlayerHealth (Player system la procesa si tiene autoridad)
            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                TargetID = targetID,
                AttackerID = sourceID,
                Damage         = finalDamage,
                HitPoint       = hitPoint,
                HitNormal      = hitNormal
            });
        }

        /// <summary>
        /// Daño de área: aplica a todos los targets con cálculo por distancia.
        /// </summary>
        public static void ProcessAreaDamage(
            WeaponConfig config,
            int          sourceID,
            Collider[]   targets,
            Vector3      explosionOrigin,
            float        radius)
        {
            if (config == null || targets == null) return;

            foreach (var col in targets)
            {
                if (col == null) continue;

                var authority = col.GetComponentInParent<Player.Authority.PlayerAuthority>();
                if (authority == null) continue;

                float dist = Vector3.Distance(explosionOrigin, col.transform.position);
                if (dist > radius) continue;

                ProcessHit(config, sourceID, authority.PlayerID,
                    dist, false,
                    col.ClosestPoint(explosionOrigin),
                    (col.transform.position - explosionOrigin).normalized);
            }
        }
    }
}
