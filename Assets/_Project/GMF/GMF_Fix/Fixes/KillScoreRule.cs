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
    //  KILL SCORE RULE
    //  Uso: TDM, FFA
    //  Cada kill da _points puntos al equipo asesino
    // ════════════════════════════════════════════════════════

    [CreateAssetMenu(
        fileName = "KillScoreRule",
        menuName = "GameMode Framework/Rules/Kill Score Rule")]
    public class KillScoreRule : RuleBaseSO
    {
        public override string RuleID => "kill_score";

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        private IGameModeContext _ctx;

        public override void Initialize(IGameModeContext ctx) => _ctx = ctx;
        public override void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

        public override void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            if (!IsEnabled || e.KillerID < 0 || e.KillerTeamID < 0) return;
            (_ctx as GameModeContext)?._score.AddScore(e.KillerTeamID, _points, e.KillerID, "Kill");
        }
    }
}
