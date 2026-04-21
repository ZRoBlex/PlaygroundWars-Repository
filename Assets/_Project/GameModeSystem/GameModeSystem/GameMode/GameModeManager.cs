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
            EventBus<GameStateChangedEvent>.Subscribe(OnCoreStateChanged);
        }
 
        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnCoreStateChanged);
        }
 
        private void OnCoreStateChanged(GameStateChangedEvent e)
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