// ============================================================
//  FrameworkEvents.cs
//  GameModeFramework/Events/FrameworkEvents.cs
//
//  Eventos del GameMode Framework.
//  Structs puros: sin referencias a MonoBehaviour.
//  Todos los sistemas del framework se comunican solo via estos.
// ============================================================

using UnityEngine;

namespace GameMode.Framework.Events
{
    // ── Ciclo de vida del GameMode ────────────────────────────

    public struct GameInitializedEvent
    {
        public string ModeID;
    }

    public struct GameStartedEvent
    {
        public string ModeID;
        public int    Round;
        public float  Timestamp;
    }

    public struct GameEndedEvent
    {
        public string ModeID;
        public int    WinnerTeamID;    // -1 = empate
        public string Reason;          // "ScoreReached", "TimeExpired", "LastTeam"
        public float  Duration;
    }

    // ── Rondas ────────────────────────────────────────────────

    public struct RoundStartedEvent
    {
        public int   Round;
        public float Duration;         // 0 = sin límite
    }

    public struct RoundEndedEvent
    {
        public int Round;
        public int WinnerTeamID;
        public int ScoreTeamA;
        public int ScoreTeamB;
    }

    public struct RoundTimerTickEvent
    {
        public float Remaining;
        public float Total;
    }

    // ── Objetivos ─────────────────────────────────────────────

    /// <summary>
    /// Evento base que un IObjective emite al ser interactuado.
    /// Las reglas (IGameRule) reaccionan a este evento.
    /// NUNCA contiene lógica de resultado — solo qué pasó.
    /// </summary>
    public struct ObjectiveInteractedEvent
    {
        public string ObjectiveID;
        public string InteractionType; // "Pickup", "Capture", "Drop", "Return", "Contested", "Secured"
        public int    PlayerID;
        public int    PlayerTeamID;
        public int    ObjectiveOwnerTeamID;
        public Vector3 Position;
    }

    /// <summary>Una regla determinó que un objetivo fue completado (genera score).</summary>
    public struct ObjectiveScoredEvent
    {
        public string ObjectiveID;
        public string InteractionType;
        public int    ScoringTeamID;
        public int    ScoringPlayerID;
        public int    Points;
    }

    /// <summary>Una regla solicita resetear un objetivo (ej: bandera volver a base).</summary>
    public struct ObjectiveResetRequestedEvent
    {
        public string ObjectiveID;
    }

    // ── Puntuación ────────────────────────────────────────────

    public struct ScoreChangedEvent
    {
        public int    TeamID;
        public int    PlayerID;        // -1 si es score de equipo puro
        public int    Delta;           // Cuánto cambió
        public int    NewTeamTotal;
        public int    NewPlayerTotal;
        public string Reason;          // "Capture", "Kill", "Assist"
    }

    // ── Equipos ───────────────────────────────────────────────

    public struct PlayerJoinedTeamEvent
    {
        public int    PlayerID;
        public int    TeamID;
        public string TeamName;
    }

    public struct PlayerLeftTeamEvent
    {
        public int PlayerID;
        public int TeamID;
    }

    public struct TeamsBalancedEvent
    {
        public int TeamCount;
    }

    // ── Jugadores ─────────────────────────────────────────────

    public struct PlayerEliminatedEvent
    {
        public int    VictimID;
        public int    KillerID;
        public int    VictimTeamID;
        public int    KillerTeamID;
        public string WeaponID;
        public bool   WasCarryingObjective;
    }

    public struct PlayerRespawnedGameModeEvent
    {
        public int     PlayerID;
        public int     TeamID;
        public Vector3 SpawnPosition;
    }
}
