// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_CaptureZone.cs                            ║
// ║                                                          ║
// ║  CLASE INCLUIDA:                                         ║
// ║    • CaptureZone      (MonoBehaviour)                    ║
// ║                                                          ║
// ║  USO:                                                    ║
// ║    GMF_CaptureZone.cs      → se añade a GameObject zona  ║
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
using Core.Events;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  CAPTURE ZONE
    //  ► Añadir al GameObject de cada base
    //  ► TeamID = 0 → base del equipo Red; TeamID = 1 → Blue
    //  ► Asegurar Collider con isTrigger = true
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
            if (pTeam < 0) return;

            // ── Solo emitir "Capture" si se cumplen las 3 condiciones ──
            //  1. El jugador está en SU propia base (misma TeamID que la zona)
            //  2. El jugador lleva una bandera
            //  3. La bandera es del equipo ENEMIGO

            var bridge = other.GetComponentInParent<FlagCarrierBridge>();

            if (pTeam == _teamID                              // 1. base propia
                && bridge != null && bridge.IsCarrying        // 2. lleva algo
                && bridge.CarriedFlag != null                 // 3a. hay flag
                && bridge.CarriedFlag.TeamID != pTeam)        // 3b. flag enemiga
            {
                // ✅ Captura válida — incluimos el ID de la bandera en el evento
                // para que ObjectiveCaptureRule pueda resetearla
                EmitInteractionWithCarried("Capture", pid, pTeam, bridge.CarriedObjectiveID);
            }
            else
            {
                // Solo "Enter" para otros efectos / reglas futuras
                EmitInteraction("Enter", pid, pTeam);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
        }

        public override void Reset() { State = "Idle"; }

        // ── Helper: emite el evento con CarriedObjectiveID ────

        private void EmitInteractionWithCarried(
            string interaction, int playerID, int playerTeamID, string carriedObjectiveID)
        {
            if (!IsActive) return;

            EventBus<ObjectiveInteractedEvt>.Raise(new ObjectiveInteractedEvt
            {
                ObjectiveID        = _objectiveID,
                Interaction        = interaction,
                PlayerID           = playerID,
                PlayerTeamID       = playerTeamID,
                ObjectiveTeamID    = _teamID,
                Position           = transform.position,
                CarriedObjectiveID = carriedObjectiveID
            });
        }

        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _teamID == 0
                ? new Color(1f, 0.2f, 0.2f, 0.3f)
                : new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}
