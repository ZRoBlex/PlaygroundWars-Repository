// // ============================================================
// //  GameEvents_Additions.cs
// //  Core/Events/GameEvents_Additions.cs
// //
// //  ⚠️  PARCHE — Añadir al archivo GameEvents.cs existente
// //  (o importar este archivo en el namespace Core.Events)
// //
// //  CORRIGE: Error de compilación "ApplyDamageRequestEvent no existe"
// //
// //  ARQUITECTURA DE AUTORIDAD:
// //  ┌──────────────────────────────────────────────────────────┐
// //  │  CLIENTE                                                 │
// //  │   PlayerInput → ShootInputEvent → HitDetectionSystem    │
// //  │   HitDetectionSystem → ApplyDamageRequestEvent (emite)  │
// //  │                                                          │
// //  │  SERVIDOR (Host o Dedicated)                             │
// //  │   ServerDamageProcessor → escucha ApplyDamageRequest    │
// //  │   → valida → aplica daño real → PlayerDamagedEvent      │
// //  └──────────────────────────────────────────────────────────┘
// //
// //  REGLA: El cliente NUNCA aplica daño. Solo envía REQUESTS.
// //         El servidor es la única autoridad del daño.
// // ============================================================

// using UnityEngine;

// namespace Core.Events
// {
//     // ── CORRECCIÓN PRINCIPAL ──────────────────────────────────

//     /// <summary>
//     /// REQUEST de daño enviada por el cliente (o la lógica local en offline).
//     /// NO aplica daño directamente.
//     /// Solo la autoridad (servidor/host) la procesa y aplica.
//     ///
//     /// FLUJO CORRECTO:
//     ///   HitDetection → emite ApplyDamageRequestEvent
//     ///   ServerDamageProcessor → valida → emite DamageAppliedEvent
//     ///   PlayerHealth → escucha DamageAppliedEvent → aplica HP
//     /// </summary>
//     public struct ApplyDamageRequestEvent
//     {
//         public int     AttackerID;      // Quien dispara
//         public int     TargetID;        // Quien recibe
//         public float   Damage;          // Daño base solicitado
//         public Vector3 HitPoint;        // Punto de impacto
//         public Vector3 HitNormal;       // Normal de la superficie
//         public string  WeaponID;        // ID del arma para modificadores
//         public bool    IsHeadshot;      // Para multiplicador
//         public float   Distance;        // Para falloff
//     }

//     /// <summary>
//     /// Daño VALIDADO y APLICADO por el servidor.
//     /// Solo se emite desde la autoridad — nunca desde el cliente.
//     /// PlayerHealth escucha este evento para reducir HP.
//     /// </summary>
//     public struct DamageAppliedEvent
//     {
//         public int     AttackerID;
//         public int     TargetID;
//         public float   FinalDamage;     // Daño real tras modificadores del servidor
//         public float   RemainingHealth;
//         public bool    WasLethal;
//         public Vector3 HitPoint;
//         public string  WeaponID;
//     }

//     /// <summary>
//     /// El servidor rechazó la solicitud de daño (anti-cheat, estado inválido).
//     /// </summary>
//     public struct DamageRequestRejectedEvent
//     {
//         public int    AttackerID;
//         public int    TargetID;
//         public string Reason;   // "TargetDead", "AntiCheat", "OutOfRange"
//     }

//     // ── Eventos de servidor ───────────────────────────────────

//     /// <summary>El contexto de red del jugador cambió (offline ↔ host ↔ client).</summary>
//     public struct NetworkContextChangedEvent
//     {
//         public int         PlayerID;
//         public NetworkRole Role;
//     }

//     public enum NetworkRole
//     {
//         Offline,          // Todo local, sin red
//         Host,             // Este jugador es host y tiene autoridad
//         Client,           // Este jugador es cliente, sin autoridad de gameplay
//         DedicatedServer   // Servidor sin jugador local
//     }

//     // ── Eventos de movimiento (Source of Truth) ───────────────

//     /// <summary>
//     /// Estado de movimiento VALIDADO por física real.
//     /// Publicado por MovementStateProvider cada FixedUpdate.
//     ///
//     /// CRÍTICO: IsGrounded aquí es el resultado de un SphereCast REAL.
//     /// NO es un bool que se setea en código. Eso causaba el infinite jump.
//     /// </summary>
//     public struct MovementStateValidatedEvent
//     {
//         public int     PlayerID;
//         public bool    IsGroundedReal;      // SphereCast real — no bool manual
//         public bool    IsCrouchingReal;     // CharacterController.height < standHeight
//         public Vector3 Velocity;
//         public float   HorizontalSpeed;
//     }
// }
