// ============================================================
//  ApplyDamageRequestEvent_Fix.cs
//  Core/Events/ApplyDamageRequestEvent_Fix.cs
//
//  ════════════════════════════════════════════════════════════
//  ⚠️  INSTRUCCIONES DE APLICACIÓN
//  ════════════════════════════════════════════════════════════
//
//  ERRORES QUE CORRIGE:
//  1. "ApplyDamageRequestEvent is ambiguous between
//      Core.Events.ApplyDamageRequestEvent and
//      Player.Events.ApplyDamageRequestEvent"
//
//  2. "does not contain a definition for 'TargetPlayerID'"
//     (campos renombrados en la versión nueva)
//
//  PASOS:
//  ─────────────────────────────────────────────────────────
//  PASO 1 ─ Añadir ESTE archivo a Assets/_Project/Core/Events/
//
//  PASO 2 ─ Abrir Player/Events/PlayerEvents.cs
//            Eliminar el struct ApplyDamageRequestEvent
//            que está definido allí (buscar "ApplyDamageRequest").
//
//  PASO 3 ─ En cualquier archivo que use ApplyDamageRequestEvent
//            con los campos viejos, hacer estos reemplazos:
//
//            TargetPlayerID  →  TargetID
//            SourcePlayerID  →  AttackerID
//            Amount          →  Damage
//
//  PASO 4 ─ Eliminar GameEvents_Additions.cs si lo añadiste,
//            ya que este archivo lo reemplaza completamente.
//  ════════════════════════════════════════════════════════════

using UnityEngine;

namespace Core.Events
{
    // ── EVENTO CANÓNICO ÚNICO ─────────────────────────────────
    // Una sola definición para todo el proyecto.
    // Contiene TODOS los campos que usan los distintos sistemas.

    /// <summary>
    /// REQUEST de daño enviada por el cliente o la lógica de detección.
    /// NUNCA aplica daño directamente.
    /// Solo la autoridad (servidor/host/offline) la procesa.
    ///
    /// Campos unificados:
    ///   - AttackerID / TargetID      → para autoridad y UI
    ///   - Damage                     → daño base solicitado
    ///   - HitPoint / HitNormal       → para efectos visuales
    ///   - WeaponID / IsHeadshot / Distance → para modificadores del servidor
    /// </summary>
    public struct ApplyDamageRequestEvent
    {
        // ── Identidad ─────────────────────────────
        public int     AttackerID;       // Quien dispara (era: SourcePlayerID)
        public int     TargetID;         // Quien recibe  (era: TargetPlayerID)

        // ── Daño ──────────────────────────────────
        public float   Damage;           // Daño base solicitado (era: Amount)

        // ── Posición ──────────────────────────────
        public Vector3 HitPoint;
        public Vector3 HitNormal;

        // ── Modificadores (opcionales) ────────────
        public string  WeaponID;         // Para lookup de config en el servidor
        public bool    IsHeadshot;       // Para multiplicador de headshot
        public float   Distance;         // Para falloff de distancia
    }

    // ── Daño validado y aplicado por el servidor ──────────────

    /// <summary>
    /// Publicado SOLO por la autoridad (ServerDamageProcessor).
    /// PlayerHealth escucha este evento — nunca la Request.
    /// </summary>
    public struct DamageAppliedEvent
    {
        public int     AttackerID;
        public int     TargetID;
        public float   FinalDamage;
        public float   RemainingHealth;
        public bool    WasLethal;
        public Vector3 HitPoint;
        public string  WeaponID;
    }

    /// <summary>Request rechazada por el servidor (anti-cheat, estado inválido).</summary>
    public struct DamageRequestRejectedEvent
    {
        public int    AttackerID;
        public int    TargetID;
        public string Reason;
    }

    // ── Estado de movimiento validado ────────────────────────

    /// <summary>
    /// Estado de movimiento REAL publicado por MovementStateProvider.
    /// IsGroundedReal = SphereCast físico, nunca bool manual.
    /// </summary>
    public struct MovementStateValidatedEvent
    {
        public int     PlayerID;
        public bool    IsGroundedReal;
        public bool    IsCrouchingReal;
        public Vector3 Velocity;
        public float   HorizontalSpeed;
    }
}
