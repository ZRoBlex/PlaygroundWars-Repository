// ============================================================
//  AbilityManager.cs
//  AbilitySystem/Abilities/AbilityManager.cs
//
//  RESPONSABILIDAD ÚNICA: Gestionar habilidades del jugador.
//  Escucha inputs de habilidades y activa la habilidad del slot.
// ============================================================

namespace Abilities
{
    using System.Collections.Generic;
    using Abilities.Events;
    using Core.Events;
    using Player.Authority;
    using Player.Events;
    using UnityEngine;

    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class AbilityManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Habilidades por slot (índice = slot)")]
        [SerializeField] private List<AbilityBase> _abilities = new();

        [Header("Red")]
        [SerializeField] private bool _useNetworking = false;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority     _authority;
        private AbilityCooldownSystem _cooldowns;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _cooldowns = new AbilityCooldownSystem(_authority.PlayerID, this);

            // Inyectar cooldown system en cada habilidad
            foreach (var ability in _abilities)
                ability?.SetCooldownSystem(_cooldowns);
        }

        private void OnEnable()
        {
            EventBus<PlayerAbilityInputEvent>.Subscribe(OnAbilityInput);
            EventBus<OnCooldownEndEvent>.Subscribe(OnCooldownEnd);
        }

        private void OnDisable()
        {
            EventBus<PlayerAbilityInputEvent>.Unsubscribe(OnAbilityInput);
            EventBus<OnCooldownEndEvent>.Unsubscribe(OnCooldownEnd);
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnAbilityInput(PlayerAbilityInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            ActivateSlot(e.AbilitySlot);
        }

        private void OnCooldownEnd(OnCooldownEndEvent e)
        {
            if (e.OwnerID != _authority.PlayerID) return;
            var ability = GetAbilityByID(e.AbilityID);
            ability?.OnCooldownEnd();
        }

        // ── API Pública ───────────────────────────────────────

        public bool ActivateSlot(int slot)
        {
            if (slot < 0 || slot >= _abilities.Count) return false;
            return _abilities[slot]?.TryActivate() ?? false;
        }

        public bool IsReady(int slot)
        {
            if (slot < 0 || slot >= _abilities.Count) return false;
            var ab = _abilities[slot];
            return ab != null && _cooldowns.IsReady(ab.Config?.AbilityID ?? "");
        }

        public float GetCooldownRemaining(int slot)
        {
            if (slot < 0 || slot >= _abilities.Count) return 0f;
            var ab = _abilities[slot];
            return ab != null ? _cooldowns.GetRemaining(ab.Config?.AbilityID ?? "") : 0f;
        }

        public void ResetAllCooldowns() => _cooldowns.ResetAll();

        public void AssignAbility(AbilityBase ability, int slot)
        {
            if (slot < 0 || slot >= _abilities.Count) return;
            ability?.SetCooldownSystem(_cooldowns);
            _abilities[slot] = ability;
        }

        private AbilityBase GetAbilityByID(string id)
        {
            foreach (var ab in _abilities)
                if (ab?.Config?.AbilityID == id) return ab;
            return null;
        }

        public IReadOnlyList<AbilityBase> Abilities => _abilities.AsReadOnly();
    }
}
