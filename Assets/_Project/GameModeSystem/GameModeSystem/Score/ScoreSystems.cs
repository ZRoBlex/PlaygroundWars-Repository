// ============================================================
//  ScoreSystem.cs
//  GameMode/Score/ScoreSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Registrar y validar puntuación por equipo.
//  Sin lógica de UI. Solo datos y eventos.
// ============================================================

using Core.Debug;
using Core.Events;
using GameMode.Config;
using GameMode.Events;

namespace GameMode.Score
{
    public class ScoreSystem
    {
        // ── Estado ────────────────────────────────────────────

        public int ScoreTeamA  { get; private set; }
        public int ScoreTeamB  { get; private set; }

        // ── Config ────────────────────────────────────────────

        private readonly CTFConfig _config;

        // ── Constructor ───────────────────────────────────────

        public ScoreSystem(CTFConfig config)
        {
            _config = config;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Añade puntos a un equipo y publica el evento.
        /// Retorna true si el equipo alcanzó ScoreToWin.
        /// </summary>
        public bool AddScore(int teamID, int amount = 1)
        {
            if (teamID == 0) ScoreTeamA += amount;
            else             ScoreTeamB += amount;

            int newScore = teamID == 0 ? ScoreTeamA : ScoreTeamB;

            CoreLogger.LogSystem("ScoreSystem",
                $"Team {teamID}: {newScore}/{_config.ScoreToWin} (A={ScoreTeamA} B={ScoreTeamB})");

            EventBus<OnScoreChangedEvent>.Raise(new OnScoreChangedEvent
            {
                TeamID     = teamID,
                NewScore   = newScore,
                ScoreToWin = _config.ScoreToWin
            });

            return newScore >= _config.ScoreToWin;
        }

        public bool HasTeamWon(out int winnerID)
        {
            if (ScoreTeamA >= _config.ScoreToWin) { winnerID = 0; return true; }
            if (ScoreTeamB >= _config.ScoreToWin) { winnerID = 1; return true; }
            winnerID = -1;
            return false;
        }

        public int GetScore(int teamID) => teamID == 0 ? ScoreTeamA : ScoreTeamB;

        public void Reset()
        {
            ScoreTeamA = 0;
            ScoreTeamB = 0;
        }
    }
}