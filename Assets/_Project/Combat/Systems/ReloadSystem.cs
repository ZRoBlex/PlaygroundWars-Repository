// ============================================================
//  ReloadSystem.cs
//  Combat/Systems/ReloadSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Timer y lógica de recarga de un arma.
//
//  Trabaja junto a AmmoSystem: administra el temporizador y
//  llama a AmmoSystem.Reload() cuando el proceso termina.
//  Puede ser cancelado en cualquier momento.
// ============================================================

using System.Collections;
using Combat.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Combat.Systems
{
    public class ReloadSystem
    {
        // ── Estado ────────────────────────────────────────────

        public bool  IsReloading    { get; private set; }
        public float Progress       { get; private set; }  // 0 → 1

        // ── Dependencias ──────────────────────────────────────

        private readonly WeaponConfig  _config;
        private readonly int           _ownerID;
        private readonly AmmoSystem    _ammo;
        private readonly MonoBehaviour _runner;

        private Coroutine _coroutine;

        // ── Constructor ───────────────────────────────────────

        public ReloadSystem(WeaponConfig config, int ownerID, AmmoSystem ammo, MonoBehaviour runner)
        {
            _config  = config;
            _ownerID = ownerID;
            _ammo    = ammo;
            _runner  = runner;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Inicia la recarga. Retorna false si ya recarga, cargador lleno o sin reserva.
        /// </summary>
        public bool StartReload()
        {
            if (IsReloading)
            {
                CoreLogger.LogSystemDebug("ReloadSystem",
                    $"[P{_ownerID}][{_config.WeaponID}] Ya recargando.");
                return false;
            }
            if (_ammo.IsFullMagazine)
            {
                CoreLogger.LogSystemDebug("ReloadSystem",
                    $"[P{_ownerID}][{_config.WeaponID}] Cargador lleno.");
                return false;
            }
            if (!_ammo.HasReserveForReload())
            {
                CoreLogger.LogSystemDebug("ReloadSystem",
                    $"[P{_ownerID}][{_config.WeaponID}] Sin reserva.");
                return false;
            }

            _coroutine = _runner.StartCoroutine(ReloadRoutine());
            return true;
        }

        /// <summary>Cancela la recarga en curso (cambio de arma, recibió daño, etc.).</summary>
        public void Cancel()
        {
            if (!IsReloading) return;

            if (_coroutine != null)
                _runner.StopCoroutine(_coroutine);

            IsReloading = false;
            Progress    = 0f;

            EventBus<OnReloadCancelledEvent>.Raise(new OnReloadCancelledEvent
            {
                OwnerID  = _ownerID,
                WeaponID = _config.WeaponID
            });

            CoreLogger.LogSystemDebug("ReloadSystem",
                $"[P{_ownerID}][{_config.WeaponID}] Recarga cancelada.");
        }

        // ── Coroutine ─────────────────────────────────────────

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            Progress    = 0f;

            CoreLogger.LogSystem("ReloadSystem",
                $"[P{_ownerID}][{_config.WeaponID}] Iniciando recarga ({_config.ReloadTime}s)");

            EventBus<OnReloadStartEvent>.Raise(new OnReloadStartEvent
            {
                OwnerID  = _ownerID,
                WeaponID = _config.WeaponID,
                Duration = _config.ReloadTime
            });

            float elapsed = 0f;
            float total   = Mathf.Max(0.01f, _config.ReloadTime);

            while (elapsed < total)
            {
                elapsed  += Time.deltaTime;
                Progress  = Mathf.Clamp01(elapsed / total);
                yield return null;
            }

            int added = _ammo.Reload();

            IsReloading = false;
            Progress    = 1f;

            EventBus<OnReloadCompleteEvent>.Raise(new OnReloadCompleteEvent
            {
                OwnerID  = _ownerID,
                WeaponID = _config.WeaponID,
                NewAmmo  = _ammo.CurrentMagazine
            });

            CoreLogger.LogSystem("ReloadSystem",
                $"[P{_ownerID}][{_config.WeaponID}] Recarga completa. +{added}");
        }
    }
}
