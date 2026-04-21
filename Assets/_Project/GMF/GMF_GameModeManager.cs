// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_GameModeManager.cs                         ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • GameModeManager (MonoBehaviour)                     ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Registrar y activar modos de juego.                   ║
// ║    Un solo modo activo a la vez.                         ║
// ║    Reacciona a Core.GameStateChangedEvent.Playing        ║
// ║    para iniciar el modo por defecto.                     ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    1. Añadir al mismo GameObject que GameModeBase        ║
// ║       (o en un GameObject padre)                         ║
// ║    2. Asignar _defaultMode en Inspector                  ║
// ║    3. Los modos son hijos GameObjects con GameModeBase   ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • Llamar Activate() antes de Awake() → _registry vacío║
// ║    • _defaultMode null → partida nunca arranca           ║
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
        [Tooltip("Se activa cuando el Core entra en estado Playing.")]
        [SerializeField] private GameModeBase _defaultMode;

        public GameModeBase ActiveMode { get; private set; }

        private readonly Dictionary<string, GameModeBase> _registry = new();

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            // Auto-registro de todos los modos hijos en la jerarquía
            foreach (var mode in GetComponentsInChildren<GameModeBase>(true))
            {
                _registry[mode.ModeID] = mode;
                mode.gameObject.SetActive(false);
                CoreLogger.LogSystemDebug("GameModeManager",
                    $"Modo registrado: '{mode.ModeID}'");
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
            if (e.Current == Core.GameState.Playing && ActiveMode == null)
                Activate(_defaultMode?.ModeID ?? string.Empty);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Activa un modo por su ModeID.</summary>
        public bool Activate(string modeID)
        {
            GameModeBase next = null;

            if (!string.IsNullOrEmpty(modeID) && _registry.TryGetValue(modeID, out next))
            {
                // Encontrado
            }
            else if (_defaultMode != null)
            {
                CoreLogger.LogWarning(
                    $"[GameModeManager] Modo '{modeID}' no encontrado. Usando default.");
                next = _defaultMode;
            }
            else
            {
                CoreLogger.LogError("[GameModeManager] No hay modo de juego disponible.");
                return false;
            }

            // Desactivar el modo anterior
            if (ActiveMode != null)
            {
                ActiveMode.ResetGame();
                ActiveMode.gameObject.SetActive(false);
            }

            // Activar el nuevo
            ActiveMode = next;
            ActiveMode.gameObject.SetActive(true);
            ActiveMode.StartGame();

            CoreLogger.LogSystem("GameModeManager",
                $"Modo activo: '{ActiveMode.ModeID}'");
            return true;
        }

        /// <summary>Registra un modo externo (si no está en la jerarquía).</summary>
        public void Register(GameModeBase mode)
        {
            if (mode == null) return;
            _registry[mode.ModeID] = mode;
        }

        public GameModeBase Get(string modeID)
            => _registry.TryGetValue(modeID, out var m) ? m : null;
    }
}
