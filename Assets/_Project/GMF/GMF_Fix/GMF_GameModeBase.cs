// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_GameModeBase.cs  (REEMPLAZA el anterior)   ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + static Instance → Flag/Zones lo usan sin Find       ║
// ║    + auto-asignación de equipos en PlayerReadyEvent      ║
// ║    + GameModeBase ya NO se desactiva — GameModeManager   ║
// ║      simplemente no llama StartGame() en los inactivos   ║
// ║    - Eliminado: 'enabled = false' cuando _def es null    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF.Config;
using Player.Events;
using UnityEngine;

namespace GMF
{
    [DisallowMultipleComponent]
    public class GameModeBase : MonoBehaviour
    {
        // ── Static Instance ───────────────────────────────────
        // ► Punto de acceso sin FindObjectOfType.
        // ► Flag, CaptureZone y reglas lo usan para consultar Teams.
        // ► Se setea en StartGame(), se limpia en ResetGame/EndGame.

        public static GameModeBase Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────

        [Header("Definición del modo")]
        [SerializeField] private GMF_Config _def;

        [Tooltip("true = servidor/host/offline.  false = cliente puro (NO evalúa reglas).")]
        [SerializeField] private bool _isAuthority = true;

        // ── Subsistemas ───────────────────────────────────────

        private GameModeContext       _ctx;
        private RuleEngine            _ruleEngine;
        private WinConditionEvaluator _winEval;
        private Coroutine             _phaseCoroutine;
        private readonly Dictionary<int, int> _roundWins = new();

        // ── Estado público ────────────────────────────────────

        public IGameModeContext Context     => _ctx;
        public bool             IsRunning   { get; private set; }
        public bool             IsAuthority => _isAuthority;
        public string           ModeID      => _def != null ? _def.ModeID : "unknown";

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (_def == null)
            {
                CoreLogger.LogError($"[GameModeBase] '{name}': GameModeDefinitionSO no asignado.");
                return;
            }

            _ctx = new GameModeContext();
            _ctx.Build(_def.ModeID, _def.TeamConfig, _def.ScoreConfig);

            if (_isAuthority)
            {
                _ruleEngine = new RuleEngine();
                _ruleEngine.Initialize(_ctx, _def.GetRules());

                _winEval                = new WinConditionEvaluator();
                _winEval.OnWinDetected  = OnWinDetected;
                _winEval.Initialize(_ctx, _def.GetWinConditions());
            }

            EventBus<ObjectiveResetEvt>.Subscribe(OnObjectiveReset);
        }

        private void OnEnable()
        {
            // Asignación automática de equipos cuando un jugador está listo
            EventBus<PlayerReadyEvent>.Subscribe(OnPlayerReady);
        }

        private void OnDisable()
        {
            EventBus<PlayerReadyEvent>.Unsubscribe(OnPlayerReady);
        }

        private void OnDestroy()
        {
            _ruleEngine?.Dispose();
            _winEval?.Dispose();
            EventBus<ObjectiveResetEvt>.Unsubscribe(OnObjectiveReset);
            if (Instance == this) Instance = null;
        }

        // ── Team auto-assign ──────────────────────────────────

        private void OnPlayerReady(PlayerReadyEvent e)
        {
            if (!_isAuthority || !IsRunning) return;
            if (_ctx.Teams.GetTeam(e.PlayerID) >= 0) return; // ya asignado

            _ctx._teams.AutoAssign(e.PlayerID);
        }

        // ── API Pública ───────────────────────────────────────

        public void StartGame()
        {
            if (!_isAuthority)
            {
                CoreLogger.LogWarning("[GameModeBase] StartGame ignorado: sin autoridad.");
                return;
            }
            if (_def == null)
            {
                CoreLogger.LogError("[GameModeBase] StartGame cancelado: _def es null.");
                return;
            }
            if (IsRunning) return;

            Instance  = this;
            IsRunning = true;
            _roundWins.Clear();

            CoreLogger.LogSystem("GameModeBase", $"[{ModeID}] StartGame()");

            EventBus<GameStartedEvt>.Raise(new GameStartedEvt
            {
                ModeID    = ModeID,
                Round     = _ctx.CurrentRound,
                Timestamp = Time.realtimeSinceStartup
            });

            if (_phaseCoroutine != null) StopCoroutine(_phaseCoroutine);
            _phaseCoroutine = StartCoroutine(WarmUpThenPlay());
        }

        public void EndGame(int winnerTeamID = -1, string reason = "Manual")
        {
            if (!_isAuthority) return;

            IsRunning = false;
            if (Instance == this) Instance = null;
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }

            _ctx.SetPhase(GameModePhase.PostGame);

            CoreLogger.LogSystem("GameModeBase",
                $"[{ModeID}] EndGame. Winner=T{winnerTeamID} ({reason})");

