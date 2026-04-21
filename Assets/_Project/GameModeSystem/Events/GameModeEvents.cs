// ============================================================
//  GameModeEvents.cs
//  GameMode/Events/GameModeEvents.cs
//
//  Todos los event structs del sistema de modos de juego.
//  Sin referencias a MonoBehaviours — solo tipos de valor.
// ============================================================

using UnityEngine;

namespace GameMode.Events
{
    // ── Ciclo de vida del GameMode ────────────────────────────

    public struct OnGameStartedEvent
    {
        public string GameModeID;
        public float  Timestamp;
    }

    public struct OnGameEndedEvent
    {
        public string GameModeID;
        public int    WinnerTeamID;   // -1 = empate
        public float  Duration;
    }

    public struct OnGameModeChangedEvent
    {
        public string PreviousModeID;
        public string NewModeID;
    }

    // ── Rondas ────────────────────────────────────────────────

    public struct OnRoundStartedEvent
    {
        public int   RoundNumber;
        public float Duration;
    }

    public struct OnRoundEndedEvent
    {
        public int RoundNumber;
        public int WinnerTeamID;
        public int ScoreTeamA;
        public int ScoreTeamB;
    }

    public struct OnRoundTimerUpdatedEvent
    {
        public float Remaining;
        public float Total;
    }

    // ── Puntuación ────────────────────────────────────────────

    public struct OnScoreChangedEvent
    {
        public int TeamID;
        public int NewScore;
        public int ScoreToWin;
    }

    public struct OnTeamWonRoundEvent
    {
        public int TeamID;
        public int Score;
        public int Round;
    }

    // ── Bandera (CTF) ─────────────────────────────────────────

    public struct OnFlagPickedEvent
    {
        public int     CarrierID;
        public int     FlagTeamID;     // Equipo dueño de la bandera
        public Vector3 PickupPosition;
    }

    public struct OnFlagDroppedEvent
    {
        public int     CarrierID;
        public int     FlagTeamID;
        public Vector3 DropPosition;
    }

    public struct OnFlagReturnedEvent
    {
        public int     FlagTeamID;
        public int     ReturnedByID;   // -1 = retorno automático por timer
        public Vector3 BasePosition;
    }

    public struct OnFlagCapturedEvent
    {
        public int     CapturingPlayerID;
        public int     CapturingTeamID;
        public int     FlagTeamID;
        public Vector3 CapturePosition;
    }

    // ── Fase del GameMode ─────────────────────────────────────

    public enum GameModePhase { Idle, WarmUp, Playing, RoundEnd, PostGame }

    public struct OnGameModePhaseChangedEvent
    {
        public GameModePhase Previous;
        public GameModePhase Current;
    }
}
