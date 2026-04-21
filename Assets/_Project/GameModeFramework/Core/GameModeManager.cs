// ============================================================
//  GameModeManager.cs
//  GameModeFramework/Core/GameModeManager.cs
//
//  RESPONSABILIDAD ÚNICA: Registro y activación de modos de juego.
//  Un solo modo activo a la vez.
// ============================================================

namespace GameMode.Framework
{
    using System.Collections.Generic;
    using GameMode.Framework.Events;
    using Core.Debug;
    using Core.Events;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class GameModeManager : MonoBehaviour
    {
        [Header("Modo por defecto")]
        [SerializeField] private GameModeBase _defaultMode;

        public GameModeBase CurrentMode { get; private set; }

        private readonly Dictionary<string, GameModeBase> _registry = new();

        private void Awake()
        {
            // Autoregistrar modos hijos
            foreach (var mode in GetComponentsInChildren<GameModeBase>(true))
            {
                _registry[mode.ModeID] = mode;
                mode.gameObject.SetActive(false);
                CoreLogger.LogSystemDebug("GameModeManager", $"Modo registrado: '{mode.ModeID}'");
            }
        }

        private void OnEnable()
        {
            EventBus<Core.Events.GameStateChangedEvent>.Subscribe(OnCoreStateChanged);
        }

        private void OnDisable()
        {
            EventBus<Core.Events.GameStateChangedEvent>.Unsubscribe(OnCoreStateChanged);
        }

        private void OnCoreStateChanged(Core.Events.GameStateChangedEvent e)
        {
            if (e.Current == Core.GameState.Playing && CurrentMode == null)
                Activate(_defaultMode?.ModeID ?? "");
        }

        public bool Activate(string modeID)
        {
            if (!_registry.TryGetValue(modeID, out var mode))
            {
                if (_defaultMode != null && !string.IsNullOrEmpty(modeID))
                    CoreLogger.LogWarning($"[GameModeManager] Modo '{modeID}' no registrado.");

                // Fallback al default
                if (_defaultMode != null)
                    mode = _defaultMode;
                else return false;
            }

            string prev = CurrentMode?.ModeID ?? "";
            CurrentMode?.ResetGame();
            CurrentMode?.gameObject.SetActive(false);

            CurrentMode = mode;
            CurrentMode.gameObject.SetActive(true);
            CurrentMode.StartGame();

            CoreLogger.LogSystem("GameModeManager", $"Modo activo: '{mode.ModeID}'");

            EventBus<GameMode.Framework.Events.GameInitializedEvent>.Raise(
                new GameMode.Framework.Events.GameInitializedEvent { ModeID = mode.ModeID });
            return true;
        }

        public void Register(GameModeBase mode)
        {
            if (mode == null) return;
            _registry[mode.ModeID] = mode;
        }
    }
}
