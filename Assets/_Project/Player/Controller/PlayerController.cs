// ============================================================
//  PlayerController.cs
//  PlayerSystem/Controller/PlayerController.cs
//
//  RESPONSABILIDAD ÚNICA: Punto central del jugador.
//  Coordina e inicializa todos los subsistemas del jugador.
//  NO contiene lógica de gameplay ni de movimiento.
//
//  CARACTERÍSTICAS:
//  • Referencia única a todos los sistemas del jugador
//  • Inicialización ordenada y segura
//  • Activación/desactivación de sistemas individual
//  • Toggle UseNetworking sin modificar gameplay code
//  • Soporte para Offline / Host / Client / DedicatedServer
//  • Punto de entrada para sistemas externos (weapons, abilities, etc.)
// ============================================================

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Camera;
using Player.Config;
using Player.Events;
using Player.Health;
using Player.Movement;
using Player.Respawn;
using UnityEngine;

// Alias para evitar ambigüedad con UnityEngine.Input
using PlayerInputSystem = Player.Input.PlayerInput;

namespace Player.Controller
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)] // Inicializar antes que los sistemas individuales
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Sistemas (auto-detect si están en el mismo GO)")]
        [SerializeField] private PlayerAuthority       _authority;
        [SerializeField] private PlayerInputSystem     _input;
        [SerializeField] private PlayerMovement        _movement;
        [SerializeField] private PlayerCameraController _camera;
        [SerializeField] private PlayerHealth          _health;
        [SerializeField] private PlayerRespawn         _respawn;

        [Header("Debug")]
        [SerializeField] private bool _logInitialization = true;

        // ── Propiedades públicas ──────────────────────────────

        public PlayerAuthority        Authority => _authority;
        public PlayerInputSystem      Input     => _input;
        public PlayerMovement         Movement  => _movement;
        public PlayerCameraController Camera    => _camera;
        public PlayerHealth           Health    => _health;
        public PlayerRespawn          Respawn   => _respawn;
        public PlayerConfig           Config    => _config;

        public bool IsInitialized { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            ValidateConfig();
            AutoDetectSystems();
            InitializeSystems();
        }

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<PlayerRespawnedEvent>.Subscribe(OnPlayerRespawned);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<PlayerRespawnedEvent>.Unsubscribe(OnPlayerRespawned);
        }

        // ── Inicialización ────────────────────────────────────

        private void InitializeSystems()
        {
            if (_logInitialization)
                CoreLogger.LogSystem("PlayerController", "Inicializando sistemas del jugador...");

            // 1. Authority (primero: otros sistemas dependen de PlayerID e IsLocalPlayer)
            if (_authority == null)
            {
                CoreLogger.LogError("[PlayerController] PlayerAuthority no encontrado. Abortando init.");
                return;
            }

            // En modo offline: forzar autoridad local
            if (_config != null && !_config.UseNetworking)
                _authority.ForceOfflineAuthority();

            // 2. Health
            _health?.Initialize();

            // 3. Sistemas solo activos para jugadores locales
            if (_authority.IsLocalPlayer)
            {
                EnableLocalPlayerSystems();
            }
            else
            {
                // Jugador remoto: deshabilitar input y cámara
                DisableLocalOnlySystems();
            }

            IsInitialized = true;

            if (_logInitialization)
                CoreLogger.LogSystem("PlayerController",
                    $"Jugador P{_authority.PlayerID} listo | " +
                    $"IsLocal={_authority.IsLocalPlayer} | " +
                    $"HasAuthority={_authority.HasAuthority} | " +
                    $"Mode={(_config != null ? _config.NetworkMode.ToString() : "N/A")}"
                );

            EventBus<PlayerReadyEvent>.Raise(new PlayerReadyEvent
            {
                PlayerID      = _authority.PlayerID,
                IsLocalPlayer = _authority.IsLocalPlayer
            });
        }

        // ── Activación de sistemas ────────────────────────────

        private void EnableLocalPlayerSystems()
        {
            SetSystemEnabled(_input,    true);
            SetSystemEnabled(_movement, true);
            SetSystemEnabled(_camera,   true);
        }

        private void DisableLocalOnlySystems()
        {
            // Input y cámara nunca activos en jugadores remotos
            SetSystemEnabled(_input,  false);
            SetSystemEnabled(_camera, false);

            // El movimiento puede estar activo (para aplicar posición recibida del servidor)
            SetSystemEnabled(_movement, true);
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            CoreLogger.LogSystemDebug("PlayerController",
                $"[P{_authority.PlayerID}] Procesando muerte."
            );

            // El PlayerRespawn.cs maneja el resto
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            CoreLogger.LogSystemDebug("PlayerController",
                $"[P{_authority.PlayerID}] Respawn completado."
            );
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Fuerza un respawn inmediato. Útil para debug o comandos de admin.
        /// </summary>
        public void ForceRespawn() => _respawn?.ForceRespawn();

        /// <summary>
        /// Aplica daño al jugador (respeta el modelo de autoridad).
        /// Desde sistemas externos (armas, trampas, etc.)
        /// </summary>
        public void RequestDamage(float amount, int sourceID, UnityEngine.Vector3 hitPoint, UnityEngine.Vector3 hitNormal)
        {
            EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
            {
                TargetPlayerID = _authority.PlayerID,
                SourcePlayerID = sourceID,
                Amount         = amount,
                HitPoint       = hitPoint,
                HitNormal      = hitNormal
            });
        }

        /// <summary>
        /// Cura al jugador (respeta el modelo de autoridad).
        /// </summary>
        public void RequestHeal(float amount) => _health?.Heal(amount);

        /// <summary>Deshabilita toda entrada del jugador (cutscenes, menús, etc.).</summary>
        public void DisableInput()  => _input?.DisableInput();

        /// <summary>Reactiva la entrada del jugador.</summary>
        public void EnableInput()   => _input?.EnableInput();

        /// <summary>Actualiza el NetworkMode en tiempo de ejecución.</summary>
        public void SetNetworkAuthority(int playerID, bool hasAuthority, bool isLocalPlayer)
        {
            _authority?.SetNetworkAuthority(playerID, hasAuthority, isLocalPlayer);

            if (isLocalPlayer)
                EnableLocalPlayerSystems();
            else
                DisableLocalOnlySystems();
        }

        // ── Helpers ───────────────────────────────────────────

        private void SetSystemEnabled(MonoBehaviour system, bool active)
        {
            if (system != null)
                system.enabled = active;
        }

        private void AutoDetectSystems()
        {
            if (_authority == null) _authority = GetComponent<PlayerAuthority>();
            if (_input     == null) _input     = GetComponent<PlayerInputSystem>();
            if (_movement  == null) _movement  = GetComponent<PlayerMovement>();
            if (_camera    == null) _camera    = GetComponent<PlayerCameraController>();
            if (_health    == null) _health    = GetComponent<PlayerHealth>();
            if (_respawn   == null) _respawn   = GetComponent<PlayerRespawn>();
        }

        private void ValidateConfig()
        {
            if (_config == null)
                CoreLogger.LogError("[PlayerController] PlayerConfig no asignado. Muchos sistemas no funcionarán.");
        }
    }
}
