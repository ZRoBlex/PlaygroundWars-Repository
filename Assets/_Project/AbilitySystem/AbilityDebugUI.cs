using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    public class AbilityDebugUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AbilityManager _abilityManager;

        [Header("UI Settings")]
        [SerializeField] private bool _showUI = true;

        private void OnGUI()
        {
            if (!_showUI || _abilityManager == null) return;

            var abilities = _abilityManager.GetAllAbilities();

            GUILayout.BeginArea(new Rect(20, 20, 300, 600), GUI.skin.box);

            GUILayout.Label("=== ABILITIES ===");

            foreach (var ability in abilities)
            {
                DrawAbility(ability);
            }

            GUILayout.EndArea();
        }

        private void DrawAbility(AbilityInstance ability)
        {
            if (ability == null || ability.Definition == null) return;

            string name = ability.Definition.DisplayName;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(name);

            // Estado
            if (ability.IsReady)
            {
                if (GUILayout.Button("ACTIVATE"))
                {
                    _abilityManager.RequestActivate(ability.Definition.AbilityID);
                }
            }
            else
            {
                float cd = ability.CurrentCooldown;
                GUILayout.Label($"Cooldown: {cd:F1}s");
            }

            GUILayout.EndVertical();
        }
    }
}