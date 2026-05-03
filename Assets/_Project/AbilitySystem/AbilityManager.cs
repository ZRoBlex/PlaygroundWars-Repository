// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityManager.cs                              ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Core/            ║
// ║  CLASE ÚNICA: AbilityManager (MonoBehaviour)             ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Gestionar todas las habilidades de una entidad.       ║
// ║    Añadir al prefab del jugador.                         ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace AbilitySystem
{
    [DisallowMultipleComponent]
    public class AbilityManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Habilidades iniciales (índice = slot)")]
        [SerializeField] private List<AbilityBase> _startAbilities = new();

        [Range(1, 8)]
        [SerializeField] private int _maxSlots = 4;

        [SerializeField] private bool _useNetworking = false;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority       _authority;
        private AbilityCooldownSystem _cooldowns;

        // ── Estado ────────────────────────────────────────────

        private readonly List<AbilityBase>           _slots    = new();
        private readonly Dictionary<string, int>     _idToSlot = new();

        public IReadOnlyList<AbilityBase> Slots   => _slots;
        public int                        OwnerID => _authority?.PlayerID ?? 0;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _cooldowns = new AbilityCooldownSystem(OwnerID, this);
        }

        private void Start()
        {
            foreach (var ab in _startAbilities)
                if (ab != null) AddAbility(ab);
        }

        private void OnEnable()
        {
            EventBus<PlayerAbilityInputEvt>.Subscribe(OnInput);
            EventBus<OnCooldownEndEvt>.Subscribe(OnCooldownEnd);
        }

        private void OnDisable()
        {
            EventBus<PlayerAbilityInputEvt>.Unsubscribe(OnInput);
            EventBus<OnCooldownEndEvt>.Unsubscribe(OnCooldownEnd);
        }

        // ── Input ─────────────────────────────────────────────

        private void OnInput(PlayerAbilityInputEvt e)
        {
            if (e.PlayerID != OwnerID) return;
            ActivateSlot(e.SlotIndex);
        }

        private void OnCooldownEnd(OnCooldownEndEvt e)
        {
            if (e.OwnerID != OwnerID) return;
            if (_idToSlot.TryGetValue(e.AbilityID, out int slot))
                _slots[slot]?.OnCooldownEnd();
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Activa la habilidad en el slot indicado.</summary>
        public bool ActivateSlot(int slot)
        {
            if (slot < 0 || slot >= _slots.Count) return false;

            if (_useNetworking && !_authority.HasAuthority)
            {
                // Cliente → enviar request al servidor
                EventBus<RequestActivateAbilityEvt>.Raise(new RequestActivateAbilityEvt
                {
                    RequesterID = OwnerID,
                    AbilityID   = _slots[slot]?.Config?.AbilityID ?? "",
                    Timestamp   = Time.time
                });
                return false;
            }

            return _slots[slot]?.TryActivate() ?? false;
        }

        /// <summary>Activa una habilidad por su AbilityID.</summary>
        public bool ActivateByID(string abilityID)
        {
            if (!_idToSlot.TryGetValue(abilityID, out int slot)) return false;
            return ActivateSlot(slot);
        }

        /// <summary>Añade una habilidad en el primer slot libre.</summary>
        public bool AddAbility(AbilityBase ability, int slotHint = -1)
        {
            if (ability == null) return false;
            if (_slots.Count >= _maxSlots)
            {
                CoreLogger.LogWarning($"[AbilityManager] P{OwnerID}: slots llenos.");
                return false;
            }

            string id = ability.Config?.AbilityID ?? "";
            if (!string.IsNullOrEmpty(id) && _idToSlot.ContainsKey(id))
            {
                CoreLogger.LogWarning($"[AbilityManager] '{id}' ya existe.");
                return false;
            }

            ability.InjectCooldown(_cooldowns);

            int slot = slotHint >= 0 && slotHint < _maxSlots ? slotHint : _slots.Count;

            // Expandir lista si hace falta
            while (_slots.Count <= slot) _slots.Add(null);
            _slots[slot] = ability;

            if (!string.IsNullOrEmpty(id))
                _idToSlot[id] = slot;

            CoreLogger.LogSystem("AbilityManager",
                $"P{OwnerID}: '{id}' → slot {slot}");

            EventBus<OnAbilityAddedEvt>.Raise(new OnAbilityAddedEvt
            {
                OwnerID   = OwnerID,
                AbilityID = id,
                SlotIndex = slot
            });

            return true;
        }

        /// <summary>Remueve una habilidad por ID.</summary>
        public bool RemoveAbility(string abilityID)
        {
            if (!_idToSlot.TryGetValue(abilityID, out int slot)) return false;

            _slots[slot] = null;
            _idToSlot.Remove(abilityID);
            _cooldowns.Reset(abilityID);

            EventBus<OnAbilityRemovedEvt>.Raise(new OnAbilityRemovedEvt
            {
                OwnerID   = OwnerID,
                AbilityID = abilityID
            });

            return true;
        }

        public AbilityBase GetSlot(int slot)
            => slot >= 0 && slot < _slots.Count ? _slots[slot] : null;

        public bool IsReady(int slot)
        {
            var ab = GetSlot(slot);
            return ab != null && _cooldowns.IsReady(ab.Config?.AbilityID ?? "");
        }

        public float GetCooldownRemaining(int slot)
        {
            var ab = GetSlot(slot);
            return ab != null ? _cooldowns.GetRemaining(ab.Config?.AbilityID ?? "") : 0f;
        }

        public void ResetAllCooldowns() => _cooldowns.ResetAll();

        // ── Networking: servidor procesa requests ─────────────

        private void OnEnable_Net()
        {
            EventBus<RequestActivateAbilityEvt>.Subscribe(OnNetRequest);
        }

        private void OnNetRequest(RequestActivateAbilityEvt e)
        {
            if (e.RequesterID != OwnerID) return;
            if (!_authority.HasAuthority) return;
            ActivateByID(e.AbilityID);
        }
    }

    // ── Evento de request de red ──────────────────────────────

    // public struct OnAbilityAddedEvt
    // {
    //     public int    OwnerID;
    //     public string AbilityID;
    //     public int    SlotIndex;
    // }

    // public struct OnAbilityRemovedEvt
    // {
    //     public int    OwnerID;
    //     public string AbilityID;
    // }

    // public struct RequestActivateAbilityEvt
    // {
    //     public int    RequesterID;
    //     public string AbilityID;
    //     public float  Timestamp;
    // }
}