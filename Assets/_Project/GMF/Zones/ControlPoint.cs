// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Zones.cs                                   ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • CaptureZone  (MonoBehaviour)  ← principal           ║
// ║    • ControlPoint (MonoBehaviour)  ← secundaria          ║
// ║                                                          ║
// ║  ⚠️ SEPARACIÓN REQUERIDA:                               ║
// ║    CaptureZone  → mover a CaptureZone.cs si crece > 100L ║
// ║    ControlPoint → mover a ControlPoint.cs si crece > 150L║
// ║                                                          ║
// ║  CaptureZone — RESPONSABILIDAD:                          ║
// ║    Trigger que emite "Enter"/"Exit"/"Capture".           ║
// ║    Usado en CTF: el jugador con bandera entra → Capture. ║
// ║    La regla (no esta clase) decide si es captura válida. ║
// ║                                                          ║
// ║  ControlPoint — RESPONSABILIDAD:                         ║
// ║    Punto de control estilo KOTH.                         ║
// ║    Emite "Enter","Exit","Tick" (cada segundo si ocupado).║
// ║    La regla decide cuándo dar puntos.                    ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    CaptureZone:                                          ║
// ║      1. GameObject + BoxCollider (trigger)               ║
// ║      2. Añadir CaptureZone.cs                            ║
// ║      3. ObjectiveID = "base_red", TeamID = 0             ║
// ║    ControlPoint:                                         ║
// ║      1. GameObject + SphereCollider (trigger)            ║
// ║      2. Añadir ControlPoint.cs                           ║
// ║      3. TickInterval = 1.0 (segundos entre Tick)         ║
// ║                                                          ║
// ║  UPDATE JUSTIFICADO (ControlPoint):                      ║
// ║    El timer de "Tick" necesita acumular tiempo.          ║
// ║    No se puede evitar sin coroutine (que también usa      ║
// ║    tiempo). Se elige Update por simplicidad y control.   ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(Collider))]
    public class ControlPoint : ObjectiveBase
    {
        [Header("Control Point")]
        [Tooltip("Segundos entre Tick events cuando hay ocupante.")]
        [SerializeField] private float _tickInterval = 1f;

        private readonly HashSet<int> _occupants = new();
        private float                 _tickTimer;

        protected override void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            base.Start();
        }

        // UPDATE JUSTIFICADO: acumular tiempo para emitir Tick a intervalos.
        private void Update()
        {
            if (!IsActive || _occupants.Count == 0) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval) return;

            _tickTimer = 0f;
            // Emitir Tick con el primer ocupante (las reglas calculan el equipo dominante)
            foreach (var pid in _occupants)
            {
                int pTeam = GetPlayerTeam(pid);
                EmitInteraction("Tick", pid, pTeam);
                break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;

            int pid = auth.PlayerID;
            if (_occupants.Add(pid))
            {
                State = _occupants.Count > 0 ? "Contested" : "Idle";
                EmitInteraction("Enter", pid, GetPlayerTeam(pid));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;

            int pid = auth.PlayerID;
            if (_occupants.Remove(pid))
            {
                State = _occupants.Count == 0 ? "Idle" : "Contested";
                EmitInteraction("Exit", pid, GetPlayerTeam(pid));
            }
        }

        public override void Reset()
        {
            _occupants.Clear();
            _tickTimer = 0f;
            State = "Idle";
        }

        private int GetPlayerTeam(int pid)
        {
            var gm = FindFirstObjectByType<GameModeBase>();
            return gm?.Context?.Teams?.GetTeam(pid) ?? -1;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, transform.localScale.x * 0.5f);
        }
    }
}
