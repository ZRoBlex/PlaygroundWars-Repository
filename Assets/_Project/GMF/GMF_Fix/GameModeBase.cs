// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_GameModeBase.cs  (REEMPLAZA todo anterior) ║
// ║                                                          ║
// ║  FLUJO COMPLETO DE RONDA:                                ║
// ║  1. ForceRespawnAll() → todos aparecen en sus bases      ║
// ║  2. WarmUp → jugadores no pueden moverse                 ║
// ║  3. StartRound → activa objetivos, input habilitado      ║
// ║  4. Juego normal hasta que WinCondition se cumple        ║
// ║     o el tiempo se acaba                                 ║
// ║  5. RoundEnd → UI + esperar RoundEndDuration             ║
// ║  6. Verificar si alguien ganó la partida (rondas)        ║
// ║  7. Si hay empate y SuddenDeath → ronda extra            ║
// ║  8. Si no → EndGame con tabla final                      ║
// ║                                                          ║
// ║  MUERTE SÚBITA:                                          ║
// ║    Se activa cuando todas las rondas acaban y 2+ equipos ║
// ║    tienen las mismas rondas ganadas.                     ║
// ║    Se extiende RoundsToWinMatch +1 para forzar decisión. ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static GameModeBase Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────

        [Header("Definición del modo")]
        [SerializeField] private GMF_Config _def;

        [SerializeField] private bool _isAuthority = true;

        [Header("Spawn Areas (auto-descubiertas en Start si vacío)")]
        [Tooltip("Dejar vacío para auto-descubrir GMFSpawnArea en la escena.")]
        [SerializeField] private GMFSpawnArea[] _spawnAreas = System.Array.Empty<GMFSpawnArea>();

        [Header("Objetos de la escena activados al iniciar ronda")]
        [Tooltip("Estos GameObjects se activan al acabar el WarmUp y se desactivan al iniciar.")]
        [SerializeField] private GameObject[] _roundStartObjects = System.Array.Empty<GameObject>();

        // ── Subsistemas ───────────────────────────────────────

        private GameModeContext       _ctx;
        private RuleEngine            _ruleEngine;
        private WinConditionEvaluator _winEval;
        private Coroutine             _phaseCoroutine;

        // ── Estado de partida ─────────────────────────────────

        private readonly Dictionary<int, int> _roundWins = new();
        private          bool                 _isSuddenDeath;
        private          int                  _currentRoundsToWin;

        // ── Propiedades públicas ──────────────────────────────

        public IGameModeContext                  Context         => _ctx;
        public bool                              IsRunning       { get; private set; }
        public bool                              IsAuthority     => _isAuthority;
        public string                            ModeID          => _def?.ModeID ?? "unknown";
        public GMF_Config              Definition      => _def;
        public IReadOnlyDictionary<int, int>     RoundWinsPerTeam => _roundWins;
        public bool                              IsSuddenDeath   => _isSuddenDeath;
        public int                               RoundsToWin     => _currentRoundsToWin;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (_def == null)
            {
                CoreLogger.LogError($"[GameModeBase] '{name}': GameModeDefinitionSO no asignado.");
                return;
            }

            _ctx = new GameModeContext();
            _ctx.Build(_def.ModeID, _def.TeamConfig, _def.ScoreConfig);
            _currentRoundsToWin = _def.MatchConfig.RoundsToWinMatch;

            if (_isAuthority)
            {
                _ruleEngine = new RuleEngine();
                _ruleEngine.Initialize(_ctx, _def.GetRules());

                _winEval              = new WinConditionEvaluator();
                _winEval.OnWinDetected = OnWinDetected;
                _winEval.Initialize(_ctx, _def.GetWinConditions());
            }

            EventBus<ObjectiveResetEvt>.Subscribe(OnObjectiveReset);
            EventBus<PlayerEliminatedEvt>.Subscribe(OnPlayerEliminated);
        }

        private void Start()
        {
            // Auto-descubrir spawn areas si no están asignadas manualmente
            if (_spawnAreas == null || _spawnAreas.Length == 0)
            {
                _spawnAreas = FindObjectsByType<GMFSpawnArea>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                CoreLogger.LogSystem("GameModeBase",
                    $"Auto-descubiertas {_spawnAreas.Length} SpawnArea(s).");
            }

            // Desactivar objetos de ronda al inicio
            SetRoundObjects(false);
        }

        private void OnEnable()
        {
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
            EventBus<PlayerEliminatedEvt>.Unsubscribe(OnPlayerEliminated);
            if (Instance == this) Instance = null;
        }

        // ── API Pública ───────────────────────────────────────

        public void StartGame()
        {
            if (!_isAuthority) return;
            if (_def == null)  { CoreLogger.LogError("[GameModeBase] _def null."); return; }
            if (IsRunning) return;

            Instance  = this;
            IsRunning = true;
            _roundWins.Clear();
            _isSuddenDeath      = false;
            _currentRoundsToWin = _def.MatchConfig.RoundsToWinMatch;

            AssignAllPlayersToTeams();

            CoreLogger.LogSystem("GameModeBase", $"[{ModeID}] StartGame()");

            EventBus<GameStartedEvt>.Raise(new GameStartedEvt
            {
                ModeID    = ModeID,
                Round     = _ctx.CurrentRound,
                Timestamp = Time.realtimeSinceStartup
            });

            if (_phaseCoroutine != null) StopCoroutine(_phaseCoroutine);
            _phaseCoroutine = StartCoroutine(RoundFlow());
        }

        public void EndGame(int winnerTeamID = -1, string reason = "Manual")
        {
            if (!_isAuthority) return;

            IsRunning = false;
            if (Instance == this) Instance = null;
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }

            _ctx.SetPhase(GameModePhase.PostGame);
            SetRoundObjects(false);
            SetInputEnabled(false);

            CoreLogger.LogSystem("GameModeBase", $"[{ModeID}] EndGame T{winnerTeamID} ({reason})");

            EventBus<GameEndedEvt>.Raise(new GameEndedEvt
            {
                ModeID       = ModeID,
                WinnerTeamID = winnerTeamID,
                Reason       = reason,
                Duration     = _ctx.ElapsedTime
            });

            EventBus<Core.Events.GameStateChangeRequestedEvent>.Raise(
                new Core.Events.GameStateChangeRequestedEvent { TargetState = Core.GameState.GameOver });
        }

        public void ResetGame()
        {
            if (_phaseCoroutine != null) { StopCoroutine(_phaseCoroutine); _phaseCoroutine = null; }
            IsRunning           = false;
            _isSuddenDeath      = false;
            _currentRoundsToWin = _def?.MatchConfig.RoundsToWinMatch ?? 1;
            if (Instance == this) Instance = null;
            _ctx.SetPhase(GameModePhase.Idle);
            _ctx.SetRound(1);
            _ctx.ResetRoundScore();
            _roundWins.Clear();
            _ctx._objectives.ResetAll();
            SetRoundObjects(false);
            SetInputEnabled(false);
        }

        // ── Flujo principal de rondas ─────────────────────────

        private IEnumerator RoundFlow()
        {
            while (IsRunning)
            {
                // ── PASO 1: Respawnear a todos antes del warm-up ──

                _ctx.SetPhase(GameModePhase.WarmUp);
                SetInputEnabled(false);
                SetRoundObjects(false);
                _ctx._objectives.ResetAll();

                yield return StartCoroutine(ForceRespawnAllCoroutine());

                // ── PASO 2: Warm-Up ───────────────────────────────

                float warmup = _def.RoundConfig.WarmUpDuration;
                if (warmup > 0f)
                {
                    CoreLogger.LogSystem("GameModeBase",
                        $"[{ModeID}] WarmUp {warmup}s (R{_ctx.CurrentRound})");
                    yield return new WaitForSeconds(warmup);
                }

                // ── PASO 3: Inicio de ronda ───────────────────────

                SetInputEnabled(true);
                SetRoundObjects(true);
                _ctx.SetPhase(GameModePhase.Playing);
                _ctx.ResetRoundScore();    // reset de score al inicio real de la ronda

                EventBus<RoundStartedEvt>.Raise(new RoundStartedEvt
                {
                    Round    = _ctx.CurrentRound,
                    Duration = _def.RoundConfig.RoundDuration
                });

                CoreLogger.LogSystem("GameModeBase",
                    $"[{ModeID}] Ronda {_ctx.CurrentRound} iniciada.");

                // ── PASO 4: Esperar resultado ─────────────────────

                bool roundResolved = false;
                Coroutine timerCoro = null;

                if (_def.RoundConfig.RoundDuration > 0f)
                    timerCoro = StartCoroutine(RoundTimerRoutine(() => roundResolved = true));

                yield return new WaitUntil(() => roundResolved || _ctx.Phase == GameModePhase.RoundEnd);

                if (timerCoro != null) StopCoroutine(timerCoro);

                // ── PASO 5: Resultado de ronda ────────────────────

                int roundWinner = DetermineRoundWinner();
                yield return StartCoroutine(RoundEndSequence(roundWinner));

                // ── PASO 6: ¿Fin de partida? ──────────────────────

                int matchWinner = DetermineMatchWinner();

                if (matchWinner >= 0)
                {
                    yield return new WaitForSeconds(_def.RoundConfig.EndGameDuration);
                    EndGame(matchWinner, _isSuddenDeath ? "SuddenDeath" : "RoundsWon");
                    yield break;
                }

                // Verificar empate en rondas → muerte súbita
                if (ShouldTriggerSuddenDeath())
                {
                    _isSuddenDeath       = true;
                    _currentRoundsToWin += 1;

                    EventBus<SuddenDeathStartedEvt>.Raise(new SuddenDeathStartedEvt
                    {
                        Round = _ctx.CurrentRound + 1
                    });

                    CoreLogger.LogSystem("GameModeBase",
                        "⚡ MUERTE SÚBITA activada.");
                }

                // Siguiente ronda
                _ctx.SetRound(_ctx.CurrentRound + 1);
                if (_def.TeamConfig.AutoBalance) _ctx._teams.Rebalance();
            }
        }

        // ── Timer de ronda ────────────────────────────────────

        private IEnumerator RoundTimerRoutine(System.Action onExpire)
        {
            float total = _def.RoundConfig.RoundDuration;
            float elapsed = 0f;

            while (elapsed < total && _ctx.Phase == GameModePhase.Playing)
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
                CoreLogger.LogSystem("GameModeBase", "⏰ Tiempo de ronda agotado.");
                onExpire?.Invoke();
                // El DetermineRoundWinner usará kills como tiebreaker si está activado
            }
        }

        // ── Secuencia de fin de ronda ─────────────────────────

        private IEnumerator RoundEndSequence(int winnerTeamID)
        {
            _ctx.SetPhase(GameModePhase.RoundEnd);
            SetInputEnabled(false);
            SetRoundObjects(false);

            // Acumular victorias
            if (winnerTeamID >= 0)
            {
                _roundWins.TryGetValue(winnerTeamID, out int w);
                _roundWins[winnerTeamID] = w + 1;
            }

            EventBus<RoundEndedEvt>.Raise(new RoundEndedEvt
            {
                Round        = _ctx.CurrentRound,
                WinnerTeamID = winnerTeamID,
                ScoreTeamA   = _ctx.Score.GetTeamScore(0),
                ScoreTeamB   = _ctx.Score.GetTeamScore(1)
            });

            CoreLogger.LogSystem("GameModeBase",
                $"Ronda {_ctx.CurrentRound} terminada. Winner=T{winnerTeamID}. " +
                $"Rondas ganadas: {string.Join(", ", _roundWins.Select(kv => $"T{kv.Key}:{kv.Value}"))}");

            yield return new WaitForSeconds(_def.RoundConfig.RoundEndDuration);
        }

        // ── Determinación de ganadores ────────────────────────

        private int DetermineRoundWinner()
        {
            // Ganador ya determinado por WinCondition
            if (_ctx.Phase == GameModePhase.RoundEnd)
            {
                // La ronda terminó por WinCondition — buscar quién tiene más puntos
                return _ctx.Score.GetLeadingTeam();
            }

            // Tiempo agotado
            int leader = _ctx.Score.GetLeadingTeam();

            // Verificar empate en puntos → tiebreaker por kills si está activado
            int teamCount = _def.TeamConfig.TeamCount;
            if (IsScoreTied() && _def.MatchConfig.KillTiebreakerOnTimeOut)
            {
                int killLeader = (_ctx.Score as ScoreSystem)
                    ?.GetLeadingTeamByKills(teamCount) ?? -1;

                if (killLeader >= 0)
                {
                    CoreLogger.LogSystem("GameModeBase",
                        $"Empate en puntos → T{killLeader} gana por kills.");
                    return killLeader;
                }
            }

            return leader; // -1 si empate total
        }

        private int DetermineMatchWinner()
        {
            int tc = _def.TeamConfig.TeamCount;

            for (int t = 0; t < tc; t++)
            {
                _roundWins.TryGetValue(t, out int w);
                if (w >= _currentRoundsToWin) return t;
            }
            return -1;
        }

        private bool ShouldTriggerSuddenDeath()
        {
            if (!_def.MatchConfig.SuddenDeathOnTie) return false;
            if (_isSuddenDeath) return false;  // ya estamos en muerte súbita

            // ¿Hay 2+ equipos empatados en el máximo de rondas ganadas?
            int max = _roundWins.Count > 0 ? _roundWins.Values.Max() : 0;
            if (max < _currentRoundsToWin - 1) return false; // nadie cerca del límite

            int atMax = _roundWins.Count(kv => kv.Value == max);
            return atMax >= 2;
        }

        private bool IsScoreTied()
        {
            if (_def.TeamConfig.TeamCount < 2) return false;
            int scoreA = _ctx.Score.GetTeamScore(0);
            for (int t = 1; t < _def.TeamConfig.TeamCount; t++)
                if (_ctx.Score.GetTeamScore(t) != scoreA) return false;
            return true;
        }

        // ── WinCondition callback ─────────────────────────────

        private void OnWinDetected(WinResult result)
        {
            if (_ctx.Phase != GameModePhase.Playing) return;

            // Terminar la ronda directamente — el RoundFlow loop lo detecta
            _ctx.SetPhase(GameModePhase.RoundEnd);
            CoreLogger.LogSystem("GameModeBase",
                $"WinCondition '{result.Reason}' T{result.WinnerTeamID}.");
        }

        // ── Respawn masivo ────────────────────────────────────

        /// <summary>
        /// Respawnea a todos los jugadores en sus bases de equipo.
        /// Espera a que todos hayan reaparecido antes de continuar.
        /// </summary>
        private IEnumerator ForceRespawnAllCoroutine()
        {
            // Limpiar reservas de spawn para este batch
            foreach (var area in _spawnAreas)
                area.ClearReservations();

            var authorities = FindObjectsByType<Player.Authority.PlayerAuthority>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var auth in authorities)
            {
                int teamID = _ctx.Teams.GetTeam(auth.PlayerID);
                if (teamID < 0) _ctx._teams.AutoAssign(auth.PlayerID);
                teamID = _ctx.Teams.GetTeam(auth.PlayerID);

                GMFSpawnArea area = GetSpawnAreaForTeam(teamID);
                Vector3 spawnPos  = area != null && area.TryGetSpawnPosition(out Vector3 p)
                    ? p
                    : auth.transform.position + Vector3.up;

                // Respawn sin contar como kill/muerte
                var respawn = auth.GetComponent<Player.Respawn.PlayerRespawn>();
                respawn?.ForceRespawnAt(spawnPos, Quaternion.identity);
            }

            // Dar un frame para que los respawns procesen
            yield return null;
        }

        private GMFSpawnArea GetSpawnAreaForTeam(int teamID)
        {
            foreach (var area in _spawnAreas)
                if (area != null && area.TeamID == teamID)
                    return area;
            return null;
        }

        // ── Team assignment ───────────────────────────────────

        public void AssignAllPlayersToTeams()
        {
            if (_ctx == null) return;

            var authorities = FindObjectsByType<Player.Authority.PlayerAuthority>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var auth in authorities)
                if (_ctx.Teams.GetTeam(auth.PlayerID) < 0)
                    _ctx._teams.AutoAssign(auth.PlayerID);
        }

        private void OnPlayerReady(PlayerReadyEvent e)
        {
            if (!_isAuthority || _ctx == null) return;
            if (_ctx.Teams.GetTeam(e.PlayerID) < 0)
                _ctx._teams.AutoAssign(e.PlayerID);
        }

        // ── Kill tracking ─────────────────────────────────────

        private void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            if (!_isAuthority) return;
            (_ctx._score as ScoreSystem)?.AddKill(e.KillerTeamID, e.KillerID);
        }

        // ── Objetivo ──────────────────────────────────────────

        public void RegisterObjective(IObjective obj)   => _ctx._objectives.Register(obj);
        public void UnregisterObjective(string id)      => _ctx._objectives.Unregister(id);

        private void OnObjectiveReset(ObjectiveResetEvt e)
            => _ctx._objectives.Get(e.ObjectiveID)?.Reset();

        // ── Round objects ─────────────────────────────────────

        private void SetRoundObjects(bool active)
        {
            foreach (var go in _roundStartObjects)
                if (go != null) go.SetActive(active);
        }

        // ── Input ─────────────────────────────────────────────

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

    // ── Evento de muerte súbita ───────────────────────────────

    public struct SuddenDeathStartedEvt
    {
        public int Round;
    }
}