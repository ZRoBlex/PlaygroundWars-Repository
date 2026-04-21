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
    //  FLAG CARRIER BRIDGE
    //  Componente ligero que le indica a CaptureZone si el
    //  jugador porta una bandera. Evita acoplamiento directo.
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Añadir al prefab del jugador.
    /// Permite que CaptureZone sepa si el jugador lleva bandera.
    /// </summary>
    public class FlagCarrierBridge : MonoBehaviour
    {
        public bool   IsCarrying         { get; private set; }
        public string CarriedObjectiveID { get; private set; }
        public Flag   CarriedFlag        { get; private set; }

        public void SetCarrying(Flag flag)
        {
            CarriedFlag        = flag;
            CarriedObjectiveID = flag?.ObjectiveID ?? string.Empty;
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
