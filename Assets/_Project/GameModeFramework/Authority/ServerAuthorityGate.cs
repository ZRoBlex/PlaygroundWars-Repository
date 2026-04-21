// ============================================================
//  ServerAuthorityGate.cs
//  GameModeFramework/Authority/ServerAuthorityGate.cs
//
//  RESPONSABILIDAD ÚNICA: Garantizar que la lógica crítica del
//  GameMode solo se ejecute en el servidor/host.
//
//  ARQUITECTURA MULTIPLAYER:
//  ┌────────────────────────────────────────────────────────┐
//  │                                                        │
//  │  SERVIDOR / HOST                                       │
//  │  ┌──────────────────────────────────────────────────┐  │
//  │  │  ServerAuthorityGate._isAuthority = true         │  │
//  │  │  GameModeBase.StartGame()    ← solo corre aquí   │  │
//  │  │  RuleEngine.OnEvent()        ← solo aquí         │  │
//  │  │  WinConditionEvaluator       ← solo aquí         │  │
//  │  │  ScoreSystem.AddScore()      ← solo aquí         │  │
//  │  └──────────────────────────────────────────────────┘  │
//  │                                                        │
//  │  CLIENTE                                               │
//  │  ┌──────────────────────────────────────────────────┐  │
//  │  │  ServerAuthorityGate._isAuthority = false        │  │
//  │  │  Solo escucha eventos via EventBus               │  │
//  │  │  Solo actualiza UI / visuals                     │  │
//  │  │  NUNCA llama a RuleEngine ni ScoreSystem         │  │
//  │  └──────────────────────────────────────────────────┘  │
//  │                                                        │
//  └────────────────────────────────────────────────────────┘
//
//  EN MODO OFFLINE: _isAuthority = true siempre.
//  EN MODO HOST:    _isAuthority = true para el host.
//  EN MODO CLIENT:  _isAuthority = false para el cliente.
//
//  INTEGRACIÓN:
//  Añadir este componente al mismo GO que GameModeBase.
//  GameModeBase.StartGame() consulta Authority.IsAuthority.
//  El sistema de red (Netcode/Mirror) llama a SetAuthority()
//  al conectar/desconectar.
// ============================================================

using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GameMode.Framework.Authority
{
    [DisallowMultipleComponent]
    public class ServerAuthorityGate : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Autoridad inicial")]
        [Tooltip("Offline/Host: true. Cliente puro: false.")]
        [SerializeField] private bool _isAuthority = true;

        [Header("Modo de Red")]
        [SerializeField] private NetworkMode _networkMode = NetworkMode.Offline;

        // ── Estado ────────────────────────────────────────────

        public static ServerAuthorityGate Instance { get; private set; }

        public bool IsAuthority => _isAuthority;
        public NetworkMode Mode => _networkMode;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            ApplyNetworkMode(_networkMode);
            CoreLogger.LogSystem("ServerAuthority",
                $"Autoridad: {_isAuthority} | Modo: {_networkMode}");
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Llamado por el sistema de red al confirmar el rol de este cliente.
        /// Netcode for GameObjects: desde OnNetworkSpawn() del host.
        /// Mirror: desde OnStartServer() / OnStartClient().
        /// </summary>
        public void SetAuthority(bool hasAuthority, NetworkMode mode)
        {
            _isAuthority  = hasAuthority;
            _networkMode  = mode;

            CoreLogger.LogSystem("ServerAuthority",
                $"Autoridad actualizada: {hasAuthority} | Modo: {mode}");

            EventBus<AuthorityChangedEvent>.Raise(new AuthorityChangedEvent
            {
                HasAuthority = hasAuthority,
                Mode         = mode
            });
        }

        public void SetOffline()   => SetAuthority(true,  NetworkMode.Offline);
        public void SetHost()      => SetAuthority(true,  NetworkMode.Host);
        public void SetClient()    => SetAuthority(false, NetworkMode.Client);

        private void ApplyNetworkMode(NetworkMode mode)
        {
            _isAuthority = mode != NetworkMode.Client;
        }
    }

    // ── Enums y evento ────────────────────────────────────────

    public enum NetworkMode { Offline, Host, Client, DedicatedServer }

    public struct AuthorityChangedEvent
    {
        public bool        HasAuthority;
        public NetworkMode Mode;
    }

    // ── Helper estático para validar autoridad en cualquier sitio ─

    public static class Authority
    {
        /// <summary>
        /// Retorna true si hay autoridad para ejecutar lógica de servidor.
        /// En offline siempre true. En cliente remoto siempre false.
        /// </summary>
        public static bool HasAuthority =>
            ServerAuthorityGate.Instance?.IsAuthority ?? true;

        /// <summary>Guard pattern: lanza advertencia y retorna false si no hay autoridad.</summary>
        public static bool Assert(string context)
        {
            if (HasAuthority) return true;
            CoreLogger.LogSystemDebug("Authority",
                $"'{context}' ignorado: sin autoridad de servidor.");
            return false;
        }
    }
}
