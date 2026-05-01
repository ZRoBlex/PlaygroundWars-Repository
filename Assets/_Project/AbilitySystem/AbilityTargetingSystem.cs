// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityCooldownSystem.cs                       ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Core/            ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • AbilityCooldownSystem  (C# puro)   ← principal      ║
// ║    • AbilityTargetingSystem (MonoBehaviour)              ║
// ║                                                          ║
// ║  ⚠️ SEPARAR:                                            ║
// ║    AbilityTargetingSystem → AbilityTargetingSystem.cs    ║
// ║    Motivo: es un MonoBehaviour que va en el prefab del   ║
// ║    jugador. Conviene tenerlo en su propio archivo para   ║
// ║    añadirlo/quitarlo independientemente.                 ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace AbilitySystem
{
    // ════════════════════════════════════════════════════════
    //  ABILITY TARGETING SYSTEM
    //  MonoBehaviour en el prefab del jugador.
    //  Resuelve el objetivo según el TargetType del config.
    // ════════════════════════════════════════════════════════

    public struct TargetingResult
    {
        public bool       Valid;
        public Vector3    Position;
        public Vector3    Direction;
        public int        TargetPlayerID;   // -1 si no hay jugador
        public Collider[] AreaColliders;    // solo para Area
    }

    [DisallowMultipleComponent]
    public class AbilityTargetingSystem : MonoBehaviour
    {
        [Header("Cámara del jugador")]
        [SerializeField] private Camera _camera;

        private PlayerAuthority _authority;

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            if (_camera == null) _camera = GetComponentInChildren<Camera>();
        }

        // ── API ───────────────────────────────────────────────

        public TargetingResult Resolve(AbilityConfig cfg)
        {
            if (cfg == null) return Invalid();

            return cfg.TargetType switch
            {
                TargetType.Self      => ResolveSelf(),
                TargetType.Target    => ResolveTarget(cfg),
                TargetType.Area      => ResolveArea(cfg),
                TargetType.Direction => ResolveDirection(),
                _                    => Invalid()
            };
        }

        // ── Modos ─────────────────────────────────────────────

        private TargetingResult ResolveSelf() => new()
        {
            Valid          = true,
            Position       = transform.position,
            Direction      = transform.forward,
            TargetPlayerID = _authority.PlayerID
        };

        private TargetingResult ResolveTarget(AbilityConfig cfg)
        {
            if (_camera == null) return Invalid();

            LayerMask mask = cfg.HitLayers.value == 0 ? ~0 : cfg.HitLayers;

            if (!Physics.Raycast(
                    _camera.transform.position,
                    _camera.transform.forward,
                    out RaycastHit hit, cfg.Range, mask,
                    QueryTriggerInteraction.Ignore))
                return Invalid();

            int id = GetPlayerID(hit.collider);
            if (id == _authority.PlayerID && !cfg.CanTargetSelf)
                return Invalid();

            return new TargetingResult
            {
                Valid          = true,
                Position       = hit.point,
                Direction      = (hit.point - transform.position).normalized,
                TargetPlayerID = id
            };
        }

        private TargetingResult ResolveArea(AbilityConfig cfg)
        {
            LayerMask mask = cfg.HitLayers.value == 0 ? ~0 : cfg.HitLayers;
            var hits       = Physics.OverlapSphere(transform.position, cfg.AreaRadius, mask);

            return new TargetingResult
            {
                Valid           = true,
                Position        = transform.position,
                Direction       = transform.forward,
                TargetPlayerID  = -1,
                AreaColliders   = hits
            };
        }

        private TargetingResult ResolveDirection()
        {
            Vector3 dir = _camera != null
                ? _camera.transform.forward
                : transform.forward;

            return new TargetingResult
            {
                Valid          = true,
                Position       = transform.position,
                Direction      = dir,
                TargetPlayerID = -1
            };
        }

        private static TargetingResult Invalid() => new() { Valid = false };

        private static int GetPlayerID(Collider col)
        {
            var auth = col.GetComponentInParent<PlayerAuthority>();
            return auth != null ? auth.PlayerID : -1;
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (_camera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_camera.transform.position, _camera.transform.forward * 10f);
        }
    }
}