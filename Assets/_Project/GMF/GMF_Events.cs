// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Events.cs                                  ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • GameStartedEvt       (struct)                       ║
// ║    • GameEndedEvt         (struct)                       ║
// ║    • RoundStartedEvt      (struct)                       ║
// ║    • RoundEndedEvt        (struct)                       ║
// ║    • RoundTimerTickEvt    (struct)                       ║
// ║    • ObjectiveInteractedEvt (struct)  ← CRÍTICO          ║
// ║    • ObjectiveScoredEvt   (struct)                       ║
// ║    • ObjectiveResetEvt    (struct)                       ║
// ║    • ScoreChangedEvt      (struct)                       ║
// ║    • PlayerJoinedTeamEvt  (struct)                       ║
// ║    • PlayerEliminatedEvt  (struct)                       ║
// ║    • AuthorityChangedEvt  (struct)                       ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Definir TODOS los eventos del framework como structs  ║
// ║    puros (sin referencias a MonoBehaviour).              ║
// ║    Toda comunicación entre sistemas pasa por aquí.       ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA: Ninguna. Un archivo de eventos    ║
// ║    es el patrón correcto para evitar dependencias        ║
// ║    circulares de 'using'.                                ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF
{
    // ── GameMode lifecycle ────────────────────────────────────

    /// <summary>La partida arrancó. Corre en servidor; el cliente lo recibe vía red.</summary>
    public struct GameStartedEvt
    {
        public string ModeID;
        public int    Round;
        public float  Timestamp;
    }

    /// <summary>La partida terminó. Todos los sistemas reaccionan a este evento.</summary>
    public struct GameEndedEvt
    {
        public string ModeID;
        public int    WinnerTeamID;   // -1 = empate
        public string Reason;         // "ScoreReached", "TimeExpired", "Manual"
        public float  Duration;
    }

    // ── Rondas ────────────────────────────────────────────────

    public struct RoundStartedEvt
    {
        public int   Round;
        public float Duration;  // 0 = sin límite
    }

    public struct RoundEndedEvt
    {
        public int Round;
        public int WinnerTeamID;
        public int ScoreTeamA;
        public int ScoreTeamB;
    }

    /// <summary>Publicado cada segundo durante la ronda (no cada frame).</summary>
    public struct RoundTimerTickEvt
    {
        public float Remaining;
        public float Total;
    }

    // ── Objetivos ─────────────────────────────────────────────

    /// <summary>
    /// Publicado por IObjective al detectar interacción física.
    /// Las IGameRule reaccionan a este evento para aplicar lógica.
    ///
    /// REGLA: Este evento describe QUÉ PASÓ, no qué significa.
    ///        "Pickup" = alguien tocó el objeto.
    ///        La regla decide si eso es válido o no.
    /// </summary>
    public struct ObjectiveInteractedEvt
    {
        public string ObjectiveID;
        public string Interaction;       // "Pickup","Capture","Drop","Return","Enter","Exit","Tick"
        public int    PlayerID;          // -1 si no hay jugador (timer automático)
        public int    PlayerTeamID;      // -1 si no hay jugador
        public int    ObjectiveTeamID;   // equipo dueño del objetivo
        public Vector3 Position;
    }

    /// <summary>
    /// Publicado por IGameRule cuando una interacción genera puntos.
    /// ScoreSystem escucha este evento.
    /// </summary>
    public struct ObjectiveScoredEvt
    {
        public string ObjectiveID;
        public int    ScoringTeamID;
        public int    ScoringPlayerID;
        public int    Points;
        public string Reason;
    }

    /// <summary>Publicado por IGameRule para solicitar que un objetivo vuelva a su estado base.</summary>
    public struct ObjectiveResetEvt
    {
        public string ObjectiveID;
    }

    // ── Puntuación ────────────────────────────────────────────

    public struct ScoreChangedEvt
    {
        public int    TeamID;
        public int    PlayerID;       // -1 si es score de equipo puro
        public int    Delta;
        public int    NewTeamTotal;
        public int    NewPlayerTotal;
        public string Reason;
    }

    // ── Equipos ───────────────────────────────────────────────

    public struct PlayerJoinedTeamEvt
    {
        public int    PlayerID;
        public int    TeamID;
        public string TeamName;
    }

    // ── Jugadores ─────────────────────────────────────────────

    /// <summary>
    /// Un jugador fue eliminado.
    /// WasCarryingObjective permite a reglas como FlagDropOnDeath reaccionar.
    /// </summary>
    public struct PlayerEliminatedEvt
    {
        public int    VictimID;
        public int    KillerID;
        public int    VictimTeamID;
        public int    KillerTeamID;
        public string WeaponID;
        public bool   WasCarryingObjective;
        public string CarriedObjectiveID;   // ID del objetivo que portaba
    }

    // ── Autoridad de red ──────────────────────────────────────

    public struct AuthorityChangedEvt
    {
        public bool        HasAuthority;
        public GMFNetMode  NetworkMode;
    }

    public enum GMFNetMode { Offline, Host, Client, DedicatedServer }
}
