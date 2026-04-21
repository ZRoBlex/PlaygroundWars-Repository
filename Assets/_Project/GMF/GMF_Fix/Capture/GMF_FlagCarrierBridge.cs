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

    public class FlagCarrierBridge : MonoBehaviour
    {
        public bool   IsCarrying         { get; private set; }
        public Flag   CarriedFlag        { get; private set; }
        public string CarriedObjectiveID { get; private set; }

        public void SetCarrying(Flag flag)
        {
            CarriedFlag        = flag;
            CarriedObjectiveID = flag != null ? flag.ObjectiveID : string.Empty;
            IsCarrying         = flag != null;
        }

        public void ClearCarrying()
        {
            CarriedFlag        = null;
            CarriedObjectiveID = string.Empty;
            IsCarrying         = false;
        }
    }
}
