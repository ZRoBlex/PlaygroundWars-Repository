// ============================================================
//  WeaponBase.cs
//  Combat/Weapons/WeaponBase.cs
//
//  RESPONSABILIDAD ÚNICA: Clase base abstracta de todas las armas.
//
//  POSEE:
//  • AmmoSystem   — munición actual y reserva
//  • ReloadSystem — temporizador de recarga
//  • RecoilSystem — patrón y recovery de recoil
//
//  CONTRATO:
//  • ExecuteShoot() → abstracto, implementado en cada subclase
//  • CanShoot()     → virtual, sobreescribible
//  • OnEquip / OnUnequip → ciclo de vida del arma
//
//  NOTA: Este script NO escucha inputs directamente.
//  ShootingSystem.cs es quien llama TryShoot() según el input.
// ============================================================

using Combat.Events;
using Combat.Systems;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Camera;
using UnityEngine;

namespace Combat.Weapons
{
    [DisallowMultipleComponent]
    public abstract class WeaponBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] protected WeaponConfig _config;

        [Header("Transforms")]
        [Tooltip("Punto de origen del disparo (boca del cañón).")]
        [SerializeField] protected Transform _muzzlePoint;

        // ── Subsistemas ───────────────────────────────────────

        public AmmoSystem   Ammo   { get; private set; }
        public ReloadSystem Reload { get; private set; }
        public RecoilSystem Recoil { get; private set; }

        // ── Referencias externas ──────────────────────────────

        protected PlayerAuthority       _authority;
        protected PlayerCameraController _cameraController;

        // ── Estado ────────────────────────────────────────────

        public WeaponConfig Config     => _config;
        public bool         IsEquipped { get; private set; }

        protected float _lastFireTime;

        // ── Lifecycle ─────────────────────────────────────────

        protected virtual void Awake()
        {
            _authority        = GetComponentInParent<PlayerAuthority>();
            _cameraController = GetComponentInParent<PlayerCameraController>();

            if (_config == null)
            {
                CoreLogger.LogError($"[WeaponBase] '{name}' no tiene WeaponConfig asignado.");
                return;
            }

            int ownerID = _authority != null ? _authority.PlayerID : 0;
            Ammo        = new AmmoSystem(_config, ownerID);
            Recoil      = new RecoilSystem(_config, ownerID);
            Reload      = new ReloadSystem(_config, ownerID, Ammo, this);
        }

        protected virtual void Update()
        {
            if (!IsEquipped) return;
            Recoil?.Tick(Time.deltaTime);
        }

        // ── Contrato abstracto ────────────────────────────────

        /// <summary>Lógica de disparo específica de cada tipo de arma.</summary>
        protected abstract void ExecuteShoot();

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Intenta disparar. Valida con CanShoot() antes de llamar ExecuteShoot().
        /// </summary>
        public void TryShoot()
        {
            if (!CanShoot()) return;

            _lastFireTime = Time.time;
            Ammo.Consume(1);
            Recoil.ApplyShot();
            ExecuteShoot();

            if (Ammo.IsEmpty && _config.AutoReload)
                TryReload();
        }

        /// <summary>Intenta recargar.</summary>
        public void TryReload()
        {
            Reload.StartReload();
        }

        // ── CanShoot ──────────────────────────────────────────

        public virtual bool CanShoot()
        {
            if (_config == null)  return FailShoot("WeaponNull");
            if (!IsEquipped)      return false;

            if (Reload.IsReloading && !_config.CanFireWhileReloading())
                return FailShoot("Reloading");

            if (Ammo.IsEmpty)
                return FailShoot("NoAmmo");

            if (!IsCooldownReady())
                return FailShoot("Cooldown");

            return true;
        }

        // ── Equip / Unequip ───────────────────────────────────

        public virtual void OnEquip()
        {
            IsEquipped = true;
            gameObject.SetActive(true);

            CoreLogger.LogSystemDebug("WeaponBase",
                $"[P{_authority?.PlayerID}] Equipado: {_config.WeaponID}");

            EventBus<OnWeaponEquippedEvent>.Raise(new OnWeaponEquippedEvent
            {
                OwnerID  = _authority?.PlayerID ?? 0,
                WeaponID = _config.WeaponID,
                Slot     = 0
            });
        }

        public virtual void OnUnequip()
        {
            IsEquipped = false;
            Reload.Cancel();
            Recoil.Reset();
            gameObject.SetActive(false);

            EventBus<OnWeaponUnequippedEvent>.Raise(new OnWeaponUnequippedEvent
            {
                OwnerID  = _authority?.PlayerID ?? 0,
                WeaponID = _config.WeaponID
            });
        }

        // ── Helpers ───────────────────────────────────────────

        protected bool IsCooldownReady()
            => Time.time >= _lastFireTime + _config.FireRate;

        protected Vector3 GetMuzzlePosition()
            => _muzzlePoint != null ? _muzzlePoint.position : transform.position;

        /// <summary>
        /// Dirección de disparo desde la cámara del jugador.
        /// Fallback al forward del transform si no hay cámara.
        /// </summary>
        protected Vector3 GetShootDirection()
        {
            if (_cameraController != null && _cameraController.ActiveCamera != null)
                return _cameraController.ActiveCamera.transform.forward;

            return transform.forward;
        }

        private bool FailShoot(string reason)
        {
            EventBus<OnShootFailedEvent>.Raise(new OnShootFailedEvent
            {
                ShooterID = _authority?.PlayerID ?? 0,
                WeaponID  = _config?.WeaponID ?? "unknown",
                Reason    = reason
            });
            return false;
        }
    }

    // ── Extensión de WeaponConfig para WeaponBase ─────────────

    public static class WeaponConfigExtensions
    {
        public static bool CanFireWhileReloading(this WeaponConfig cfg)
            => false; // Expandible: añadir campo en WeaponConfig si se necesita
    }
}
