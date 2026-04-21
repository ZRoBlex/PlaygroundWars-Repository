// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_GameModeBase.cs                            ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • GameModeBase (MonoBehaviour)                        ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Orquestar el ciclo de vida del modo de juego.         ║
// ║    Posee el GameModeContext (único poseedor mutable).    ║
// ║    NO contiene reglas de juego.                          ║
// ║    NO conoce implementaciones de objetivos.              ║
// ║                                                          ║
// ║  DEPENDENCIAS:                                           ║
// ║    • GameModeDefinitionSO (asignar en Inspector)         ║
// ║    • Core.Events.EventBus                                ║
// ║    • Core.GameStateChangeRequestedEvent (conecta al Core)║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    1. Crear GameObject "GameMode" en la escena de juego  ║
// ║    2. Añadir GameModeBase.cs                             ║
// ║    3. Arrastrar el GameModeDefinitionSO al campo _def    ║
// ║    4. Si es servidor/host: _isAuthority = true           ║
// ║    5. Si es cliente puro: _isAuthority = false           ║
// ║                                                          ║
// ║  SERVER AUTHORITY:                                       ║
// ║    StartGame(), EndGame(), HandleRoundEnd():             ║
// ║      → guardeados con _isAuthority                       ║
// ║    RegisterObjective(), context mutations:               ║
// ║      → también solo en servidor                         ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • _def null → NullRef en Awake                        ║
// ║    • TeamConfig.TeamNames.Length < TeamCount → error     ║
// ║    • _isAuthority = true en AMBOS host y client → desync ║
// ║    • No llamar StartGame() → la partida nunca empieza    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF.Config;
using UnityEngine;

namespace GMF
{
    [DisallowMultipleComponent]
    public class GameModeBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Definición del modo")]
        [SerializeField] private GameModeDefinitionSO _def;

        [Header("Autoridad de servidor")]
        [Tooltip("true = servidor/host/offline. false = cliente puro.")]
        [SerializeField] private bool _isAuthority = true;

        // ── Subsistemas internos ──────────────────────────────

        private GameModeContext       _ctx;
        private RuleEngine            _rules;
        private WinConditionEvaluator _winEval;
        private Coroutine             _phaseCoroutine;
        private readonly Dictionary<int, int> _roundWinsPerTeam = new();

        // ── Estado público ────────────────────────────────────

        public IGameModeContext Context   => _ctx;
        public bool             IsRunning { get; private set; }
        public string           ModeID    => _def?.ModeID ?? "unknown";
        public bool             IsAuthority => _isAuthority;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (_def == null)
            {
                CoreLogger.LogError("[GameModeBase] GameModeDefinitionSO no asignado.");
                enabled = false;
                return;
            }

            // Solo el servidor construye el contexto y el motor de reglas
            if (_isAuthority)
            {
                _ctx = new GameModeContext();
                _ctx.Build(_def.ModeID, _def.TeamConfig, _def.ScoreConfig);

                _rules = new RuleEngine();
                _rules.Initialize(_ctx, _def.GetRules());

                _winEval                = new WinConditionEvaluator();
                _winEval.OnWinDetected  = OnWinDetected;
                _winEval.Initialize(_ctx, _def.GetWinConditions());
            }
            else
            {
                // Cliente: contexto de solo lectura (sin subsistemas mutables)
                _ctx = new GameModeContext();
                _ctx.Build(_def.ModeID, _def.TeamConfig, _def.ScoreConfig);
            }

            // Escuchar ObjectiveResetEvt para propagar reset a los objetivos
            EventBus<ObjectiveResetEvt>.Subscribe(OnObjectiveResetRequested);

            CoreLogger.LogSystem("GameModeBase",
                $"'{_def.ModeID}' inicializado. Authority={_isAuthority}");
        }

        private void OnDestroy()
        {
            _rules?.Dispose();
            _winEval?.Dispose();
            EventBus<ObjectiveResetEvt>.Unsubscribe(OnObjectiveResetRequested);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Inicia la partida.
        /// Solo debe llamarse desde el servidor/host.
        /// </summary>
        public void StartGame()
        {
            if (!_isAuthority)
            {
                CoreLogger.LogWarning("[GameModeBase] StartGame ignorado: sin autoridad.");
                return;
            }
            if (IsRunning) return;

            IsRunning = true;
            _roundWinsPerTeam.Clear();

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

        /// <summary>
        /// Termina la partida inmediatamente.
        /// Solo debe llamarse desde el servidor/host.
        /// </summary>
        public void EndGame(int winnerTeamID = -1, string reason = "Manual")
        {
            if (!_isAuthority) return;

            IsRunning = false;
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

            // Notificar al Core System para cambiar GameState
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
            _ctx.SetPhase(GameModePhase.Idle);
            _ctx.SetRound(1);
            _ctx.ResetRoundScore();
            _roundWinsPerTeam.Clear();
            _ctx._objectives.ResetAll();
        }

        // ── Registro de objetivos ─────────────────────────────

        /// <summary>
        /// Llamado por ObjectiveBase.Start() automáticamente.
        /// No llamar manualmente en circunstancias normales.
        /// </summary>
        public void RegisterObjective(IObjective obj)
        {
            _ctx._objectives.Register(obj);
        }

        public void UnregisterObjective(string id)
        {
            _ctx._objectives.Unregister(id);
        }

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

            // Timer de ronda
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

                EventBus<RoundTimerTickEvt>.Raise(new RoundTimerTickEvt
                {
                    Remaining = Mathf.Max(0f, total - elapsed),
                    Total     = total
                });
            }

            // Tiempo agotado
            if (_ctx.Phase == GameModePhase.Playing)
            {
                var result = _winEval.EvaluateTimeOut();
                HandleRoundEnd(result.WinnerTeamID, result.Reason);
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

            // Verificar si alguien ganó la partida (suficientes rondas)
            if (winnerTeamID >= 0)
            {
                _roundWinsPerTeam.TryGetValue(winnerTeamID, out int wins);
                _roundWinsPerTeam[winnerTeamID] = wins + 1;
            }

            bool matchWon = winnerTeamID >= 0
                && _roundWinsPerTeam[winnerTeamID] >= _def.RoundConfig.RoundsToWinMatch;

            if (matchWon)
            {
                EndGame(winnerTeamID, "RoundsWon");
            }
            else
            {
                // Siguiente ronda
                _ctx.SetRound(_ctx.CurrentRound + 1);
                _ctx.ResetRoundScore();
                _ctx._objectives.ResetAll();

                if (_def.TeamConfig.AutoBalance)
                    _ctx._teams.Rebalance();

                _phaseCoroutine = StartCoroutine(WarmUpThenPlay());
            }
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnWinDetected(WinResult result)
        {
            if (_ctx.Phase != GameModePhase.Playing) return;
            HandleRoundEnd(result.WinnerTeamID, result.Reason);
        }

        private void HandleRoundEnd(int winnerTeamID, string reason)
        {
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }
            _phaseCoroutine = StartCoroutine(RoundEndSequence(winnerTeamID));
        }

        private void OnObjectiveResetRequested(ObjectiveResetEvt e)
        {
            _ctx._objectives.Get(e.ObjectiveID)?.Reset();
        }

        // ── Helpers ───────────────────────────────────────────

        private void SetInputEnabled(bool enabled)
        {
            var controllers = FindObjectsByType<Player.Controller.PlayerController>(
                FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (enabled) c.EnableInput();
                else         c.DisableInput();
            }
        }
    }
}
