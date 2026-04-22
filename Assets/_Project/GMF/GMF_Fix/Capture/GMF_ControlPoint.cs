// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Zones.cs  (REEMPLAZA el anterior)          ║
// ║                                                          ║
// ║  CLASES:                                                 ║
// ║    • FlagCarrierBridge  ← añadir al prefab del jugador   ║
// ║    • CaptureZone        ← añadir a las bases             ║
// ║    • ControlPoint       ← añadir a puntos de control     ║
// ║                                                          ║
// ║  ⚠️ SEPARAR: un archivo por clase (mismo namespace GMF) ║
// ║                                                          ║
// ║  FIX PRINCIPAL:                                          ║
// ║    CaptureZone ya NO requiere FlagCarrierBridge.         ║
// ║    Consulta la lista de flags en ObjectiveRegistry para  ║
// ║    saber si el jugador porta una bandera enemiga.        ║
// ║    Así funciona aunque el jugador no tenga el bridge.    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  CONTROL POINT (King of the Hill)
    // ════════════════════════════════════════════════════════

    [RequireComponent(typeof(Collider))]
    public class ControlPoint : ObjectiveBase
    {
        [Header("Control Point")]
        [SerializeField] private float _tickInterval = 1f;

        private readonly HashSet<int> _occupants  = new();
        private float                 _tickTimer;

        protected override void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            base.Start();
        }

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
            if (_occupants.Add(auth.PlayerID))
            {
                State = "Contested";
                EmitInteraction("Enter", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            if (_occupants.Remove(auth.PlayerID))
            {
                State = _occupants.Count == 0 ? "Idle" : "Contested";
                EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
            }
        }

        public override void Reset()
        {
            _occupants.Clear();
            _tickTimer = 0f;
            State      = "Idle";
        }

        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawSphere(transform.position, transform.localScale.x * 0.5f);
        }
    }
}