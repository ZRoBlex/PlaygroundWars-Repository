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
    //  FLAG CARRIER BRIDGE
    //  ► Añadir al prefab del jugador
    //  ► Permite que CaptureZone sepa si el jugador lleva flag
    //  ► Flag.DoPickUp() / DoClear() lo actualiza automáticamente
    // ════════════════════════════════════════════════════════

    // public class FlagCarrierBridge : MonoBehaviour
    // {
    //     public bool   IsCarrying         { get; private set; }
    //     public Flag   CarriedFlag        { get; private set; }
    //     public string CarriedObjectiveID { get; private set; }

    //     public void SetCarrying(Flag flag)
    //     {
    //         CarriedFlag        = flag;
    //         CarriedObjectiveID = flag != null ? flag.ObjectiveID : string.Empty;
    //         IsCarrying         = flag != null;
    //     }

    //     public void ClearCarrying()
    //     {
    //         CarriedFlag        = null;
    //         CarriedObjectiveID = string.Empty;
    //         IsCarrying         = false;
    //     }
    // }

    // ════════════════════════════════════════════════════════
    //  CAPTURE ZONE
    //  ► Añadir al GameObject de cada base
    //  ► TeamID = 0 → base del equipo Red; TeamID = 1 → Blue
    //  ► Asegurar Collider con isTrigger = true
    // ════════════════════════════════════════════════════════

    // [RequireComponent(typeof(Collider))]
    // public class CaptureZone : ObjectiveBase
    // {
    //     protected override void Start()
    //     {
    //         GetComponent<Collider>().isTrigger = true;
    //         base.Start();
    //     }

    //     private void OnTriggerEnter(Collider other)
    //     {
    //         if (!IsActive) return;

    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;

    //         int pid   = auth.PlayerID;
    //         int pTeam = GetPlayerTeam(pid);
    //         if (pTeam < 0) return;

    //         // ── Solo emitir "Capture" si se cumplen las 3 condiciones ──
    //         //  1. El jugador está en SU propia base (misma TeamID que la zona)
    //         //  2. El jugador lleva una bandera
    //         //  3. La bandera es del equipo ENEMIGO

    //         var bridge = other.GetComponentInParent<FlagCarrierBridge>();

    //         if (pTeam == _teamID                              // 1. base propia
    //             && bridge != null && bridge.IsCarrying        // 2. lleva algo
    //             && bridge.CarriedFlag != null                 // 3a. hay flag
    //             && bridge.CarriedFlag.TeamID != pTeam)        // 3b. flag enemiga
    //         {
    //             // ✅ Captura válida — incluimos el ID de la bandera en el evento
    //             // para que ObjectiveCaptureRule pueda resetearla
    //             EmitInteractionWithCarried("Capture", pid, pTeam, bridge.CarriedObjectiveID);
    //         }
    //         else
    //         {
    //             // Solo "Enter" para otros efectos / reglas futuras
    //             EmitInteraction("Enter", pid, pTeam);
    //         }
    //     }

    //     private void OnTriggerExit(Collider other)
    //     {
    //         if (!IsActive) return;
    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;
    //         EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
    //     }

    //     public override void Reset() { State = "Idle"; }

    //     // ── Helper: emite el evento con CarriedObjectiveID ────

    //     private void EmitInteractionWithCarried(
    //         string interaction, int playerID, int playerTeamID, string carriedObjectiveID)
    //     {
    //         if (!IsActive) return;

    //         EventBus<ObjectiveInteractedEvt>.Raise(new ObjectiveInteractedEvt
    //         {
    //             ObjectiveID        = _objectiveID,
    //             Interaction        = interaction,
    //             PlayerID           = playerID,
    //             PlayerTeamID       = playerTeamID,
    //             ObjectiveTeamID    = _teamID,
    //             Position           = transform.position,
    //             CarriedObjectiveID = carriedObjectiveID
    //         });
    //     }

    //     private int GetPlayerTeam(int pid)
    //         => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

    //     private void OnDrawGizmosSelected()
    //     {
    //         Gizmos.color = _teamID == 0
    //             ? new Color(1f, 0.2f, 0.2f, 0.3f)
    //             : new Color(0.2f, 0.2f, 1f, 0.3f);
    //         Gizmos.DrawCube(transform.position, transform.localScale);
    //     }
    // }

    // ════════════════════════════════════════════════════════
    //  CONTROL POINT
    //  ► Para modos KOTH
    //  ► Emite "Tick" cada _tickInterval segundos si hay alguien
    // ════════════════════════════════════════════════════════

    // [RequireComponent(typeof(Collider))]
    // public class ControlPoint : ObjectiveBase
    // {
    //     [Header("Control Point")]
    //     [Tooltip("Segundos entre Tick events.")]
    //     [SerializeField] private float _tickInterval = 1f;

    //     private readonly HashSet<int> _occupants = new();
    //     private float                 _tickTimer;

    //     protected override void Start()
    //     {
    //         GetComponent<Collider>().isTrigger = true;
    //         base.Start();
    //     }

    //     // UPDATE JUSTIFICADO: acumular tiempo para emitir Tick periódico.
    //     private void Update()
    //     {
    //         if (!IsActive || _occupants.Count == 0) return;
    //         _tickTimer += Time.deltaTime;
    //         if (_tickTimer < _tickInterval) return;
    //         _tickTimer = 0f;

    //         foreach (int pid in _occupants)
    //         {
    //             EmitInteraction("Tick", pid, GetPlayerTeam(pid));
    //             break;
    //         }
    //     }

    //     private void OnTriggerEnter(Collider other)
    //     {
    //         if (!IsActive) return;
    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;
    //         int pid = auth.PlayerID;
    //         if (!_occupants.Add(pid)) return;
    //         State = "Contested";
    //         EmitInteraction("Enter", pid, GetPlayerTeam(pid));
    //     }

    //     private void OnTriggerExit(Collider other)
    //     {
    //         if (!IsActive) return;
    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;
    //         int pid = auth.PlayerID;
    //         if (!_occupants.Remove(pid)) return;
    //         State = _occupants.Count == 0 ? "Idle" : "Contested";
    //         EmitInteraction("Exit", pid, GetPlayerTeam(pid));
    //     }

    //     public override void Reset()
    //     {
    //         _occupants.Clear();
    //         _tickTimer = 0f;
    //         State = "Idle";
    //     }

    //     private int GetPlayerTeam(int pid)
    //         => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

    //     private void OnDrawGizmosSelected()
    //     {
    //         Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
    //         Gizmos.DrawSphere(transform.position, transform.localScale.x * 0.5f);
    //     }
    // }
}
