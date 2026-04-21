// ============================================================
//  ProjectileWeapon.cs
//  Combat/Weapons/ProjectileWeapon.cs
//
//  RESPONSABILIDAD ÚNICA: Arma que lanza proyectiles físicos.
//
//  EJEMPLOS: lanzagranadas, bazuca, pistola de agua.
//
//  FLUJO:
//  1. ExecuteShoot() obtiene origen y dirección
//  2. Solicita un proyectil del pool via ProjectileManager
//  3. El proyectil se mueve, colisiona y notifica al HitDetectionSystem
//  4. Publica OnShootEvent para audio/animación
// ============================================================

using Combat.Events;
using Combat.Pool;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Combat.Weapons
{
    public class ProjectileWeapon : WeaponBase
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("FX")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private float      _muzzleFlashDuration = 0.05f;

        // ── ExecuteShoot ──────────────────────────────────────

        protected override void ExecuteShoot()
        {
            if (ProjectileManager.Instance == null)
            {
                CoreLogger.LogError("[ProjectileWeapon] ProjectileManager no está en escena.");
                return;
            }

            if (_config.ProjectilePrefab == null)
            {
                CoreLogger.LogError(
                    $"[ProjectileWeapon] WeaponConfig '{_config.WeaponID}' " +
                    "no tiene ProjectilePrefab asignado.");
                return;
            }

            Vector3 origin    = GetMuzzlePosition();
            Vector3 direction = GetShootDirection();

            // FX: muzzle flash
            if (_muzzleFlashPrefab != null)
            {
                var flash = Instantiate(_muzzleFlashPrefab, origin,
                    Quaternion.LookRotation(direction));
                Destroy(flash, _muzzleFlashDuration);
            }

            // Solicitar proyectil del pool
            ProjectileManager.Instance.Spawn(
                config:    _config,
                shooterID: _authority.PlayerID,
                position:  origin,
                direction: direction
            );

            // Evento para audio, animación, efectos
            EventBus<OnShootEvent>.Raise(new OnShootEvent
            {
                ShooterID = _authority.PlayerID,
                WeaponID  = _config.WeaponID,
                Type      = ShootingType.Projectile,
                Origin    = origin,
                Direction = direction
            });
        }
    }
}
