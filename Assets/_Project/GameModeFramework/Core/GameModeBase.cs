// ============================================================
//  GameModeBase.cs
//  GameModeFramework/Core/GameModeBase.cs
//
//  RESPONSABILIDAD ÚNICA: Orquestar el ciclo de vida del modo.
//
//  GameModeBase NO contiene reglas de juego.
//  Inicializa el contexto, el RuleEngine y el WinConditionEvaluator,
//  y reacciona cuando el evaluador detecta un ganador.
// ============================================================

using System.Collections;
using GameMode.Framework.Config;
using GameMode.Framework.Events;
using GameMode.Framework.Rules;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GameMode.Framework
{
    [DisallowMultipleComponent]
    public class GameModeBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Definición del modo")]
        [SerializeField] private GameModeDefinitionSO _definition;

        [Header("Autoridad")]
        [Tooltip("Solo el servidor/host ejecuta lógica crítica.")]
        [SerializeField] private bool _isAuthority = true;

        // ── Subsistemas internos ──────────────────────────────

        private GameModeContext       _ctx;
        private RuleEngine            _ruleEngine;
        private WinConditionEvaluator _winEval;
        private RoundSystem           _rounds;

        // ── Estado ────────────────────────────────────────────

        public IGameModeContext Context   => _ctx;
        public bool             IsRunning { get; private set; }
        public string           ModeID    => _definition?.ModeID ?? "unknown";

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (_definition == null)
            {
                CoreLogger.LogError("[GameModeBase] GameModeDefinitionSO no asignado.");
                return;
            }

            // Construir contexto
            _ctx = new GameModeContext();
            _ctx.Init(_definition.ModeID, _definition.TeamConfig, _definition.ScoreConfig);

            // RuleEngine
            _ruleEngine = new RuleEngine();
            _ruleEngine.Initialize(_ctx, _definition.GetRules());

            // WinConditionEvaluator
            _winEval              = new WinConditionEvaluator();
            _winEval.OnWinDetected = OnWinDetected;
            _winEval.Initialize(_ctx, _definition.GetWinConditions());

            // RoundSystem
            _rounds = new RoundSystem(_definition.RoundConfig, this);
            _rounds.OnTimeExpired = HandleTimeExpired;

            CoreLogger.LogSystem("GameModeBase", $"Modo '{ModeID}' inicializado.");
        }

        private void OnDestroy()
        {
            _ruleEngine?.Dispose();
            _winEval?.Dispose();
        }

        // ── API Pública ───────────────────────────────────────

        public void StartGame()
        {
            if (!_isAuthority) return;
            if (IsRunning) return;

            IsRunning = true;
            _ctx.SetPhase(GameModePhase.WarmUp);

            CoreLogger.LogSystem("GameModeBase", $"[{ModeID}] StartGame()");

            EventBus<GameStartedEvent>.Raise(new GameStartedEvent
            {
                ModeID    = ModeID,
                Round     = _ctx.CurrentRound,
                Timestamp = Time.realtimeSinceStartup
            });

            StartCoroutine(WarmUpThenPlay());
        }

        public void EndGame(int winnerTeamID = -1, string reason = "Manual")
        {
            if (!_isAuthority) return;

            IsRunning = false;
            _ctx.SetPhase(GameModePhase.PostGame);
            _rounds.Stop();

            CoreLogger.LogSystem("GameModeBase",
                $"[{ModeID}] EndGame. Winner=T{winnerTeamID} ({reason})");

            EventBus<GameEndedEvent>.Raise(new GameEndedEvent
            {
                ModeID       = ModeID,
                WinnerTeamID = winnerTeamID,
                Reason       = reason,
                Duration     = _ctx.ElapsedTime
            });

            // Notificar al Core
            EventBus<Core.Events.GameStateChangeRequestedEvent>.Raise(
                new Core.Events.GameStateChangeRequestedEvent
                {
                    TargetState = Core.GameState.GameOver
                });
        }

        public void ResetGame()
        {
            _rounds.Stop();
            _ctx.ResetScore();
            _ctx.SetRound(1);
            _ctx.SetPhase(GameModePhase.Idle);
            IsRunning = false;
        }

        // ── Fases ─────────────────────────────────────────────

        private IEnumerator WarmUpThenPlay()
        {
            float warmup = _definition.RoundConfig.WarmUpDuration;
            if (warmup > 0f)
            {
                SetAllPlayerInputEnabled(false);
                yield return new WaitForSeconds(warmup);
                SetAllPlayerInputEnabled(true);
            }

            _ctx.SetPhase(GameModePhase.Playing);
            _rounds.StartRound(_ctx.CurrentRound);
        }

        private IEnumerator RoundEndSequence(int winnerTeamID)
        {
            _ctx.SetPhase(GameModePhase.RoundEnd);
            SetAllPlayerInputEnabled(false);

            EventBus<RoundEndedEvent>.Raise(new RoundEndedEvent
            {
                Round        = _ctx.CurrentRound,
                WinnerTeamID = winnerTeamID,
                ScoreTeamA   = _ctx.Score.GetTeamScore(0),
                ScoreTeamB   = _ctx.Score.GetTeamScore(1)
            });

            yield return new WaitForSeconds(_definition.RoundConfig.RoundEndDuration);

            // ¿Fin de partida o siguiente ronda?
            int rounWins = GetTeamRoundWins(winnerTeamID);
            if (rounWins >= _definition.RoundConfig.RoundsToWinMatch)
            {
                EndGame(winnerTeamID, "RoundsWon");
            }
            else
            {
                _ctx.SetRound(_ctx.CurrentRound + 1);
                _ctx.ResetScore();
                _ctx._objectives.ResetAll();

                if (_definition.TeamConfig.AutoBalance)
                    _ctx._teams.Rebalance();

                StartCoroutine(WarmUpThenPlay());
            }
        }

        // ── Callbacks de victoria ─────────────────────────────

        private void OnWinDetected(WinResult result)
        {
            if (!IsRunning) return;
            StartCoroutine(RoundEndSequence(result.WinnerTeamID));
        }

        private void HandleTimeExpired()
        {
            if (!IsRunning) return;
            int leader = _ctx.Score.GetLeadingTeam();
            StartCoroutine(RoundEndSequence(leader));
        }

        // ── Helpers ───────────────────────────────────────────

        private readonly System.Collections.Generic.Dictionary<int, int> _roundWins = new();

        private int GetTeamRoundWins(int teamID)
        {
            _roundWins.TryGetValue(teamID, out int w);
            _roundWins[teamID] = w + 1;
            return _roundWins[teamID];
        }

        private void SetAllPlayerInputEnabled(bool enabled)
        {
            var controllers = FindObjectsByType<Player.Controller.PlayerController>(
                FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (enabled) c.EnableInput();
                else         c.DisableInput();
            }
        }

        // ── API: registro de objetivos ────────────────────────

        public void RegisterObjective(IObjective obj)
            => _ctx._objectives.Register(obj);
    }
}