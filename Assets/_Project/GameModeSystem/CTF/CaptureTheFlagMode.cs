// ============================================================
//  CaptureTheFlagMode.cs
//  GameMode/CTF/CaptureTheFlagMode.cs
//
//  RESPONSABILIDAD ÚNICA: Orquestar el flujo completo del CTF.
//
//  Conecta: FlagController × 2, ScoreSystem, RoundSystem,
//           CaptureLogicSystem, FlagCarrier en jugadores.
//
//  FASES:
//    Idle → WarmUp → Playing → RoundEnd → PostGame
// ============================================================

using System.Collections;
using System.Collections.Generic;
using GameMode.Config;
using GameMode.Events;
using GameMode.Score;
using GameMode.Round;
using Core.Debug;
using Core.Events;
using Player.Events;
using UnityEngine;

namespace GameMode.CTF
{
    public class CaptureTheFlagMode : GameModeBase
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("CTF Config")]
        [SerializeField] private CTFConfig _config;

        [Header("Banderas (2 en escena)")]
        [SerializeField] private List<FlagController> _flags = new();

        [Header("Sistemas en escena")]
        [SerializeField] private CaptureLogicSystem _captureLogic;

        // ── Sistemas ──────────────────────────────────────────

        private ScoreSystem _score;
        private RoundSystem _rounds;

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _gameModeID  = "CTF";
            _displayName = "Capture the Flag";

            _score  = new ScoreSystem(_config);
            _rounds = new RoundSystem(_config, this);

            _rounds.OnRoundTimedOut = HandleTimeOut;

            _captureLogic?.Initialize(_score);
        }

        private void OnEnable()
        {
            EventBus<OnTeamWonRoundEvent>.Subscribe(OnTeamWonRound);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus<OnTeamWonRoundEvent>.Unsubscribe(OnTeamWonRound);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
        }

        // ── Overrides de GameModeBase ─────────────────────────

        public override void StartGame()
        {
            base.StartGame();
            _score.Reset();
            _rounds.ResetAll();
            StartCoroutine(WarmUpThenPlay());
        }

        public override void ResetGame()
        {
            base.ResetGame();
            _score.Reset();
            _rounds.ResetAll();
            ResetFlags();
        }

        protected override void Update()
        {
            // El timer de ronda lo gestiona RoundSystem internamente
        }

        // ── Flujo de fases ────────────────────────────────────

        private IEnumerator WarmUpThenPlay()
        {
            SetPhase(GameModePhase.WarmUp);
            SetAllPlayersInputEnabled(false);
            ResetFlags();

            yield return new WaitForSeconds(_config.WarmUpDuration);

            SetAllPlayersInputEnabled(true);
            SetPhase(GameModePhase.Playing);
            _rounds.StartRound();
        }

        private IEnumerator RoundEndSequence(int winnerTeamID)
        {
            SetPhase(GameModePhase.RoundEnd);
            SetAllPlayersInputEnabled(false);

            _rounds.EndRound(winnerTeamID);
            ResetFlags();

            yield return new WaitForSeconds(_config.RoundEndDuration);

            if (_rounds.HasMatchWinner(out int matchWinner))
            {
                SetPhase(GameModePhase.PostGame);
                EndGame(matchWinner);
            }
            else
            {
                _score.Reset();
                _rounds.AdvanceRound();
                StartCoroutine(WarmUpThenPlay());
            }
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnTeamWonRound(OnTeamWonRoundEvent e)
        {
            if (Phase != GameModePhase.Playing) return;
            StartCoroutine(RoundEndSequence(e.TeamID));
        }

        private void HandleTimeOut(int _)
        {
            if (Phase != GameModePhase.Playing) return;

            // Al acabar el tiempo: quien tiene más puntos gana
            int a = _score.ScoreTeamA, b = _score.ScoreTeamB;
            int winner = a > b ? 0 : b > a ? 1 : -1;
            StartCoroutine(RoundEndSequence(winner));
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (Phase != GameModePhase.Playing) return;
            // El FlagCarrierComponent ya suelta la bandera al recibir PlayerDiedEvent
        }

        // ── Helpers ───────────────────────────────────────────

        private void ResetFlags()
        {
            foreach (var f in _flags) f?.ForceReset();
        }

        private void SetAllPlayersInputEnabled(bool enabled)
        {
            var controllers = FindObjectsByType<Player.Controller.PlayerController>(
                FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (enabled) c.EnableInput();
                else         c.DisableInput();
            }
        }

        // ── API Pública ───────────────────────────────────────

        public ScoreSystem  Score  => _score;
        public RoundSystem  Rounds => _rounds;

        public void ForceStartNewRound()
        {
            if (_rounds.RoundActive) return;
            _score.Reset();
            _rounds.AdvanceRound();
            StartCoroutine(WarmUpThenPlay());
        }
    }
}
