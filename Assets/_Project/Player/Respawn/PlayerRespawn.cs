// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: PlayerRespawn.cs  (REEMPLAZA el anterior)      ║
// ║                                                          ║
// ║  CAMBIOS vs versión anterior:                            ║
// ║    + Busca GMFSpawnArea del equipo del jugador           ║
// ║    + Llama TryGetSpawnPosition() para zona libre         ║
// ║    + ResetFull() para cambio de equipo (sin kill)        ║
// ║    + ForceRespawnSilent() — respawn sin PlayerDiedEvent  ║
// ║      (usado por GameModeBase en warm-up)                 ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF;
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

        [Header("Spawn Points fallback")]
        [Tooltip("Solo se usan si no hay GMFSpawnArea en la escena.")]
        [SerializeField] private List<Transform> _fallbackSpawnPoints = new();

        [Header("Visuals")]
        [SerializeField] private GameObject _playerVisuals;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority         _authority;
        private PlayerHealth            _health;
        private PlayerMovement_Fixed    _movement;
        private PlayerCameraController  _camera;

        // ── Estado ────────────────────────────────────────────

        public bool IsRespawning { get; private set; }
        public int  RespawnCount { get; private set; }

        private Vector3    _defaultPos;
        private Quaternion _defaultRot;
        private Coroutine  _respawnCoro;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _health    = GetComponent<PlayerHealth>();
            _movement  = GetComponent<PlayerMovement_Fixed>();
            _camera    = GetComponent<PlayerCameraController>();

            _defaultPos = transform.position;
            _defaultRot = transform.rotation;
        }

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            if (_respawnCoro != null) StopCoroutine(_respawnCoro);
        }

        // ── Muerte normal ─────────────────────────────────────

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (!_authority.HasAuthority) return;

            DisableSystems();

            if (_config != null && _config.AllowRespawn)
                _respawnCoro = StartCoroutine(RespawnAfterDelay(_config.RespawnDelay));
        }

        private IEnumerator RespawnAfterDelay(float delay)
        {
            IsRespawning = true;

            EventBus<PlayerPreRespawnEvent>.Raise(new PlayerPreRespawnEvent
            {
                PlayerID      = _authority.PlayerID,
                SpawnPosition = GetSpawnPosition(),
                Delay         = delay
            });

            yield return new WaitForSeconds(delay);

            DoRespawn(GetSpawnPosition(), GetSpawnRotation(), countRespawn: true);
        }

        // ── Respawn silencioso (warm-up / cambio de equipo) ───

        /// <summary>
        /// Respawnea SIN emitir PlayerDiedEvent ni contar como muerte.
        /// Usado por GameModeBase al inicio de cada ronda.
        /// </summary>
        public void ForceRespawnSilent()
        {
            if (_respawnCoro != null) StopCoroutine(_respawnCoro);
            DisableSystems();
            DoRespawn(GetSpawnPosition(), GetSpawnRotation(), countRespawn: false);
        }

        /// <summary>Respawnea en posición específica sin contar como muerte.</summary>
        public void ForceRespawnAt(Vector3 pos, Quaternion rot, bool silent = true)
        {
            if (_respawnCoro != null) StopCoroutine(_respawnCoro);
            DisableSystems();
            DoRespawn(pos, rot, countRespawn: !silent);
        }

        public void ForceRespawn()
        {
            if (_respawnCoro != null) StopCoroutine(_respawnCoro);
            DoRespawn(GetSpawnPosition(), GetSpawnRotation(), countRespawn: true);
        }

        /// <summary>
        /// Reset completo al cambiar de equipo.
        /// Restaura HP, movimiento, etc. sin contar muerte.
        /// </summary>
        public void ResetFull(Vector3 spawnPos, Quaternion spawnRot)
        {
            if (_respawnCoro != null) StopCoroutine(_respawnCoro);
            IsRespawning = false;

            // Reset salud al máximo
            _health?.ResetHealth();

            // Teleport
            _movement?.Teleport(spawnPos, spawnRot);
            _camera?.SetRotation(spawnRot.eulerAngles.y);

            EnableSystems();

            CoreLogger.LogSystem("PlayerRespawn",
                $"[P{_authority.PlayerID}] ResetFull (cambio de equipo)");

            EventBus<PlayerRespawnedEvent>.Raise(new PlayerRespawnedEvent
            {
                PlayerID      = _authority.PlayerID,
                SpawnPosition = spawnPos
            });
        }

        // ── Ejecución real ────────────────────────────────────

        private void DoRespawn(Vector3 pos, Quaternion rot, bool countRespawn)
        {
            if (_config != null && _config.ResetHealthOnRespawn)
                _health?.ResetHealth();

            _movement?.Teleport(pos, rot);
            _camera?.SetRotation(rot.eulerAngles.y);

            EnableSystems();

            if (countRespawn) RespawnCount++;
            IsRespawning = false;

            CoreLogger.LogSystem("PlayerRespawn",
                $"[P{_authority.PlayerID}] Respawn #{RespawnCount} en {pos}");

            EventBus<PlayerRespawnedEvent>.Raise(new PlayerRespawnedEvent
            {
                PlayerID      = _authority.PlayerID,
                SpawnPosition = pos
            });
        }

        // ── Obtener posición de spawn ─────────────────────────

        private Vector3 GetSpawnPosition()
        {
            // 1. Buscar GMFSpawnArea del equipo
            var gm = GameModeBase.Instance;
            if (gm != null)
            {
                int teamID = gm.Context?.Teams?.GetTeam(_authority.PlayerID) ?? -1;
                if (teamID >= 0)
                {
                    var areas = FindObjectsByType<GMFSpawnArea>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                    foreach (var area in areas)
                    {
                        if (area.TeamID != teamID) continue;
                        if (area.TryGetSpawnPosition(out Vector3 p)) return p;
                    }
                }
            }

            // 2. Fallback: spawn points manuales
            if (_fallbackSpawnPoints?.Count > 0)
            {
                _fallbackSpawnPoints.RemoveAll(sp => sp == null);
                if (_fallbackSpawnPoints.Count > 0)
                    return _fallbackSpawnPoints[Random.Range(0, _fallbackSpawnPoints.Count)].position;
            }

            // 3. Último fallback: posición de inicio
            return _defaultPos;
        }

        private Quaternion GetSpawnRotation()
        {
            var gm = GameModeBase.Instance;
            if (gm != null)
            {
                int teamID = gm.Context?.Teams?.GetTeam(_authority.PlayerID) ?? -1;
                if (teamID >= 0)
                {
                    var areas = FindObjectsByType<GMFSpawnArea>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                    foreach (var area in areas)
                        if (area.TeamID == teamID)
                            return area.transform.rotation;
                }
            }
            return _defaultRot;
        }

        // ── Sistemas ──────────────────────────────────────────

        private void DisableSystems()
        {
            if (_playerVisuals != null) _playerVisuals.SetActive(false);
            GetComponent<Player.Input.PlayerInput>()?.DisableInput();
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        private void EnableSystems()
        {
            if (_playerVisuals != null) _playerVisuals.SetActive(true);
            GetComponent<Player.Input.PlayerInput>()?.EnableInput();
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        // ── API heredada ──────────────────────────────────────

        public void SetSpawnPoints(List<Transform> pts)
            => _fallbackSpawnPoints = pts ?? new List<Transform>();

        public void AddSpawnPoint(Transform pt)
        {
            if (pt != null) _fallbackSpawnPoints.Add(pt);
        }
    }
}