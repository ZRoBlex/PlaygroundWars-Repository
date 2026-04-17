// ============================================================
//  PlayerRespawn.cs
//  PlayerSystem/Respawn/PlayerRespawn.cs
//
//  RESPONSABILIDAD ÚNICA: Manejar el respawn del jugador.
//
//  CARACTERÍSTICAS:
//  • Delay configurable antes del respawn
//  • Pool de spawn points con selección aleatoria
//  • Integración con GameMode (spawn points externos)
//  • Reset completo de estado del jugador al respawn
//  • Deshabilita sistemas durante el tiempo de muerte
//  • Solo la autoridad decide cuándo y dónde hacer respawn
//  • Soporte para AllowRespawn=false (GameOver permanente)
// ============================================================

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using Player.Health;
using Player.Movement;
using Player.Camera;
using UnityEngine;

namespace Player.Respawn
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerRespawn : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Spawn Points")]
        [Tooltip("Lista de posibles spawn points. Si está vacía, usa la posición inicial.")]
        [SerializeField] private List<Transform> _spawnPoints = new();

        [Header("Visuals")]
        [Tooltip("Raíz del mesh del jugador para ocultar durante la muerte.")]
        [SerializeField] private GameObject _playerVisuals;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority      _authority;
        private PlayerHealth         _health;
        private PlayerMovement       _movement;
        private PlayerCameraController _camera;

        // ── Estado ────────────────────────────────────────────

        public bool IsRespawning    { get; private set; }
        public int  RespawnCount    { get; private set; }

        private Vector3    _defaultSpawnPosition;
        private Quaternion _defaultSpawnRotation;
        private Coroutine  _respawnCoroutine;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _health    = GetComponent<PlayerHealth>();
            _movement  = GetComponent<PlayerMovement>();
            _camera    = GetComponent<PlayerCameraController>();

            _defaultSpawnPosition = transform.position;
            _defaultSpawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);

            if (_respawnCoroutine != null)
                StopCoroutine(_respawnCoroutine);
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            // Solo la autoridad gestiona el respawn
            if (!_authority.HasAuthority) return;

            HandleDeath();
        }

        // ── Muerte ────────────────────────────────────────────

        private void HandleDeath()
        {
            CoreLogger.LogSystem("PlayerRespawn", $"[P{_authority.PlayerID}] Jugador muerto.");

            // Deshabilitar sistemas durante la muerte
            DisablePlayerSystems();

            if (_config != null && _config.AllowRespawn)
                _respawnCoroutine = StartCoroutine(RespawnRoutine());
            else
                CoreLogger.LogSystem("PlayerRespawn",
                    $"[P{_authority.PlayerID}] AllowRespawn=false. Sin respawn."
                );
        }

        // ── Coroutine de respawn ──────────────────────────────

        private IEnumerator RespawnRoutine()
        {
            IsRespawning = true;
            float delay  = _config != null ? _config.RespawnDelay : 3f;

            Vector3    spawnPos = GetSpawnPosition();
            Quaternion spawnRot = GetSpawnRotation();

            // Notificar que el respawn está por ocurrir
            EventBus<PlayerPreRespawnEvent>.Raise(new PlayerPreRespawnEvent
            {
                PlayerID      = _authority.PlayerID,
                SpawnPosition = spawnPos,
                Delay         = delay
            });

            CoreLogger.LogSystem("PlayerRespawn",
                $"[P{_authority.PlayerID}] Respawn en {delay}s en {spawnPos}"
            );

            yield return new WaitForSeconds(delay);

            ExecuteRespawn(spawnPos, spawnRot);
        }

        // ── Ejecutar respawn ──────────────────────────────────

        private void ExecuteRespawn(Vector3 position, Quaternion rotation)
        {
            // Reset de salud
            if (_config != null && _config.ResetHealthOnRespawn)
                _health?.ResetHealth();

            // Teleport
            _movement?.Teleport(position, rotation);

            // Cámara
            _camera?.SetRotation(rotation.eulerAngles.y);

            // Re-habilitar sistemas
            EnablePlayerSystems();

            RespawnCount++;
            IsRespawning = false;

            CoreLogger.LogSystem("PlayerRespawn",
                $"[P{_authority.PlayerID}] Respawn #{RespawnCount} completado en {position}"
            );

            EventBus<PlayerRespawnedEvent>.Raise(new PlayerRespawnedEvent
            {
                PlayerID      = _authority.PlayerID,
                SpawnPosition = position
            });
        }

        // ── Spawn Points ──────────────────────────────────────

        private Vector3 GetSpawnPosition()
        {
            if (_spawnPoints == null || _spawnPoints.Count == 0)
                return _defaultSpawnPosition;

            // Eliminar nulls
            _spawnPoints.RemoveAll(sp => sp == null);
            if (_spawnPoints.Count == 0)
                return _defaultSpawnPosition;

            return _spawnPoints[Random.Range(0, _spawnPoints.Count)].position;
        }

        private Quaternion GetSpawnRotation()
        {
            if (_spawnPoints == null || _spawnPoints.Count == 0)
                return _defaultSpawnRotation;

            // Toma la rotación del spawn point seleccionado (simplificado)
            return _spawnPoints[0].rotation;
        }

        /// <summary>
        /// Registra spawn points en runtime (ej: los manda el GameMode).
        /// </summary>
        public void SetSpawnPoints(List<Transform> points)
        {
            _spawnPoints = points ?? new List<Transform>();
            CoreLogger.LogSystemDebug("PlayerRespawn",
                $"[P{_authority.PlayerID}] SpawnPoints actualizados: {_spawnPoints.Count}"
            );
        }

        /// <summary>Añade un spawn point dinámicamente.</summary>
        public void AddSpawnPoint(Transform point)
        {
            if (point != null)
                _spawnPoints.Add(point);
        }

        // ── Sistemas ──────────────────────────────────────────

        private void DisablePlayerSystems()
        {
            // Visuals
            if (_playerVisuals != null)
                _playerVisuals.SetActive(false);

            // Input → no puede controlar durante muerte
            var input = GetComponent<Player.Input.PlayerInput>();
            input?.DisableInput();

            // Collider
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            CoreLogger.LogSystemDebug("PlayerRespawn", $"[P{_authority.PlayerID}] Sistemas deshabilitados.");
        }

        private void EnablePlayerSystems()
        {
            if (_playerVisuals != null)
                _playerVisuals.SetActive(true);

            var input = GetComponent<Player.Input.PlayerInput>();
            input?.EnableInput();

            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            CoreLogger.LogSystemDebug("PlayerRespawn", $"[P{_authority.PlayerID}] Sistemas habilitados.");
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Fuerza un respawn inmediato en la posición actual.
        /// Útil para editor tools o comandos de debug.
        /// </summary>
        public void ForceRespawn()
        {
            if (_respawnCoroutine != null)
                StopCoroutine(_respawnCoroutine);

            ExecuteRespawn(GetSpawnPosition(), GetSpawnRotation());
        }

        /// <summary>
        /// Fuerza un respawn en una posición específica.
        /// </summary>
        public void ForceRespawnAt(Vector3 position, Quaternion rotation)
        {
            if (_respawnCoroutine != null)
                StopCoroutine(_respawnCoroutine);

            ExecuteRespawn(position, rotation);
        }
    }
}
