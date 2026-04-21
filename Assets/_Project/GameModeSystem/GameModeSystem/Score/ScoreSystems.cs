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

// ============================================================
//  RoundSystem.cs
//  GameMode/Round/RoundSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar rondas (número, timer, victoria).
//  Usa coroutines para el timer — sin Update innecesario.
// ============================================================

namespace GameMode.Round
{
    using System.Collections;
    using Core.Debug;
    using Core.Events;
    using GameMode.Config;
    using GameMode.Events;
    using UnityEngine;

    public class RoundSystem
    {
        // ── Estado ────────────────────────────────────────────

        public int   CurrentRound   { get; private set; } = 1;
        public int   WinsTeamA      { get; private set; }
        public int   WinsTeamB      { get; private set; }
        public float RoundTimer     { get; private set; }
        public bool  RoundActive    { get; private set; }

        // ── Config ────────────────────────────────────────────

        private readonly CTFConfig   _config;
        private readonly MonoBehaviour _runner;

        private Coroutine _timerCoroutine;

        public System.Action<int> OnRoundTimedOut;  // teamID=-1 = empate por tiempo

        // ── Constructor ───────────────────────────────────────

        public RoundSystem(CTFConfig config, MonoBehaviour coroutineRunner)
        {
            _config = config;
            _runner = coroutineRunner;
        }

        // ── API Pública ───────────────────────────────────────

        public void StartRound()
        {
            RoundActive = true;
            RoundTimer  = _config.RoundDuration;

            CoreLogger.LogSystem("RoundSystem", $"Ronda {CurrentRound} iniciada.");

            EventBus<OnRoundStartedEvent>.Raise(new OnRoundStartedEvent
            {
                RoundNumber = CurrentRound,
                Duration    = _config.RoundDuration
            });

            if (_config.RoundDuration > 0f)
            {
                if (_timerCoroutine != null) _runner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = _runner.StartCoroutine(TimerRoutine());
            }
        }

        public void EndRound(int winnerTeamID)
        {
            if (!RoundActive) return;
            RoundActive = false;

            if (_timerCoroutine != null)
            {
                _runner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (winnerTeamID == 0)      WinsTeamA++;
            else if (winnerTeamID == 1) WinsTeamB++;

            CoreLogger.LogSystem("RoundSystem",
                $"Ronda {CurrentRound} terminada. Winner={winnerTeamID} " +
                $"Wins: A={WinsTeamA} B={WinsTeamB}");

            EventBus<OnRoundEndedEvent>.Raise(new OnRoundEndedEvent
            {
                RoundNumber  = CurrentRound,
                WinnerTeamID = winnerTeamID,
                ScoreTeamA   = WinsTeamA,
                ScoreTeamB   = WinsTeamB
            });
        }

        public void AdvanceRound()
        {
            CurrentRound++;
        }

        public bool HasMatchWinner(out int winnerID)
        {
            if (WinsTeamA >= _config.RoundsToWin) { winnerID = 0; return true; }
            if (WinsTeamB >= _config.RoundsToWin) { winnerID = 1; return true; }
            winnerID = -1;
            return false;
        }

        public void ResetAll()
        {
            CurrentRound = 1;
            WinsTeamA    = 0;
            WinsTeamB    = 0;
            RoundActive  = false;
        }

        // ── Timer coroutine ───────────────────────────────────

        private IEnumerator TimerRoutine()
        {
            while (RoundTimer > 0f)
            {
                yield return null;
                RoundTimer -= Time.deltaTime;

                EventBus<OnRoundTimerUpdatedEvent>.Raise(new OnRoundTimerUpdatedEvent
                {
                    Remaining = Mathf.Max(0f, RoundTimer),
                    Total     = _config.RoundDuration
                });
            }

            CoreLogger.LogSystem("RoundSystem", "Timer de ronda agotado.");
            OnRoundTimedOut?.Invoke(-1);
        }
    }
}
