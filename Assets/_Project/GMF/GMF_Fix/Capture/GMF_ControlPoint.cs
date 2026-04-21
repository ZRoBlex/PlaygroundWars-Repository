// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Zones.cs  (REEMPLAZA el anterior)          ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • CaptureZone      (MonoBehaviour)                    ║
// ║    • ControlPoint     (MonoBehaviour)                    ║
// ║    • FlagCarrierBridge (MonoBehaviour)                   ║
// ║                                                          ║
// ║  ⚠️ SEPARAR (lo haces tú):                              ║
// ║    GMF_CaptureZone.cs      → se añade a GameObject zona  ║
// ║    GMF_ControlPoint.cs     → se añade a GameObject punto ║
// ║    GMF_FlagCarrierBridge.cs → se añade al prefab jugador ║
// ║    Mismo namespace GMF en los 3 archivos.                ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + CaptureZone valida que el jugador lleve una bandera  ║
// ║      enemiga ANTES de emitir "Capture"                   ║
// ║    + El evento incluye CarriedObjectiveID                 ║
// ║      (ObjectiveCaptureRule lo usa para resetear la flag)  ║
// ║    - GetPlayerTeam usa GameModeBase.Instance              ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  CONTROL POINT
    //  ► Para modos KOTH
    //  ► Emite "Tick" cada _tickInterval segundos si hay alguien
    // ════════════════════════════════════════════════════════

    [RequireComponent(typeof(Collider))]
    public class ControlPoint : ObjectiveBase
    {
        [Header("Control Point")]
        [Tooltip("Segundos entre Tick events.")]
        [SerializeField] private float _tickInterval = 1f;

        private readonly HashSet<int> _occupants = new();
        private float                 _tickTimer;

        protected override void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            base.Start();
        }

        // UPDATE JUSTIFICADO: acumular tiempo para emitir Tick periódico.
        private void Update()
        {
            if (!IsActive || _occupants.Count == 0) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval) return;
            _tickTimer = 0f;

            foreach (int pid in _occupants)
            {
                EmitInteraction("Tick", pid, GetPlayerTeam(pid));
                break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            int pid = auth.PlayerID;
            if (!_occupants.Add(pid)) return;
            State = "Contested";
            EmitInteraction("Enter", pid, GetPlayerTeam(pid));
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            int pid = auth.PlayerID;
            if (!_occupants.Remove(pid)) return;
            State = _occupants.Count == 0 ? "Idle" : "Contested";
            EmitInteraction("Exit", pid, GetPlayerTeam(pid));
        }

        public override void Reset()
        {
            _occupants.Clear();
            _tickTimer = 0f;
            State = "Idle";
        }

        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, transform.localScale.x * 0.5f);
        }
    }
}
