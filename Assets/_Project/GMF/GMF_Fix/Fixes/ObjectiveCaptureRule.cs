// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RuleCatalog.cs  (REEMPLAZA el anterior)    ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • KillScoreRule        (RuleBaseSO)                   ║
// ║    • ObjectiveCaptureRule (RuleBaseSO)                   ║
// ║    • ObjectiveTickRule    (RuleBaseSO)                   ║
// ║    • DropFlagOnDeathRule  (RuleBaseSO)                   ║
// ║                                                          ║
// ║  ⚠️ SEPARAR: cada clase en su propio archivo             ║
// ║    Motivo: son ScriptableObjects con [CreateAssetMenu],  ║
// ║    conviene tener un archivo por clase para claridad     ║
// ║    en la ventana de creación de assets.                  ║
// ║                                                          ║
// ║  CÓMO USAR:                                              ║
// ║    Assets → Create → GameMode Framework → Rules → [nombre]
// ║    Arrastra el asset al array Rules del GameModeDefinitionSO
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  OBJECTIVE CAPTURE RULE
    //  Uso: CTF, cualquier modo con objetivos capturables
    //  Cuando CaptureZone emite "Capture" → punto al equipo
    // ════════════════════════════════════════════════════════

    [CreateAssetMenu(
        fileName = "ObjectiveCaptureRule",
        menuName = "GameMode Framework/Rules/Objective Capture Rule")]
    public class ObjectiveCaptureRule : RuleBaseSO
    {
        public override string RuleID => "objective_capture";

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        private IGameModeContext _ctx;

        public override void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public override void OnObjectiveInteracted(ObjectiveInteractedEvt e)
        {
            if (!IsEnabled) return;
            if (e.Interaction != "Capture") return;
            if (e.PlayerTeamID < 0) return;

            (_ctx as GameModeContext)?._score.AddScore(
                e.PlayerTeamID, _points, e.PlayerID, "Capture");

            // Solicitar reset de la bandera que el jugador portaba
            // La CaptureZone incluye el CarriedObjectiveID en el evento
            if (!string.IsNullOrEmpty(e.CarriedObjectiveID))
            {
                EventBus<ObjectiveResetEvt>.Raise(new ObjectiveResetEvt
                {
                    ObjectiveID = e.CarriedObjectiveID
                });
            }
        }

        public override void OnPlayerEliminated(PlayerEliminatedEvt e) { }
    }
}
