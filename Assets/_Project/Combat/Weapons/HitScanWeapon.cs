// ============================================================
//  HitScanWeapon.cs
//  Combat/Weapons/HitScanWeapon.cs
//
//  RESPONSABILIDAD ÚNICA: Arma de disparo instantáneo (raycast).
//
//  EJEMPLOS: pistola, rifle, sniper, escopeta (pellets).
//
//  FLUJO:
//  1. ExecuteShoot() obtiene origen y dirección
//  2. Llama a HitDetectionSystem.ProcessHitScan()
//  3. Muestra efectos visuales (tracer, muzzle flash, impacto)
//  4. Publica OnShootEvent para audio/animación
// ============================================================

using Combat.Events;
using Combat.Systems;
using Core.Events;
using UnityEngine;

namespace Combat.Weapons
{
    public class HitScanWeapon : WeaponBase
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("FX — Boca del Cañón")]
        [SerializeField] private GameObject    _muzzleFlashPrefab;
        [SerializeField] private float         _muzzleFlashDuration = 0.05f;

        [Header("FX — Tracer (trayectoria visual)")]
        [SerializeField] private LineRenderer  _tracerLine;
        [SerializeField] private float         _tracerDuration      = 0.06f;

        [Header("FX — Impacto")]
        [SerializeField] private GameObject    _impactFxPrefab;

        // ── Coroutine ─────────────────────────────────────────

        private Coroutine _tracerCoroutine;

        // ── ExecuteShoot ──────────────────────────────────────

        protected override void ExecuteShoot()
        {
            Vector3 origin    = GetMuzzlePosition();
            Vector3 direction = GetShootDirection();

            // FX: muzzle flash
            if (_muzzleFlashPrefab != null)
            {
                var flash = Instantiate(_muzzleFlashPrefab, origin, Quaternion.LookRotation(direction));
                Destroy(flash, _muzzleFlashDuration);
            }

            // Detección de impacto y daño
            HitDetectionSystem_Fixed.ProcessHitScan(_config, _authority.PlayerID, Camera.main, direction);

            // FX: tracer visual
            if (_tracerLine != null)
            {
                if (_tracerCoroutine != null) StopCoroutine(_tracerCoroutine);
                _tracerCoroutine = StartCoroutine(ShowTracer(origin, direction));
            }

            // Evento de disparo para audio, animación, HUD
            EventBus<OnShootEvent>.Raise(new OnShootEvent
            {
                ShooterID = _authority.PlayerID,
                WeaponID  = _config.WeaponID,
                Type      = ShootingType.HitScan,
                Origin    = origin,
                Direction = direction
            });
        }

        // ── Tracer ────────────────────────────────────────────

        private System.Collections.IEnumerator ShowTracer(Vector3 origin, Vector3 direction)
        {
            // Calcular endpoint: donde impactó o distancia máxima
            Vector3 endpoint = origin + direction * _config.MaxRange;

            if (Physics.Raycast(origin, direction, out RaycastHit hit,
                    _config.MaxRange, _config.HitLayers))
            {
                endpoint = hit.point;

                // FX de impacto en el punto de golpe
                if (_impactFxPrefab != null)
                {
                    var fx = Instantiate(_impactFxPrefab, hit.point,
                        Quaternion.LookRotation(hit.normal));
                    Destroy(fx, 1.5f);
                }
            }

            _tracerLine.SetPosition(0, origin);
            _tracerLine.SetPosition(1, endpoint);
            _tracerLine.enabled = true;

            yield return new WaitForSeconds(_tracerDuration);

            _tracerLine.enabled = false;
        }
    }
}
