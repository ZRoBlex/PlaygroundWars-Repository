// ============================================================
//  ShootingSystem.cs
//  Combat/Systems/ShootingSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Coordinar el disparo con el arma activa.
//
//  • Escucha PlayerShootInputEvent del Player System
//  • Determina si es semi o automático según WeaponConfig
//  • Para armas automáticas: dispara mientras se mantiene el gatillo
//  • Para semi: disparo único por pulsación
//  • WeaponManager le pasa la referencia del arma activa
// ============================================================

using Combat.Weapons;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Events;
using UnityEngine;

namespace Combat.Systems
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class ShootingSystem : MonoBehaviour
    {
        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;
        private WeaponBase      _activeWeapon;

        // ── Estado ────────────────────────────────────────────

        public bool      IsHoldingTrigger  { get; private set; }
        public WeaponBase ActiveWeapon     => _activeWeapon;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        private void OnEnable()
        {
            EventBus<PlayerShootInputEvent>.Subscribe(OnShootInput);
        }

        private void OnDisable()
        {
            EventBus<PlayerShootInputEvent>.Unsubscribe(OnShootInput);
            IsHoldingTrigger = false;
        }

        private void Update()
        {
            // Auto-fire: dispara cada frame mientras se mantiene el gatillo
            if (!IsHoldingTrigger)  return;
            if (_activeWeapon == null) return;
            if (_activeWeapon.Config == null) return;
            if (!_activeWeapon.Config.IsAutomatic) return;

            _activeWeapon.TryShoot();
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnShootInput(PlayerShootInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            IsHoldingTrigger = e.IsPressed;

            // Disparo semi-automático: solo al apretar (no al mantener)
            if (e.IsPressed && _activeWeapon != null)
            {
                if (_activeWeapon.Config != null && !_activeWeapon.Config.IsAutomatic)
                    _activeWeapon.TryShoot();
            }
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Asigna el arma activa. Llamado por WeaponManager.</summary>
        public void SetActiveWeapon(WeaponBase weapon)
        {
            _activeWeapon    = weapon;
            IsHoldingTrigger = false;

            CoreLogger.LogSystemDebug("ShootingSystem",
                $"[P{_authority.PlayerID}] Arma activa → {weapon?.Config?.WeaponID ?? "ninguna"}");
        }

        /// <summary>Disparo forzado por código (editor tool, habilidad).</summary>
        public void ForceShoot()
        {
            _activeWeapon?.TryShoot();
        }

        /// <summary>Recarga forzada (tecla R, editor).</summary>
        public void ForceReload()
        {
            _activeWeapon?.TryReload();
        }
    }
}
