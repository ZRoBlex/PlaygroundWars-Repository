using UnityEngine;

namespace AbilitySystem
{
    [DisallowMultipleComponent]
    public class AbilityNetworkSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // CONFIG
        // ─────────────────────────────────────────────

        [Header("Networking")]
        [SerializeField] private bool _isServer = true; // 🔥 SIMULACIÓN

        // ─────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────

        private void OnEnable()
        {
            AbilityEvents.OnAbilityRequested += OnAbilityRequested;
        }

        private void OnDisable()
        {
            AbilityEvents.OnAbilityRequested -= OnAbilityRequested;
        }

        // ─────────────────────────────────────────────
        // CLIENT → SERVER REQUEST
        // ─────────────────────────────────────────────

        private void OnAbilityRequested(AbilityEventData data)
        {
            if (_isServer)
            {
                // Si ya somos server, ejecutamos directo
                ServerExecuteAbility(data);
            }
            else
            {
                // Cliente → enviar al servidor
                SendToServer(data);
            }
        }

        // ─────────────────────────────────────────────
        // CLIENT SIDE
        // ─────────────────────────────────────────────

        private void SendToServer(AbilityEventData data)
        {
            // 🔥 AQUÍ VA TU LIBRERÍA DE NETWORKING
            // Ejemplo:
            // NetworkManager.Send(data);

            Debug.Log($"[Network] Sending ability request to server: {data.AbilityID}");
        }

        // ─────────────────────────────────────────────
        // SERVER SIDE
        // ─────────────────────────────────────────────

        private void ServerExecuteAbility(AbilityEventData data)
        {
            // 🔥 AQUÍ BUSCAS EL MANAGER DEL PLAYER
            var manager = FindAbilityManager(data.SourceID);

            if (manager == null)
            {
                Debug.LogWarning("[Network] AbilityManager not found");
                return;
            }

            var ability = manager.GetAbility(data.AbilityID);

            if (ability == null)
            {
                Debug.LogWarning("[Network] Ability not found");
                return;
            }

            // 🔥 EJECUCIÓN REAL (AUTORITATIVA)
            AbilityExecutionSystem.ExecuteAbility(ability);

            // 🔥 REPLICAR RESULTADO A CLIENTES
            BroadcastResult(data);
        }

        // ─────────────────────────────────────────────
        // SERVER → CLIENTS
        // ─────────────────────────────────────────────

        private void BroadcastResult(AbilityEventData data)
        {
            // 🔥 AQUÍ VA TU LIBRERÍA DE NETWORKING
            // Ejemplo:
            // NetworkManager.Broadcast(data);

            Debug.Log($"[Network] Broadcasting ability result: {data.AbilityID}");
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────

        private AbilityManager FindAbilityManager(int playerID)
        {
            var managers = FindObjectsOfType<AbilityManager>();

            foreach (var m in managers)
            {
                var authority = m.GetComponent<Player.Authority.PlayerAuthority>();

                if (authority != null && authority.PlayerID == playerID)
                    return m;
            }

            return null;
        }
    }
}