// ============================================================
//  WeaponManager.cs
//  Combat/Weapons/WeaponManager.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar los slots de armas del jugador.
//
//  CARACTERÍSTICAS:
//  • Sistema de N slots configurables
//  • Equipar, desequipar, ciclar armas
//  • Notifica a ShootingSystem del arma activa
//  • Cambio de arma por scroll, número o evento
//  • Reinicia munición en respawn
// ============================================================

using System.Collections.Generic;
using Combat.Events;
using Combat.Systems;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Events;
using UnityEngine;

namespace Combat.Weapons
{
    [RequireComponent(typeof(PlayerAuthority))]
    [RequireComponent(typeof(ShootingSystem))]
    [DisallowMultipleComponent]
    public class WeaponManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Slots")]
        [Range(1, 4)]
        [SerializeField] private int _maxSlots = 2;

        [Tooltip("Armas asignadas al inicio (índice = slot).")]
        [SerializeField] private List<WeaponBase> _startingWeapons = new();

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;
        private ShootingSystem  _shooting;

        // ── Estado ────────────────────────────────────────────

        private WeaponBase[] _slots;
        private int          _currentSlot;

        public WeaponBase ActiveWeapon => _slots?[_currentSlot];
        public int        CurrentSlot  => _currentSlot;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _shooting  = GetComponent<ShootingSystem>();
            _slots     = new WeaponBase[_maxSlots];
        }

        private void Start()
        {
            for (int i = 0; i < _startingWeapons.Count && i < _maxSlots; i++)
                if (_startingWeapons[i] != null)
                    AssignToSlot(_startingWeapons[i], i, activate: false);

            EquipSlot(0);
        }

        private void OnEnable()
        {
            EventBus<PlayerAbilityInputEvent>.Subscribe(OnAbilityInput);
            EventBus<PlayerRespawnedEvent>.Subscribe(OnRespawn);
        }

        private void OnDisable()
        {
            EventBus<PlayerAbilityInputEvent>.Unsubscribe(OnAbilityInput);
            EventBus<PlayerRespawnedEvent>.Unsubscribe(OnRespawn);
        }

        private void Update()
        {
            // Scroll de ratón para cambiar arma
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.05f)
                CycleWeapon(scroll > 0 ? 1 : -1);

            // Teclas numéricas 1-N
            for (int i = 0; i < _maxSlots; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    EquipSlot(i);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Equipa el slot indicado.</summary>
        public void EquipSlot(int slot)
        {
            if (slot < 0 || slot >= _maxSlots) return;
            if (_slots[slot] == null)
            {
                CoreLogger.LogSystemDebug("WeaponManager",
                    $"[P{_authority.PlayerID}] Slot {slot} vacío.");
                return;
            }
            if (slot == _currentSlot && ActiveWeapon != null && ActiveWeapon.IsEquipped) return;

            string prevID = ActiveWeapon?.Config?.WeaponID ?? string.Empty;

            ActiveWeapon?.OnUnequip();
            _currentSlot = slot;
            _slots[_currentSlot]?.OnEquip();
            _shooting.SetActiveWeapon(_slots[_currentSlot]);

            CoreLogger.LogSystem("WeaponManager",
                $"[P{_authority.PlayerID}] Slot {slot} → {_slots[slot]?.Config?.WeaponID}");

            EventBus<OnWeaponSwitchedEvent>.Raise(new OnWeaponSwitchedEvent
            {
                OwnerID          = _authority.PlayerID,
                PreviousWeaponID = prevID,
                NewWeaponID      = _slots[slot]?.Config?.WeaponID ?? string.Empty,
                NewSlot          = slot
            });
        }

        /// <summary>Asigna un arma a un slot sin equiparla.</summary>
        public void AssignToSlot(WeaponBase weapon, int slot, bool activate = false)
        {
            if (weapon == null || slot < 0 || slot >= _maxSlots) return;

            if (_slots[slot] != null && _slots[slot].IsEquipped)
                _slots[slot].OnUnequip();

            _slots[slot] = weapon;
            weapon.gameObject.SetActive(false);

            if (activate) EquipSlot(slot);
        }

        /// <summary>Cicla hacia adelante o atrás entre slots con arma.</summary>
        public void CycleWeapon(int dir)
        {
            int next = _currentSlot;
            for (int i = 0; i < _maxSlots; i++)
            {
                next = ((next + dir) % _maxSlots + _maxSlots) % _maxSlots;
                if (_slots[next] != null) { EquipSlot(next); return; }
            }
        }

        /// <summary>Devuelve el arma en un slot (puede ser null).</summary>
        public WeaponBase GetWeapon(int slot)
            => slot >= 0 && slot < _maxSlots ? _slots[slot] : null;

        /// <summary>Resetea munición de todas las armas (respawn).</summary>
        public void ResetAllAmmo()
        {
            foreach (var w in _slots) w?.Ammo?.Reset();
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnAbilityInput(PlayerAbilityInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (e.AbilitySlot < _maxSlots) EquipSlot(e.AbilitySlot);
        }

        private void OnRespawn(PlayerRespawnedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            ResetAllAmmo();
        }
    }
}
