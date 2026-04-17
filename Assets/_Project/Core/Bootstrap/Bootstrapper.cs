// ============================================================
//  Bootstrapper.cs
//  Core/Bootstrap/Bootstrapper.cs
//
//  RESPONSABILIDAD ÚNICA: Orquestar la inicialización del juego.
//
//  CARACTERÍSTICAS:
//  • Se ejecuta ANTES que cualquier otro sistema (Script Execution Order)
//  • Instancia el GameManager si no existe (carga desde Resources)
//  • Inicia la secuencia de arranque ordenada
//  • Navega a la escena inicial después del init
//  • Soporte para escena de Bootstrap dedicada (recomendado)
//
//  SETUP:
//  1. Crear una escena llamada "Bootstrap" (primera en Build Settings)
//  2. Crear un GameObject vacío, añadir este script
//  3. Asignar el prefab GameManager en el Inspector
//  4. La escena Bootstrap → carga MainMenu automáticamente
//
//  ALTERNATIVA SIN ESCENA BOOTSTRAP:
//  Añadir el Bootstrapper a la escena MainMenu como primer objeto.
//  El orden de ejecución garantiza que se inicializa antes.
// ============================================================

using Core.Config;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Core.Bootstrap
{
    /// <summary>
    /// Script Execution Order: -100 (configurar en Project Settings o con el atributo)
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class Bootstrapper : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Prefabs")]
        [Tooltip("Prefab del GameManager. Si es null, se busca en Resources/Core/GameManager.")]
        [SerializeField] private GameManager _gameManagerPrefab;

        [Header("Configuración")]
        [SerializeField] private CoreConfig _config;

        [Tooltip("Si es true, la escena del Bootstrapper persiste (no se recarga).")]
        [SerializeField] private bool _persistBootstrapScene = true;

        [Tooltip("Navegar automáticamente a la escena inicial después del init.")]
        [SerializeField] private bool _loadInitialScene = true;

        // ── Estado ────────────────────────────────────────────

        private static bool _hasBootstrapped = false;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            // Evitar doble bootstrap (ej: si la escena se recarga)
            if (_hasBootstrapped)
            {
                CoreLogger.LogSystemDebug("Bootstrapper", "Ya ejecutado. Destruyendo duplicado.");
                Destroy(gameObject);
                return;
            }

            CoreLogger.LogSystem("Bootstrapper", "Iniciando secuencia de arranque...");

            ValidateConfig();

            if (_persistBootstrapScene)
                DontDestroyOnLoad(gameObject);

            RunBootstrap();
        }

        // ── Bootstrap Sequence ────────────────────────────────

        private void RunBootstrap()
        {
            // Paso 1: Configurar Logger
            ConfigureLogger();

            // Paso 2: Asegurar que GameManager exista
            var gameManager = EnsureGameManager();
            if (gameManager == null)
            {
                CoreLogger.LogError("[Bootstrapper] No se pudo crear el GameManager. Abortando.");
                return;
            }

            // Paso 3: Inicializar el GameManager (y sus subsistemas)
            gameManager.Initialize();

            // Paso 4: Marcar como completado
            _hasBootstrapped = true;

            CoreLogger.LogSystem("Bootstrapper", "Arranque completado ✓");

            // Paso 5: Navegar a la escena inicial
            if (_loadInitialScene && !string.IsNullOrEmpty(_config.InitialScene))
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                // Solo navegar si no estamos ya en la escena destino
                if (currentScene != _config.InitialScene)
                {
                    CoreLogger.LogSystem("Bootstrapper", $"Navegando a escena inicial: '{_config.InitialScene}'");
                    gameManager.SceneLoader.LoadScene(_config.InitialScene);
                }
                else
                {
                    CoreLogger.LogSystemDebug("Bootstrapper",
                        $"Ya en escena inicial '{_config.InitialScene}', sin navegación."
                    );
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────

        private GameManager EnsureGameManager()
        {
            // Si ya existe una instancia (ej: DontDestroyOnLoad), usarla
            if (GameManager.Instance != null)
            {
                CoreLogger.LogSystemDebug("Bootstrapper", "GameManager ya existe.");
                return GameManager.Instance;
            }

            // Intentar instanciar desde prefab asignado
            if (_gameManagerPrefab != null)
            {
                var gm = Instantiate(_gameManagerPrefab);
                gm.name = "GameManager";
                CoreLogger.LogSystemDebug("Bootstrapper", "GameManager creado desde prefab asignado.");
                return gm;
            }

            // Fallback: buscar en Resources
            var prefabFromResources = Resources.Load<GameManager>("Core/GameManager");
            if (prefabFromResources != null)
            {
                var gm = Instantiate(prefabFromResources);
                gm.name = "GameManager";
                CoreLogger.LogSystemDebug("Bootstrapper", "GameManager cargado desde Resources.");
                return gm;
            }

            // Último recurso: crear GameObject vacío con el componente
            CoreLogger.LogWarning(
                "[Bootstrapper] Prefab de GameManager no encontrado. " +
                "Creando GameManager vacío (sin CoreConfig asignado)."
            );
            var fallback = new GameObject("GameManager");
            return fallback.AddComponent<GameManager>();
        }

        private void ConfigureLogger()
        {
            CoreLogger.DebugMode = _config != null && _config.DebugMode;
            CoreLogger.LogSystem("Bootstrapper", $"DebugMode = {CoreLogger.DebugMode}");
        }

        private void ValidateConfig()
        {
            if (_config == null)
            {
                CoreLogger.LogError(
                    "[Bootstrapper] CoreConfig no asignado. " +
                    "Arrastra un CoreConfig al campo _config del Bootstrapper."
                );
            }
        }

        // ── Editor Utility ────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>
        /// Permite reiniciar el estado del Bootstrapper desde el Editor.
        /// Llamado por CoreEditorWindow al hacer "Reiniciar Juego".
        /// </summary>
        [ContextMenu("Reset Bootstrap Flag")]
        public void ResetBootstrapFlag()
        {
            _hasBootstrapped = false;
            CoreLogger.LogSystemDebug("Bootstrapper", "Flag de bootstrap reseteado.");
        }
#endif
    }
}
