// ============================================================
//  GameStateManager.cs
//  Core/GameState/GameStateManager.cs
//
//  RESPONSABILIDAD ÚNICA: Manejo del estado global del juego.
//
//  CARACTERÍSTICAS:
//  • Máquina de estados finita (FSM) con transiciones validadas
//  • Comunicación via EventBus (sin referencias directas)
//  • Historial de estados para debugging
//  • Callbacks por estado (OnEnter / OnExit)
//  • Transiciones configurables, no hardcodeadas en lógica
//
//  USO:
//    GameStateManager.RequestStateChange(GameState.Playing);
//    GameStateManager.CurrentState
//    GameStateManager.IsInState(GameState.Playing)
//    EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
// ============================================================

using System;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Core
{
    public class GameStateManager
    {
        // ── Estado actual ─────────────────────────────────────

        public GameState CurrentState  { get; private set; } = GameState.None;
        public GameState PreviousState { get; private set; } = GameState.None;

        // ── Historial (para debug) ────────────────────────────

        private readonly List<GameState> _history = new();
        private const int MAX_HISTORY = 20;

        public IReadOnlyList<GameState> History => _history;

        // ── Transiciones válidas ──────────────────────────────
        // Definidas en tabla; no hardcodeadas en switch.
        // Para agregar una transición: añadir a este diccionario.

        private static readonly Dictionary<GameState, HashSet<GameState>> _validTransitions
            = new()
        {
            { GameState.None,         new HashSet<GameState> { GameState.Initializing } },
            { GameState.Initializing, new HashSet<GameState> { GameState.MainMenu } },
            { GameState.MainMenu,     new HashSet<GameState> { GameState.Lobby, GameState.Playing } },
            { GameState.Lobby,        new HashSet<GameState> { GameState.Playing, GameState.MainMenu } },
            { GameState.Playing,      new HashSet<GameState> { GameState.Paused, GameState.GameOver, GameState.MainMenu } },
            { GameState.Paused,       new HashSet<GameState> { GameState.Playing, GameState.MainMenu } },
            { GameState.GameOver,     new HashSet<GameState> { GameState.Lobby, GameState.MainMenu } },
        };

        // ── Callbacks por estado ──────────────────────────────
        // Sistemas externos pueden registrar acciones para Enter/Exit.

        private readonly Dictionary<GameState, Action> _onEnterCallbacks = new();
        private readonly Dictionary<GameState, Action> _onExitCallbacks  = new();

        // ── Constructor ───────────────────────────────────────

        public GameStateManager()
        {
            // Escuchar solicitudes externas de cambio de estado
            EventBus<GameStateChangeRequestedEvent>.Subscribe(OnStateChangeRequested);
        }

        public void Dispose()
        {
            EventBus<GameStateChangeRequestedEvent>.Unsubscribe(OnStateChangeRequested);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Solicita un cambio de estado. Valida la transición antes de aplicarla.
        /// </summary>
        public bool RequestStateChange(GameState target)
        {
            if (target == CurrentState)
            {
                CoreLogger.LogSystemDebug("GameStateManager", $"Ya en estado {target}, ignorando solicitud.");
                return false;
            }

            if (!IsTransitionValid(CurrentState, target))
            {
                CoreLogger.LogWarning(
                    $"[GameStateManager] Transición inválida: {CurrentState} → {target}. " +
                    "Usa ForceStateChange() si es intencional."
                );
                return false;
            }

            ApplyStateChange(target);
            return true;
        }

        /// <summary>
        /// Fuerza un cambio de estado ignorando la tabla de transiciones.
        /// Solo para casos excepcionales (desconexión, errores críticos).
        /// </summary>
        public void ForceStateChange(GameState target)
        {
            CoreLogger.LogSystemDebug("GameStateManager", $"Forzando cambio de estado: {CurrentState} → {target}");
            ApplyStateChange(target);
        }

        public bool IsInState(GameState state) => CurrentState == state;

        public bool WasInState(GameState state) => PreviousState == state;

        /// <summary>Registra acción a ejecutar al ENTRAR en un estado.</summary>
        public void RegisterOnEnter(GameState state, Action callback)
        {
            if (!_onEnterCallbacks.ContainsKey(state))
                _onEnterCallbacks[state] = null;
            _onEnterCallbacks[state] += callback;
        }

        /// <summary>Registra acción a ejecutar al SALIR de un estado.</summary>
        public void RegisterOnExit(GameState state, Action callback)
        {
            if (!_onExitCallbacks.ContainsKey(state))
                _onExitCallbacks[state] = null;
            _onExitCallbacks[state] += callback;
        }

        public void UnregisterOnEnter(GameState state, Action callback)
        {
            if (_onEnterCallbacks.ContainsKey(state))
                _onEnterCallbacks[state] -= callback;
        }

        public void UnregisterOnExit(GameState state, Action callback)
        {
            if (_onExitCallbacks.ContainsKey(state))
                _onExitCallbacks[state] -= callback;
        }

        // ── Implementación interna ────────────────────────────

        private void ApplyStateChange(GameState target)
        {
            PreviousState = CurrentState;

            // OnExit del estado anterior
            if (_onExitCallbacks.TryGetValue(PreviousState, out var exitCb))
                exitCb?.Invoke();

            CurrentState = target;

            // Historial
            _history.Add(target);
            if (_history.Count > MAX_HISTORY)
                _history.RemoveAt(0);

            CoreLogger.LogSystem("GameStateManager", $"Estado: {PreviousState} → {CurrentState}");

            // Notificar via EventBus
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent
            {
                Previous = PreviousState,
                Current  = CurrentState
            });

            // OnEnter del nuevo estado
            if (_onEnterCallbacks.TryGetValue(CurrentState, out var enterCb))
                enterCb?.Invoke();
        }

        private bool IsTransitionValid(GameState from, GameState to)
        {
            return _validTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
        }

        private void OnStateChangeRequested(GameStateChangeRequestedEvent e)
        {
            RequestStateChange(e.TargetState);
        }
    }
}
