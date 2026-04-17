// ============================================================
//  GameManager.cs
//  Core/GameManager/GameManager.cs
//
//  RESPONSABILIDAD ÚNICA: Punto de acceso central al Core.
//  Posee y expone todos los subsistemas del Core System.
//  NO contiene lógica de gameplay.
//
//  CARACTERÍSTICAS:
//  • Singleton seguro (no usa FindObjectOfType en caliente)
//  • DontDestroyOnLoad automático
//  • Inicialización ordenada de subsistemas
//  • Acceso público de solo lectura a cada subsistema
//  • Toggle UseNetworking para soporte multi/single player
//
//  USO:
//    GameManager.Instance.StateManager.RequestStateChange(GameState.Playing);
//    GameManager.Instance.TimeManager.Pause();
//    GameManager.Instance.SceneLoader.LoadScene("Game");
// ============================================================

using Core.Config;
using Core.Debug;
using Core.Events;
using Core.SceneManagement;
using UnityEngine;

// El alias evita ambigüedad con UnityEngine.Time
using CoreTime = Core.Time.TimeManager;

namespace Core
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private CoreConfig _config;

        [Header("Debug")]
        [SerializeField] private bool _overrideDebugMode = false;

        // ── Subsistemas (acceso público, solo lectura) ─────────

        public GameStateManager  StateManager { get; private set; }
        public CoreTime          TimeManager  { get; private set; }
        public SceneLoader       SceneLoader  { get; private set; }

        // ── Flags ─────────────────────────────────────────────

        public bool IsInitialized { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (!EnsureSingleton()) return;
            ValidateConfig();
            ApplyDebugConfig();
        }

        private void Start()
        {
            // La inicialización real la maneja el Bootstrapper.
            // Start solo inicia el proceso si NO hay Bootstrapper en escena.
            if (!IsInitialized)
            {
                CoreLogger.LogSystemDebug("GameManager",
                    "No se encontró Bootstrapper. Inicializando directamente."
                );
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StateManager?.Dispose();
                Instance = null;
            }
        }

        // ── Inicialización ────────────────────────────────────

        /// <summary>
        /// Inicializa todos los subsistemas del Core en orden.
        /// Llamado por el Bootstrapper. No llamar manualmente.
        /// </summary>
        internal void Initialize()
        {
            if (IsInitialized)
            {
                CoreLogger.LogWarning("[GameManager] Ya inicializado. Ignorando llamada duplicada.");
                return;
            }

            CoreLogger.LogSystem("GameManager", "Inicializando Core System...");

            // Orden de inicialización: Logger → State → Time → Scene
            InitializeStateManager();
            InitializeTimeManager();
            InitializeSceneLoader();

            IsInitialized = true;

            CoreLogger.LogSystem("GameManager", "Core System listo ✓");

            EventBus<GameInitializedEvent>.Raise(new GameInitializedEvent
            {
                Timestamp = UnityEngine.Time.realtimeSinceStartup
            });

            // Mover al primer estado real
            StateManager.RequestStateChange(GameState.MainMenu);
        }

        // ── Subsistemas ───────────────────────────────────────

        private void InitializeStateManager()
        {
            StateManager = new GameStateManager();

            // Reaccionar a cambios de estado relevantes al GameManager
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

            CoreLogger.LogSystemDebug("GameManager", "StateManager listo.");
        }

        private void InitializeTimeManager()
        {
            TimeManager = new CoreTime(_config, this);
            CoreLogger.LogSystemDebug("GameManager", "TimeManager listo.");
        }

        private void InitializeSceneLoader()
        {
            SceneLoader = new SceneLoader(_config, this);
            CoreLogger.LogSystemDebug("GameManager", "SceneLoader listo.");
        }

        // ── Reacciones a eventos ──────────────────────────────

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            switch (e.Current)
            {
                case GameState.Paused:
                    TimeManager.Pause();
                    break;

                case GameState.Playing when e.Previous == GameState.Paused:
                    TimeManager.Resume();
                    break;

                case GameState.MainMenu:
                    TimeManager.ResetTimeScale();
                    break;
            }
        }

        // ── API de conveniencia ───────────────────────────────

        /// <summary>Shorthand para cambios de estado frecuentes.</summary>
        public void RequestStateChange(GameState state)
        {
            StateManager?.RequestStateChange(state);
        }

        /// <summary>Reinicia el Core (útil para "Play Again").</summary>
        public void RestartGame()
        {
            StateManager.ForceStateChange(GameState.Initializing);
            SceneLoader.LoadScene(_config.InitialScene, () =>
            {
                StateManager.RequestStateChange(GameState.MainMenu);
            });
        }

        // ── Helpers privados ──────────────────────────────────

        private bool EnsureSingleton()
        {
            if (Instance != null && Instance != this)
            {
                CoreLogger.LogWarning("[GameManager] Instancia duplicada detectada. Destruyendo.");
                Destroy(gameObject);
                return false;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        private void ValidateConfig()
        {
            if (_config == null)
            {
                CoreLogger.LogError("[GameManager] ¡CoreConfig no asignado en el Inspector!");
                _config = ScriptableObject.CreateInstance<CoreConfig>();
                CoreLogger.LogWarning("[GameManager] Usando CoreConfig con valores por defecto.");
            }
        }

        private void ApplyDebugConfig()
        {
            CoreLogger.DebugMode = _overrideDebugMode || _config.DebugMode;
        }
    }
}
