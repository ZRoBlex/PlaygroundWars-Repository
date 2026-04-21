// ============================================================
//  Objectives.cs
//  GameModeFramework/Objectives/Objectives.cs
//
//  OBJETIVOS REUTILIZABLES DEL FRAMEWORK.
//  No conocen las reglas. Solo emiten eventos de interacción.
//
//  CONTENIDO:
//  • ObjectiveBase  — base MonoBehaviour para todos los objetivos
//  • Flag           — bandera que se puede recoger/soltar/capturar
//  • CaptureZone    — zona de captura (trigger)
//  • ControlPoint   — punto de control (KOTH) — estructura base
// ============================================================

using GameMode.Framework.Config;
using GameMode.Framework.Events;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace GameMode.Framework.Objectives
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

            // Publicar interacción — las reglas deciden si es captura válida
            // (ej: FlagCaptureRule valida que el jugador lleve bandera enemiga
            //  y está en su propia zona)
            // var carrier = other.GetComponentInParent<CTF.Flag.FlagCarrierComponent>();

            // EmitInteraction(
            //     carrier?.IsCarrying == true ? "Capture" : "Enter",
            //     auth.PlayerID,
            //     GetPlayerTeam(auth.PlayerID)
            // );
        }

        public override void Reset() { CurrentState = "Idle"; }

        // private int GetPlayerTeam(int pid)
        // {
        //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
        //     return tm?.GetTeamOf(pid) ?? -1;
        // }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _ownerTeamID == 0
                ? new Color(1f, 0.2f, 0.2f, 0.3f)
                : new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, 2f);
        }
    }
}
