// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_GameModeManager.cs  (REEMPLAZA el anterior)║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    - Eliminado: GetComponentsInChildren + SetActive(false)║
// ║    + [SerializeField] GameModeBase[] _allModes           ║
// ║      Ahora los modos se asignan en Inspector directamente║
// ║    + Los GameObjects de modo NUNCA se desactivan         ║
// ║      solo se llama ResetGame() y StartGame()             ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    1. Crear un GameObject "GameModeManager"              ║
// ║    2. Añadir GameModeManager.cs                          ║
// ║    3. Crear un GameObject "CTF_Mode" (u otro)            ║
// ║       con GameModeBase.cs en la misma escena             ║
// ║    4. Asignar "CTF_Mode" al campo _defaultMode           ║
// ║       (y también al array _allModes si tienes varios)    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GMF
{
    [DisallowMultipleComponent]
    public class GameModeManager : MonoBehaviour
    {
        [Header("Modo por defecto")]
        [Tooltip("GameModeBase que se activa cuando el Core entra en estado Playing.")]
        [SerializeField] private GameModeBase _defaultMode;

        [Header("Todos los modos disponibles (opcional)")]
        [Tooltip("Para Activate() por nombre. No es necesario si solo hay un modo.")]
        [SerializeField] private GameModeBase[] _allModes = System.Array.Empty<GameModeBase>();

        public GameModeBase ActiveMode { get; private set; }

        private readonly Dictionary<string, GameModeBase> _registry = new();

        private void Awake()
        {
            // Registrar el modo default
            if (_defaultMode != null)
                _registry[_defaultMode.ModeID] = _defaultMode;

            // Registrar modos adicionales
            foreach (var mode in _allModes)
                if (mode != null)
                    _registry[mode.ModeID] = mode;

            CoreLogger.LogSystem("GameModeManager",
                $"{_registry.Count} modo(s) registrado(s).");
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
            if (e.Current == Core.GameState.Playing && ActiveMode == null)
                Activate(_defaultMode?.ModeID ?? string.Empty);
        }

        // ── API Pública ───────────────────────────────────────

        public bool Activate(string modeID)
        {
            GameModeBase next = null;

            if (!string.IsNullOrEmpty(modeID) && _registry.TryGetValue(modeID, out next))
            {
                // encontrado
            }
            else if (_defaultMode != null)
            {
                if (!string.IsNullOrEmpty(modeID))
                    CoreLogger.LogWarning(
                        $"[GameModeManager] Modo '{modeID}' no registrado. Usando default.");
                next = _defaultMode;
            }
            else
            {
                CoreLogger.LogError("[GameModeManager] No hay modo disponible.");
                return false;
            }

            // Resetear modo anterior
            if (ActiveMode != null && ActiveMode != next)
                ActiveMode.ResetGame();

            ActiveMode = next;
            ActiveMode.StartGame();

            CoreLogger.LogSystem("GameModeManager",
                $"Modo activo: '{ActiveMode.ModeID}'");
            return true;
        }

        public void Register(GameModeBase mode)
        {
            if (mode == null) return;
            _registry[mode.ModeID] = mode;
        }

        public GameModeBase Get(string modeID)
            => _registry.TryGetValue(modeID, out var m) ? m : null;

        /// <summary>Inicia directamente desde editor o botón de lobby.</summary>
        [ContextMenu("Start Default Mode")]
        public void StartDefault() => Activate(_defaultMode?.ModeID ?? string.Empty);
    }
}
