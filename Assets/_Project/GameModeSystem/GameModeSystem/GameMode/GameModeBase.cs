// ============================================================
//  GameModeBase.cs
//  GameMode/GameMode/GameModeBase.cs
//
//  RESPONSABILIDAD ÚNICA: Contrato base de cualquier modo de juego.
//
//  Para crear un nuevo modo: heredar de GameModeBase
//  y sobreescribir los métodos virtuales necesarios.
//  No modificar esta clase para añadir nuevos modos.
// ============================================================

using GameMode.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GameMode
{
    public abstract class GameModeBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Identificación (sobreescribir en subclase)")]
        [SerializeField] protected string _gameModeID   = "Base";
        [SerializeField] protected string _displayName  = "Game Mode";

        // ── Estado ────────────────────────────────────────────

        public GameModePhase  Phase       { get; protected set; } = GameModePhase.Idle;
        public bool           IsRunning   { get; protected set; }
        public float          ElapsedTime { get; protected set; }
        public string         GameModeID  => _gameModeID;

        // ── Métodos del contrato ──────────────────────────────

        /// <summary>Inicia el modo de juego. Llamado por GameModeManager.</summary>
        public virtual void StartGame()
        {
            IsRunning   = true;
            ElapsedTime = 0f;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] StartGame()");

            EventBus<OnGameStartedEvent>.Raise(new OnGameStartedEvent
            {
                GameModeID = _gameModeID,
                Timestamp  = Time.realtimeSinceStartup
            });
        }

        /// <summary>Termina el modo de juego. Llamado por GameModeManager o desde la subclase.</summary>
        public virtual void EndGame(int winnerTeamID = -1)
        {
            IsRunning = false;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] EndGame. Winner={winnerTeamID}");

            EventBus<OnGameEndedEvent>.Raise(new OnGameEndedEvent
            {
                GameModeID    = _gameModeID,
                WinnerTeamID  = winnerTeamID,
                Duration      = ElapsedTime
            });

            // Conectar con el Core GameStateManager
            EventBus<Core.Events.GameStateChangeRequestedEvent>.Raise(
                new Core.Events.GameStateChangeRequestedEvent
                {
                    TargetState = Core.GameState.GameOver
                });
        }

        /// <summary>Lógica frame-a-frame. Update en la subclase debe llamar base.UpdateGame().</summary>
        public virtual void UpdateGame()
        {
            if (!IsRunning) return;
            ElapsedTime += Time.deltaTime;
        }

        /// <summary>Llamado entre rondas o al reiniciar. Resetea estado del modo.</summary>
        public virtual void ResetGame()
        {
            IsRunning   = false;
            ElapsedTime = 0f;
            Phase       = GameModePhase.Idle;
        }

        /// <summary>Cambia la fase del GameMode y publica el evento.</summary>
        protected void SetPhase(GameModePhase newPhase)
        {
            GameModePhase prev = Phase;
            Phase = newPhase;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] Fase: {prev} → {newPhase}");

            EventBus<OnGameModePhaseChangedEvent>.Raise(new OnGameModePhaseChangedEvent
            {
                Previous = prev,
                Current  = newPhase
            });
        }

        // ── Unity Lifecycle ───────────────────────────────────

        protected virtual void Update()
        {
            if (IsRunning) UpdateGame();
        }
    }
}

// ============================================================
//  GameModeManager.cs
//  GameMode/GameMode/GameModeManager.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar el modo de juego activo.
//
//  • Registrar modos disponibles
//  • Activar/desactivar modos dinámicamente
//  • Un solo modo activo a la vez
// ============================================================

namespace GameMode
{
    using System.Collections.Generic;
    using Core.Debug;
    using Core.Events;
    using GameMode.Events;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class GameModeManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Modo inicial")]
        [SerializeField] private GameModeBase _defaultMode;

        // ── Estado ────────────────────────────────────────────

        public GameModeBase CurrentMode { get; private set; }

        private readonly Dictionary<string, GameModeBase> _modes = new();

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            // Registrar todos los modos hijos automáticamente
            foreach (var mode in GetComponentsInChildren<GameModeBase>())
            {
                _modes[mode.GameModeID] = mode;
                mode.gameObject.SetActive(false);
                CoreLogger.LogSystemDebug("GameModeManager",
                    $"Modo registrado: {mode.GameModeID}");
            }
        }

        private void OnEnable()
        {
            // Iniciar cuando el Core entra en Playing
            EventBus<Core.Events.GameStateChangedEvent>.Subscribe(OnCoreStateChanged);
        }

        private void OnDisable()
        {
            EventBus<Core.Events.GameStateChangedEvent>.Unsubscribe(OnCoreStateChanged);
        }

        private void OnCoreStateChanged(Core.Events.GameStateChangedEvent e)
        {
            if (e.Current == Core.GameState.Playing && CurrentMode == null)
                ActivateMode(_defaultMode?.GameModeID ?? string.Empty);
            else if (e.Current == Core.GameState.GameOver)
                CurrentMode?.EndGame();
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Activa un modo por ID, desactivando el anterior.</summary>
        public bool ActivateMode(string modeID)
        {
            if (string.IsNullOrEmpty(modeID) || !_modes.ContainsKey(modeID))
            {
                // Intentar con el defaultMode directo si no está registrado
                if (_defaultMode != null && CurrentMode == null)
                {
                    SetMode(_defaultMode);
                    return true;
                }
                CoreLogger.LogWarning($"[GameModeManager] Modo '{modeID}' no registrado.");
                return false;
            }

            SetMode(_modes[modeID]);
            return true;
        }

        private void SetMode(GameModeBase mode)
        {
            if (mode == null) return;

            string prevID = CurrentMode?.GameModeID ?? string.Empty;

            CurrentMode?.ResetGame();
            CurrentMode?.gameObject.SetActive(false);

            CurrentMode = mode;
            CurrentMode.gameObject.SetActive(true);
            CurrentMode.StartGame();

            EventBus<OnGameModeChangedEvent>.Raise(new OnGameModeChangedEvent
            {
                PreviousModeID = prevID,
                NewModeID      = mode.GameModeID
            });

            CoreLogger.LogSystem("GameModeManager", $"Modo activo: {mode.GameModeID}");
        }

        public void StartCurrentMode() => CurrentMode?.StartGame();
        public void EndCurrentMode(int winner = -1) => CurrentMode?.EndGame(winner);

        public GameModeBase GetMode(string id)
            => _modes.TryGetValue(id, out var m) ? m : null;
    }
}
