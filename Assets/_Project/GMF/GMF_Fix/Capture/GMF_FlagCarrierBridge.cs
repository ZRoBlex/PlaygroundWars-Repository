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
    //  FLAG CARRIER BRIDGE (OPCIONAL)
    //  ► Añadir al prefab del jugador si quieres
    //  ► CaptureZone ya no depende de él
    // ════════════════════════════════════════════════════════

    public class FlagCarrierBridge : MonoBehaviour
    {
        public bool   IsCarrying         { get; private set; }
        public Flag   CarriedFlag        { get; private set; }
        public string CarriedObjectiveID { get; private set; }
        [SerializeField] private Transform _carryPoint;

        public Transform CarryPoint => _carryPoint;

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