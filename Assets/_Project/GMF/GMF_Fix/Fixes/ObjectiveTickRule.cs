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
    //  OBJECTIVE TICK RULE
    //  Uso: King of the Hill
    //  Cada "Tick" de ControlPoint → punto al equipo ocupante
    // ════════════════════════════════════════════════════════

    [CreateAssetMenu(
        fileName = "ObjectiveTickRule",
        menuName = "GameMode Framework/Rules/Objective Tick Rule")]
    public class ObjectiveTickRule : RuleBaseSO
    {
        public override string RuleID => "objective_tick";

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        [Tooltip("Vacío = reacciona a cualquier ControlPoint.")]
        [SerializeField]
        private string _filterObjectiveID = "";

        private IGameModeContext _ctx;

        public override void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public override void OnObjectiveInteracted(ObjectiveInteractedEvt e)
        {
            if (!IsEnabled || e.Interaction != "Tick") return;
            if (!string.IsNullOrEmpty(_filterObjectiveID) && e.ObjectiveID != _filterObjectiveID) return;
            if (e.PlayerTeamID < 0) return;

            (_ctx as GameModeContext)?._score.AddScore(
                e.PlayerTeamID, _points, e.PlayerID, "ControlPoint");
        }

        public override void OnPlayerEliminated(PlayerEliminatedEvt e) { }
    }
}
