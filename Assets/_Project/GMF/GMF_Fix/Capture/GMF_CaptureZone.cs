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
using Core.Events;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  CAPTURE ZONE
    //  ► Cuando el jugador entra, verifica en el registro si
    //    porta una bandera enemiga → emite "Capture"
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

            // ✅ Buscar si este jugador porta alguna bandera enemiga
            // Consulta el registro — no necesita FlagCarrierBridge
            Flag carriedEnemyFlag = FindCarriedEnemyFlag(pid, pTeam);

            if (pTeam == _teamID && carriedEnemyFlag != null)
            {
                // ✅ Captura válida: jugador en su base portando bandera enemiga
                EmitInteractionWithCarried("Capture", pid, pTeam, carriedEnemyFlag.ObjectiveID);
            }
            else
            {
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

        // ── Busca en el registro una bandera portada por este jugador ─

        private Flag FindCarriedEnemyFlag(int playerID, int playerTeamID)
        {
            var gm = GameModeBase.Instance;
            if (gm?.Context?.Objectives == null) return null;

            var objs = gm.Context.Objectives.GetAll();
            foreach (var obj in objs)
            {
                var flag = obj as Flag;
                if (flag == null) continue;
                if (!flag.IsBeingCarried) continue;
                if (flag.CarrierID != playerID) continue;
                if (flag.TeamID == playerTeamID) continue; // es su propia bandera
                return flag; // bandera enemiga portada por este jugador
            }
            return null;
        }

        // ── Emitir con ID de bandera ──────────────────────────

        private void EmitInteractionWithCarried(
            string interaction, int playerID, int playerTeamID, string carriedID)
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
                CarriedObjectiveID = carriedID
            });
        }

        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _teamID == 0
                ? new Color(1f, 0.2f, 0.2f, 0.5f)
                : new Color(0.2f, 0.2f, 1f, 0.5f);
            Gizmos.DrawCube(transform.position, transform.localScale);
            Gizmos.color = _teamID == 0 ? Color.red : Color.blue;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}