            EventBus<GameEndedEvt>.Raise(new GameEndedEvt
            {
                ModeID       = ModeID,
                WinnerTeamID = winnerTeamID,
                Reason       = reason,
                Duration     = _ctx.ElapsedTime
            });

            EventBus<Core.Events.GameStateChangeRequestedEvent>.Raise(
                new Core.Events.GameStateChangeRequestedEvent
                {
                    TargetState = Core.GameState.GameOver
                });
        }

        public void ResetGame()
        {
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }
            IsRunning = false;
            if (Instance == this) Instance = null;
            _ctx.SetPhase(GameModePhase.Idle);
            _ctx.SetRound(1);
            _ctx.ResetRoundScore();
            _roundWins.Clear();
            _ctx._objectives.ResetAll();
        }

        // ── Registro de objetivos ─────────────────────────────

        public void RegisterObjective(IObjective obj)   => _ctx._objectives.Register(obj);
        public void UnregisterObjective(string id)      => _ctx._objectives.Unregister(id);

        // ── Fases ─────────────────────────────────────────────

        private IEnumerator WarmUpThenPlay()
        {
            _ctx.SetPhase(GameModePhase.WarmUp);
            SetInputEnabled(false);

            float warmup = _def.RoundConfig.WarmUpDuration;
            if (warmup > 0f) yield return new WaitForSeconds(warmup);

            SetInputEnabled(true);
            _ctx.SetPhase(GameModePhase.Playing);

            EventBus<RoundStartedEvt>.Raise(new RoundStartedEvt
            {
                Round    = _ctx.CurrentRound,
                Duration = _def.RoundConfig.RoundDuration
            });

            if (_def.RoundConfig.RoundDuration > 0f)
                _phaseCoroutine = StartCoroutine(RoundTimerRoutine());
        }

        private IEnumerator RoundTimerRoutine()
        {
            float total = _def.RoundConfig.RoundDuration;
            float elapsed = 0f;

            while (elapsed < total)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                _ctx.Tick(1f);

                EventBus<RoundTimerTickEvt>.Raise(new RoundTimerTickEvt
                {
                    Remaining = Mathf.Max(0f, total - elapsed),
                    Total     = total
                });
            }

            if (_ctx.Phase == GameModePhase.Playing)
            {
                var result = _winEval != null
                    ? _winEval.EvaluateTimeOut()
                    : WinResult.Draw;
                StartRoundEndSequence(result.WinnerTeamID, result.Reason);
            }
        }

        private IEnumerator RoundEndSequence(int winnerTeamID)
        {
            _ctx.SetPhase(GameModePhase.RoundEnd);
            SetInputEnabled(false);

            EventBus<RoundEndedEvt>.Raise(new RoundEndedEvt
            {
                Round        = _ctx.CurrentRound,
                WinnerTeamID = winnerTeamID,
                ScoreTeamA   = _ctx.Score.GetTeamScore(0),
                ScoreTeamB   = _ctx.Score.GetTeamScore(1)
            });

            yield return new WaitForSeconds(_def.RoundConfig.RoundEndDuration);

            // Contar victorias de ronda
            if (winnerTeamID >= 0)
            {
                _roundWins.TryGetValue(winnerTeamID, out int w);
                _roundWins[winnerTeamID] = w + 1;
            }

            bool matchWon = winnerTeamID >= 0
                && _roundWins.TryGetValue(winnerTeamID, out int wins)
                && wins >= _def.RoundConfig.RoundsToWinMatch;

            if (matchWon)
            {
                EndGame(winnerTeamID, "RoundsWon");
            }
            else
            {
                _ctx.SetRound(_ctx.CurrentRound + 1);
                _ctx.ResetRoundScore();
                _ctx._objectives.ResetAll();
                if (_def.TeamConfig.AutoBalance) _ctx._teams.Rebalance();
                _phaseCoroutine = StartCoroutine(WarmUpThenPlay());
            }
        }

        private void StartRoundEndSequence(int winnerTeamID, string reason)
        {
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }
            _phaseCoroutine = StartCoroutine(RoundEndSequence(winnerTeamID));
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnWinDetected(WinResult result)
        {
            if (_ctx.Phase != GameModePhase.Playing) return;
            StartRoundEndSequence(result.WinnerTeamID, result.Reason);
        }

        private void OnObjectiveReset(ObjectiveResetEvt e)
        {
            _ctx._objectives.Get(e.ObjectiveID)?.Reset();
        }

        // ── Helpers ───────────────────────────────────────────

        private void SetInputEnabled(bool enabled)
        {
            var controllers = FindObjectsByType<Player.Controller.PlayerController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (enabled) c.EnableInput();
                else         c.DisableInput();
            }
        }
    }
}
