// ============================================================
//  PlayerAuthority.cs
//  PlayerSystem/Authority/PlayerAuthority.cs
//
//  RESPONSABILIDAD ÚNICA: Modelo de autoridad de red del jugador.
//
//  Abstrae si un jugador tiene autoridad sobre su propia lógica.
//  Todos los sistemas del jugador consultan HasAuthority e IsLocalPlayer
//  antes de ejecutar lógica crítica.
//
//  REGLAS DE AUTORIDAD:
//  • Offline:           HasAuthority=true, IsLocalPlayer=true siempre
//  • Host (local):      HasAuthority=true, IsLocalPlayer=true
//  • Client (remoto):   HasAuthority=false, IsLocalPlayer=false
//  • DedicatedServer:   HasAuthority=true,  IsLocalPlayer=false
//
//  Esta clase NO implementa Netcode directamente.
//  Es un contrato/interfaz que el sistema de networking rellenará.
//  En offline, los valores se setean automáticamente a local=true.
// ============================================================

using Core.Debug;
using Player.Config;
using Player.Events;
using Core.Events;
using UnityEngine;

namespace Player.Authority
{
    [DisallowMultipleComponent]
    public class PlayerAuthority : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Identificación")]
        [Tooltip("ID único del jugador en la sesión. 0 = local/offline.")]
        [SerializeField] private int _playerID = 0;

        // ── Autoridad ─────────────────────────────────────────

        /// <summary>
        /// Este objeto tiene autoridad sobre su estado de gameplay.
        /// En offline/host siempre true para el jugador local.
        /// En client, false para jugadores remotos.
        /// </summary>
        public bool HasAuthority   { get; private set; }

        /// <summary>
        /// Este objeto representa al jugador local (quien tiene el teclado/gamepad).
        /// En servidor dedicado, todos los jugadores tienen IsLocalPlayer=false.
        /// </summary>
        public bool IsLocalPlayer  { get; private set; }

        /// <summary>ID único del jugador en la sesión actual.</summary>
        public int  PlayerID       => _playerID;

        // ── Inicialización ────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                CoreLogger.LogError("[PlayerAuthority] PlayerConfig no asignado.");
                return;
            }

            InitializeAuthority();
        }

        private void InitializeAuthority()
        {
            switch (_config.NetworkMode)
            {
                case NetworkMode.Offline:
                    // Singleplayer puro: autoridad local total
                    HasAuthority  = true;
                    IsLocalPlayer = true;
                    break;

                case NetworkMode.Host:
                    // El host tiene autoridad y es jugador local
                    HasAuthority  = true;
                    IsLocalPlayer = true;
                    break;

                case NetworkMode.Client:
                    // Por defecto en Client: sin autoridad hasta que el servidor lo confirme.
                    // El sistema de red (Netcode/Mirror) debe llamar a SetNetworkAuthority()
                    // una vez que el jugador es spawneado como "owner".
                    HasAuthority  = false;
                    IsLocalPlayer = false;
                    break;

                case NetworkMode.DedicatedServer:
                    // El servidor tiene autoridad pero no es un jugador local
                    HasAuthority  = true;
                    IsLocalPlayer = false;
                    break;
            }

            CoreLogger.LogSystemDebug("PlayerAuthority",
                $"[P{_playerID}] Mode={_config.NetworkMode} | " +
                $"HasAuthority={HasAuthority} | IsLocalPlayer={IsLocalPlayer}"
            );
        }

        // ── API para sistemas de red ──────────────────────────

        /// <summary>
        /// Llamado por el sistema de Netcode al confirmar ownership.
        /// En Netcode for GameObjects: llamar desde OnNetworkSpawn().
        /// En Mirror: llamar desde OnStartLocalPlayer().
        /// </summary>
        public void SetNetworkAuthority(int playerID, bool hasAuthority, bool isLocalPlayer)
        {
            _playerID     = playerID;
            HasAuthority  = hasAuthority;
            IsLocalPlayer = isLocalPlayer;

            CoreLogger.LogSystem("PlayerAuthority",
                $"[P{_playerID}] Autoridad de red actualizada → " +
                $"HasAuthority={HasAuthority} | IsLocalPlayer={IsLocalPlayer}"
            );

            EventBus<PlayerReadyEvent>.Raise(new PlayerReadyEvent
            {
                PlayerID      = _playerID,
                IsLocalPlayer = isLocalPlayer
            });
        }

        /// <summary>
        /// Forza modo offline (útil para pruebas o modos sin red).
        /// </summary>
        public void ForceOfflineAuthority(int playerID = 0)
        {
            SetNetworkAuthority(playerID, hasAuthority: true, isLocalPlayer: true);
        }

        // ── Helpers de validación ─────────────────────────────

        /// <summary>Loguea advertencia si no hay autoridad. Retorna false si no tiene.</summary>
        public bool AssertAuthority(string context)
        {
            if (!HasAuthority)
            {
                CoreLogger.LogSystemDebug("PlayerAuthority",
                    $"[P{_playerID}] '{context}' ignorado: sin autoridad."
                );
                return false;
            }
            return true;
        }

        /// <summary>Loguea advertencia si no es jugador local. Retorna false si no lo es.</summary>
        public bool AssertLocalPlayer(string context)
        {
            if (!IsLocalPlayer)
            {
                CoreLogger.LogSystemDebug("PlayerAuthority",
                    $"[P{_playerID}] '{context}' ignorado: no es jugador local."
                );
                return false;
            }
            return true;
        }
    }
}
