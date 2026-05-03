using UnityEngine;

namespace AbilitySystem
{
    [DisallowMultipleComponent]
    public class AbilityInputHandler : MonoBehaviour
    {
        private AbilityManager _abilityManager;

        private void Awake()
        {
            _abilityManager = GetComponent<AbilityManager>();
        }

        private void Update()
        {
            if (_abilityManager == null) return;

            // 🔥 EJEMPLO: tecla Q activa primera habilidad
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TryActivateSlot(0);
            }

            // 🔥 EJEMPLO: tecla E activa segunda habilidad
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryActivateSlot(1);
            }
        }

        private void TryActivateSlot(int index)
        {
            var abilities = _abilityManager.GetAllAbilities();

            if (abilities == null || abilities.Count <= index)
            {
                Debug.LogWarning($"No ability in slot {index}");
                return;
            }

            var ability = abilities[index];

            if (ability == null)
                return;

            Debug.Log($"Trying to activate: {ability.Definition.DisplayName}");

            // 🔥 ESTO ES LO IMPORTANTE
            _abilityManager.RequestActivate(ability.Definition.AbilityID);
        }
    }
}