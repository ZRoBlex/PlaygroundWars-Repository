// ============================================================
//  PlayerEvents.cs
//  PlayerSystem/Events/PlayerEvents.cs
//
//  Todos los event structs del sistema de jugador.
//  Comunicación 100% desacoplada via EventBus<T>.
//
//  REGLA: Solo tipos de valor. Sin referencias a MonoBehaviours.
//         El int playerID identifica al jugador en todos los eventos.
// ============================================================

using UnityEngine;

namespace Player.Events
{
    // ── Input ─────────────────────────────────────────────────

    /// <summary>Vector de movimiento normalizado del jugador.</summary>
    public struct PlayerMoveInputEvent
    {
        public int     PlayerID;
        public Vector2 MoveDirection;   // Normalizado [-1, 1]
        public bool    IsRunning;
    }

    /// <summary>Solicitud de salto.</summary>
    public struct PlayerJumpRequestEvent
    {
        public int PlayerID;
    }

    /// <summary>Delta de movimiento de cámara (mouse / stick derecho).</summary>
    public struct PlayerLookInputEvent
    {
        public int     PlayerID;
        public Vector2 LookDelta;       // Píxeles / unidades de stick
    }

    /// <summary>Botón de disparo presionado o soltado.</summary>
    public struct PlayerShootInputEvent
    {
        public int  PlayerID;
        public bool IsPressed;
    }

    /// <summary>Habilidad activada (0=primaria, 1=secundaria, 2=ultimate).</summary>
    public struct PlayerAbilityInputEvent
    {
        public int AbilitySlot;   // 0, 1, 2
        public int PlayerID;
    }

    /// <summary>Interacción con el entorno.</summary>
    public struct PlayerInteractInputEvent
    {
        public int PlayerID;
    }

    // ── Movimiento ────────────────────────────────────────────

    /// <summary>El jugador se movió a una nueva posición (post-validación).</summary>
    public struct PlayerMovedEvent
    {
        public int     PlayerID;
        public Vector3 NewPosition;
        public Vector3 Velocity;
    }

    /// <summary>El jugador saltó.</summary>
    public struct PlayerJumpedEvent
    {
        public int     PlayerID;
        public Vector3 Position;
    }

    /// <summary>El jugador aterrizó (tocó el suelo).</summary>
    public struct PlayerLandedEvent
    {
        public int     PlayerID;
        public float   FallDistance;
    }

    /// <summary>Solicitud de movimiento del cliente al servidor (multiplayer).</summary>
    public struct PlayerMoveRequestEvent
    {
        public int     PlayerID;
        public Vector2 InputDirection;
        public bool    JumpRequested;
        public bool    IsRunning;
        public float   Timestamp;
    }

    // ── Salud ─────────────────────────────────────────────────

    /// <summary>El jugador recibió daño (disparado por la autoridad).</summary>
    public struct PlayerDamagedEvent
    {
        public int     PlayerID;
        public int     AttackerID;
        public float   Amount;
        public float   RemainingHealth;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
    }

    /// <summary>El jugador fue curado.</summary>
    public struct PlayerHealedEvent
    {
        public int   PlayerID;
        public float Amount;
        public float CurrentHealth;
    }

    /// <summary>El jugador murió.</summary>
    public struct PlayerDiedEvent
    {
        public int     PlayerID;
        public int     KillerID;
        public Vector3 DeathPosition;
    }

    /// <summary>El jugador recibió un efecto de estado (slow, freeze, etc.).</summary>
    public struct PlayerStatusEffectEvent
    {
        public int    PlayerID;
        public string EffectName;
        public float  Duration;
        public bool   IsApplied;  // true = aplicar, false = remover
    }

    // ── Respawn ───────────────────────────────────────────────

    /// <summary>El jugador va a hacer respawn (antes de aparecer).</summary>
    public struct PlayerPreRespawnEvent
    {
        public int     PlayerID;
        public Vector3 SpawnPosition;
        public float   Delay;
    }

    /// <summary>El jugador apareció en el mundo.</summary>
    public struct PlayerRespawnedEvent
    {
        public int     PlayerID;
        public Vector3 SpawnPosition;
    }

    // ── Estado del jugador ────────────────────────────────────

    /// <summary>El jugador está listo (conectado + inicializado).</summary>
    public struct PlayerReadyEvent
    {
        public int  PlayerID;
        public bool IsLocalPlayer;
    }

    /// <summary>El jugador se desconectó o abandonó.</summary>
    public struct PlayerLeftEvent
    {
        public int PlayerID;
    }

    // ── Solicitudes de daño (cliente → servidor) ──────────────

    // /// <summary>
    // /// Solicitud de aplicar daño. Solo la autoridad la procesa.
    // /// El cliente la envía; el servidor la valida y aplica.
    // /// </summary>
    // public struct ApplyDamageRequestEvent
    // {
    //     public int     TargetPlayerID;
    //     public int     SourcePlayerID;
    //     public float   Amount;
    //     public Vector3 HitPoint;
    //     public Vector3 HitNormal;
    // }
}
