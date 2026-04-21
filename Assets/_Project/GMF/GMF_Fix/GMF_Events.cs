// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Events.cs  (REEMPLAZA el anterior)         ║
// ║                                                          ║
// ║  CAMBIO:                                                 ║
// ║    + CarriedObjectiveID en ObjectiveInteractedEvt        ║
// ║      CaptureZone lo rellena con el ID de la bandera      ║
// ║      que portaba el jugador.                             ║
// ║      ObjectiveCaptureRule lo usa para emitir Reset.      ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF
{
    public struct GameStartedEvt   { public string ModeID; public int Round; public float Timestamp; }
    public struct GameEndedEvt     { public string ModeID; public int WinnerTeamID; public string Reason; public float Duration; }
    public struct RoundStartedEvt  { public int Round; public float Duration; }
    public struct RoundEndedEvt    { public int Round; public int WinnerTeamID; public int ScoreTeamA; public int ScoreTeamB; }
    public struct RoundTimerTickEvt { public float Remaining; public float Total; }

    /// <summary>
    /// Emitido por IObjective al detectar interacción física.
    /// Las IGameRule reaccionan a este evento.
    ///
    /// CarriedObjectiveID: si el jugador portaba un objetivo (bandera),
    /// se rellena con su ID. ObjectiveCaptureRule lo usa para resetear la bandera.
    /// </summary>
    public struct ObjectiveInteractedEvt
    {
        public string  ObjectiveID;          // ID del objetivo que emite (zona, flag, etc.)
        public string  Interaction;          // "Pickup","Capture","Drop","Return","Enter","Exit","Tick"
        public int     PlayerID;
        public int     PlayerTeamID;
        public int     ObjectiveTeamID;
        public Vector3 Position;
        public string  CarriedObjectiveID;   // ← NUEVO: ID de la bandera portada (si aplica)
    }

    public struct ObjectiveScoredEvt { public string ObjectiveID; public int ScoringTeamID; public int ScoringPlayerID; public int Points; public string Reason; }
    public struct ObjectiveResetEvt  { public string ObjectiveID; }

    public struct ScoreChangedEvt
    {
        public int    TeamID;
        public int    PlayerID;
        public int    Delta;
        public int    NewTeamTotal;
        public int    NewPlayerTotal;
        public string Reason;
    }

    public struct PlayerJoinedTeamEvt { public int PlayerID; public int TeamID; public string TeamName; }

    public struct PlayerEliminatedEvt
    {
        public int    VictimID;
        public int    KillerID;
        public int    VictimTeamID;
        public int    KillerTeamID;
        public string WeaponID;
        public bool   WasCarryingObjective;
        public string CarriedObjectiveID;
    }

    public struct AuthorityChangedEvt { public bool HasAuthority; public GMFNetMode NetworkMode; }
    public enum  GMFNetMode { Offline, Host, Client, DedicatedServer }
}
