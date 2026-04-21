// ============================================================
//  AbilityTargetingSystem.cs
//  AbilitySystem/Targeting/AbilityTargetingSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Resolver objetivos de habilidades.
//
//  MODOS:
//  • Self      → el propio jugador
//  • Target    → jugador en crosshair (Raycast desde cámara)
//  • Area      → todos en radio alrededor del jugador o cursor
//  • Direction → forward del jugador
//
//  Retorna AbilityTarget con la info resuelta.
//  AbilityBase consulta esto en Activate().
// ============================================================

using Abilities.Config;
using Abilities.Events;
using Core.Debug;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace Abilities
{
    /// <summary>Resultado de la resolución de targeting.</summary>
    public struct AbilityTarget
    {
        public bool      Valid;
        public TargetType Type;
        public Vector3   Position;
        public Vector3   Direction;
        public int       TargetPlayerID;       // -1 si no hay jugador
        public Collider[] AreaTargets;          // Para AOE
    }

    [DisallowMultipleComponent]
    public class AbilityTargetingSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Cámara del jugador")]
        [SerializeField] private Camera _playerCamera;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private PlayerAuthority _authority;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();

            if (_playerCamera == null)
                _playerCamera = GetComponentInChildren<Camera>();
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Resuelve el objetivo según el tipo de targeting de la habilidad.
        /// Retorna AbilityTarget con Valid=false si no hay objetivo válido.
        /// </summary>
        public AbilityTarget Resolve(AbilityConfig config)
        {
            if (config == null)
                return new AbilityTarget { Valid = false };

            return config.TargetType switch
            {
                TargetType.Self      => ResolveSelf(),
                TargetType.Target    => ResolveTarget(config),
                TargetType.Area      => ResolveArea(config),
                TargetType.Direction => ResolveDirection(),
                _                    => new AbilityTarget { Valid = false }
            };
        }

        // ── Self ──────────────────────────────────────────────

        private AbilityTarget ResolveSelf()
        {
            return new AbilityTarget
            {
                Valid          = true,
                Type           = TargetType.Self,
                Position       = transform.position,
                Direction      = transform.forward,
                TargetPlayerID = _authority.PlayerID
            };
        }

        // ── Target (jugador en crosshair) ─────────────────────

        private AbilityTarget ResolveTarget(AbilityConfig config)
        {
            if (_playerCamera == null)
                return new AbilityTarget { Valid = false };

            if (!Physics.Raycast(
                    _playerCamera.transform.position,
                    _playerCamera.transform.forward,
                    out RaycastHit hit,
                    config.Range,
                    config.TargetLayers,
                    QueryTriggerInteraction.Ignore))
            {
                CoreLogger.LogSystemDebug("Targeting",
                    $"[P{_authority.PlayerID}] No hay objetivo en rango.");
                return new AbilityTarget { Valid = false };
            }

            var targetAuth = hit.collider.GetComponentInParent<PlayerAuthority>();
            int targetID   = targetAuth != null ? targetAuth.PlayerID : -1;

            // Validar si puede apuntar a sí mismo
            if (targetID == _authority.PlayerID && !config.CanTargetSelf)
                return new AbilityTarget { Valid = false };

            EventBus<OnTargetAcquiredEvent>.Raise(new OnTargetAcquiredEvent
            {
                OwnerID        = _authority.PlayerID,
                AbilityID      = config.AbilityID,
                TargetID       = targetID,
                TargetPosition = hit.point
            });

            return new AbilityTarget
            {
                Valid          = true,
                Type           = TargetType.Target,
                Position       = hit.point,
                Direction      = (hit.point - transform.position).normalized,
                TargetPlayerID = targetID
            };
        }

        // ── Area (AOE) ────────────────────────────────────────

        private AbilityTarget ResolveArea(AbilityConfig config)
        {
            // Buscar todos los colliders en el radio
            var hits = Physics.OverlapSphere(
                transform.position,
                config.AreaRadius,
                config.TargetLayers);

            return new AbilityTarget
            {
                Valid          = hits.Length > 0,
                Type           = TargetType.Area,
                Position       = transform.position,
                Direction      = transform.forward,
                TargetPlayerID = -1,
                AreaTargets    = hits
            };
        }

        // ── Direction (proyectil/cone) ────────────────────────

        private AbilityTarget ResolveDirection()
        {
            Vector3 dir = _playerCamera != null
                ? _playerCamera.transform.forward
                : transform.forward;

            return new AbilityTarget
            {
                Valid          = true,
                Type           = TargetType.Direction,
                Position       = transform.position,
                Direction      = dir,
                TargetPlayerID = -1
            };
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 5f);

            if (_playerCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(_playerCamera.transform.position,
                               _playerCamera.transform.forward * 10f);
            }
        }
    }
}
