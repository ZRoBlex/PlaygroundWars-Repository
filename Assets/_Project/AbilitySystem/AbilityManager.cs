using System.Collections.Generic;
using UnityEngine;
using Player.Authority;

namespace AbilitySystem
{
    [DisallowMultipleComponent]
    public class AbilityManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // INSPECTOR
        // ─────────────────────────────────────────────

        [Header("Initial Abilities")]
        [SerializeField] private List<AbilityDefinition> _startingAbilities = new();

        // ─────────────────────────────────────────────
        // REFERENCES
        // ─────────────────────────────────────────────

        private PlayerAuthority _authority;

        // ─────────────────────────────────────────────
        // RUNTIME
        // ─────────────────────────────────────────────

        private readonly List<AbilityInstance> _abilities = new();

        // ─────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        private void Start()
        {
            InitializeAbilities();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            foreach (var ability in _abilities)
                ability.Tick(dt);
        }

        // ─────────────────────────────────────────────
        // INIT
        // ─────────────────────────────────────────────

        private void InitializeAbilities()
        {
            foreach (var def in _startingAbilities)
                AddAbility(def);
        }

        // ─────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────

        public void AddAbility(AbilityDefinition definition)
        {
            if (definition == null) return;

            var instance = new AbilityInstance(
                definition,
                _authority != null ? _authority.PlayerID : -1,
                gameObject // 🔥 CRÍTICO: ahora sí pasamos Owner
            );

            _abilities.Add(instance);
        }

        public void RemoveAbility(string abilityID)
        {
            _abilities.RemoveAll(a => a.Definition.AbilityID == abilityID);
        }

        public AbilityInstance GetAbility(string abilityID)
        {
            return _abilities.Find(a => a.Definition.AbilityID == abilityID);
        }

        // 🔥 ESTO ARREGLA TU UI
        public List<AbilityInstance> GetAllAbilities()
        {
            return _abilities;
        }

        // ─────────────────────────────────────────────
        // ACTIVATION (solo request, no lógica)
        // ─────────────────────────────────────────────

        public void RequestActivate(string abilityID)
        {
            var ability = GetAbility(abilityID);

            if (ability == null) return;

            // ─────────────────────────────────────────────
            // EVENTO: REQUEST
            // ─────────────────────────────────────────────

            AbilityEventData eventData = new AbilityEventData
            {
                SourceID = ability.OwnerID,
                AbilityID = ability.Definition.AbilityID,
                Position = transform.position,
                Direction = transform.forward
            };

            AbilityEvents.RaiseRequested(eventData);

            // ⚠️ TEMP: ejecución directa (hasta meter networking)
            AbilityExecutionSystem.ExecuteAbility(ability);
        }
    }
}