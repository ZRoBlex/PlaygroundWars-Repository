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
    //  DROP FLAG ON DEATH RULE
    //  Uso: CTF
    //  Si el portador muere → la bandera cae al suelo
    // ════════════════════════════════════════════════════════

    [CreateAssetMenu(
        fileName = "DropFlagOnDeathRule",
        menuName = "GameMode Framework/Rules/Drop Flag On Death Rule")]
    public class DropFlagOnDeathRule : RuleBaseSO
    {
        public override string RuleID => "drop_flag_on_death";

        private IGameModeContext _ctx;

        public override void Initialize(IGameModeContext ctx) => _ctx = ctx;
        public override void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

        public override void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            if (!IsEnabled || !e.WasCarryingObjective) return;

            var flag = _ctx?.Objectives.Get(e.CarriedObjectiveID) as Flag;
            if (flag == null) return;

            flag.Drop(e.VictimID);

            CoreLogger.LogSystem("DropFlagOnDeathRule",
                $"P{e.VictimID} murió portando '{e.CarriedObjectiveID}'");
        }
    }
}
