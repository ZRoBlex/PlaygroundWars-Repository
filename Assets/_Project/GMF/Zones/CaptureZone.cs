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
    // ════════════════════════════════════════════════════════
    //  CAPTURE ZONE
    // ════════════════════════════════════════════════════════

    [RequireComponent(typeof(Collider))]
    public class CaptureZone : ObjectiveBase
    {
        protected override void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            base.Start();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;

            int pid   = auth.PlayerID;
            int pTeam = GetPlayerTeam(pid);

            // "Enter" siempre. Las reglas deciden si hay captura.
            EmitInteraction("Enter", pid, pTeam);

            // Si el jugador porta un objetivo → es intento de captura
            var carrier = auth.GetComponent<FlagCarrierBridge>();
            if (carrier != null && carrier.IsCarrying)
                EmitInteraction("Capture", pid, pTeam);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
        }

        public override void Reset() { State = "Idle"; }

        private int GetPlayerTeam(int pid)
        {
            var gm = FindFirstObjectByType<GameModeBase>();
            return gm?.Context?.Teams?.GetTeam(pid) ?? -1;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _teamID == 0
                ? new Color(1f, 0.2f, 0.2f, 0.3f)
                : new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}
