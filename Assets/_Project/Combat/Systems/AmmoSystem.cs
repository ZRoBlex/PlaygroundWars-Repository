// ============================================================
//  AmmoSystem.cs
//  Combat/Systems/AmmoSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar munición de un arma.
//
//  Una instancia por arma. WeaponBase la posee y la inicializa.
//  Publica eventos cada vez que la munición cambia.
// ============================================================

using Combat.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Combat.Systems
{
    public class AmmoSystem
    {
        // ── Estado ────────────────────────────────────────────

        public int  CurrentMagazine  { get; private set; }
        public int  ReserveAmmo      { get; private set; }
        public bool IsEmpty          => CurrentMagazine <= 0;
        public bool IsFullMagazine   => CurrentMagazine >= _config.MagazineSize;

        public float MagazinePercent =>
            _config.MagazineSize > 0 ? (float)CurrentMagazine / _config.MagazineSize : 0f;

        // ── Config ────────────────────────────────────────────

        private readonly WeaponConfig _config;
        private readonly int          _ownerID;

        // ── Constructor ───────────────────────────────────────

        public AmmoSystem(WeaponConfig config, int ownerID)
        {
            _config         = config;
            _ownerID        = ownerID;
            CurrentMagazine = config.MagazineSize;
            ReserveAmmo     = config.MaxReserveAmmo;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Consume balas del cargador.
        /// Retorna false si no hay suficiente munición.
        /// </summary>
        public bool Consume(int amount = 1)
        {
            if (CurrentMagazine < amount) return false;

            CurrentMagazine -= amount;
            PublishChanged();

            if (CurrentMagazine <= 0)
                EventBus<OnAmmoEmptyEvent>.Raise(new OnAmmoEmptyEvent
                {
                    OwnerID  = _ownerID,
                    WeaponID = _config.WeaponID
                });

            return true;
        }

        /// <summary>
        /// Recarga el cargador usando reserva.
        /// Retorna cuántas balas se cargaron.
        /// </summary>
        public int Reload()
        {
            if (IsFullMagazine) return 0;

            int needed = _config.MagazineSize - CurrentMagazine;

            if (_config.InfiniteReserve)
            {
                CurrentMagazine = _config.MagazineSize;
                PublishChanged();
                return needed;
            }

            int available = Mathf.Min(needed, ReserveAmmo);
            if (available <= 0) return 0;

            CurrentMagazine += available;
            ReserveAmmo     -= available;
            PublishChanged();
            return available;
        }

        /// <summary>Añade munición de reserva (pickup).</summary>
        public void AddReserve(int amount)
        {
            if (_config.InfiniteReserve) return;
            ReserveAmmo = Mathf.Min(ReserveAmmo + amount, _config.MaxReserveAmmo);
            PublishChanged();
        }

        public bool HasReserveForReload()
            => _config.InfiniteReserve || ReserveAmmo > 0;

        /// <summary>Resetea a valores iniciales (respawn / inicio de ronda).</summary>
        public void Reset()
        {
            CurrentMagazine = _config.MagazineSize;
            ReserveAmmo     = _config.MaxReserveAmmo;
            PublishChanged();
        }

        // ── Privado ───────────────────────────────────────────

        private void PublishChanged()
        {
            EventBus<OnAmmoChangedEvent>.Raise(new OnAmmoChangedEvent
            {
                OwnerID  = _ownerID,
                WeaponID = _config.WeaponID,
                Current  = CurrentMagazine,
                Max      = _config.MagazineSize,
                Reserve  = ReserveAmmo
            });
        }
    }
}
