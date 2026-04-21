// // ╔══════════════════════════════════════════════════════════╗
// // ║  ARCHIVO: GMF_RuleCatalog.cs  (REEMPLAZA el anterior)    ║
// // ║                                                          ║
// // ║  CLASES INCLUIDAS:                                       ║
// // ║    • KillScoreRule        (RuleBaseSO)                   ║
// // ║    • ObjectiveCaptureRule (RuleBaseSO)                   ║
// // ║    • ObjectiveTickRule    (RuleBaseSO)                   ║
// // ║    • DropFlagOnDeathRule  (RuleBaseSO)                   ║
// // ║                                                          ║
// // ║  ⚠️ SEPARAR: cada clase en su propio archivo             ║
// // ║    Motivo: son ScriptableObjects con [CreateAssetMenu],  ║
// // ║    conviene tener un archivo por clase para claridad     ║
// // ║    en la ventana de creación de assets.                  ║
// // ║                                                          ║
// // ║  CÓMO USAR:                                              ║
// // ║    Assets → Create → GameMode Framework → Rules → [nombre]
// // ║    Arrastra el asset al array Rules del GameModeDefinitionSO
// // ╚══════════════════════════════════════════════════════════╝

// using Core.Debug;
// using Core.Events;
// using UnityEngine;

// namespace GMF
// {
//     // ════════════════════════════════════════════════════════
//     //  KILL SCORE RULE
//     //  Uso: TDM, FFA
//     //  Cada kill da _points puntos al equipo asesino
//     // ════════════════════════════════════════════════════════

//     [CreateAssetMenu(
//         fileName = "KillScoreRule",
//         menuName = "GameMode Framework/Rules/Kill Score Rule")]
//     public class KillScoreRule : RuleBaseSO
//     {
//         public override string RuleID => "kill_score";

//         [SerializeField, Range(1, 10)]
//         private int _points = 1;

//         private IGameModeContext _ctx;

//         public override void Initialize(IGameModeContext ctx) => _ctx = ctx;
//         public override void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

//         public override void OnPlayerEliminated(PlayerEliminatedEvt e)
//         {
//             if (!IsEnabled || e.KillerID < 0 || e.KillerTeamID < 0) return;
//             (_ctx as GameModeContext)?._score.AddScore(e.KillerTeamID, _points, e.KillerID, "Kill");
//         }
//     }

//     // ════════════════════════════════════════════════════════
//     //  OBJECTIVE CAPTURE RULE
//     //  Uso: CTF, cualquier modo con objetivos capturables
//     //  Cuando CaptureZone emite "Capture" → punto al equipo
//     // ════════════════════════════════════════════════════════

//     [CreateAssetMenu(
//         fileName = "ObjectiveCaptureRule",
//         menuName = "GameMode Framework/Rules/Objective Capture Rule")]
//     public class ObjectiveCaptureRule : RuleBaseSO
//     {
//         public override string RuleID => "objective_capture";

//         [SerializeField, Range(1, 10)]
//         private int _points = 1;

//         private IGameModeContext _ctx;

//         public override void Initialize(IGameModeContext ctx) => _ctx = ctx;

//         public override void OnObjectiveInteracted(ObjectiveInteractedEvt e)
//         {
//             if (!IsEnabled) return;
//             if (e.Interaction != "Capture") return;
//             if (e.PlayerTeamID < 0) return;

//             (_ctx as GameModeContext)?._score.AddScore(
//                 e.PlayerTeamID, _points, e.PlayerID, "Capture");

//             // Solicitar reset de la bandera que el jugador portaba
//             // La CaptureZone incluye el CarriedObjectiveID en el evento
//             if (!string.IsNullOrEmpty(e.CarriedObjectiveID))
//             {
//                 EventBus<ObjectiveResetEvt>.Raise(new ObjectiveResetEvt
//                 {
//                     ObjectiveID = e.CarriedObjectiveID
//                 });
//             }
//         }

//         public override void OnPlayerEliminated(PlayerEliminatedEvt e) { }
//     }

//     // ════════════════════════════════════════════════════════
//     //  OBJECTIVE TICK RULE
//     //  Uso: King of the Hill
//     //  Cada "Tick" de ControlPoint → punto al equipo ocupante
//     // ════════════════════════════════════════════════════════

//     [CreateAssetMenu(
//         fileName = "ObjectiveTickRule",
//         menuName = "GameMode Framework/Rules/Objective Tick Rule")]
//     public class ObjectiveTickRule : RuleBaseSO
//     {
//         public override string RuleID => "objective_tick";

//         [SerializeField, Range(1, 10)]
//         private int _points = 1;

//         [Tooltip("Vacío = reacciona a cualquier ControlPoint.")]
//         [SerializeField]
//         private string _filterObjectiveID = "";

//         private IGameModeContext _ctx;

//         public override void Initialize(IGameModeContext ctx) => _ctx = ctx;

//         public override void OnObjectiveInteracted(ObjectiveInteractedEvt e)
//         {
//             if (!IsEnabled || e.Interaction != "Tick") return;
//             if (!string.IsNullOrEmpty(_filterObjectiveID) && e.ObjectiveID != _filterObjectiveID) return;
//             if (e.PlayerTeamID < 0) return;

//             (_ctx as GameModeContext)?._score.AddScore(
//                 e.PlayerTeamID, _points, e.PlayerID, "ControlPoint");
//         }

//         public override void OnPlayerEliminated(PlayerEliminatedEvt e) { }
//     }

//     // ════════════════════════════════════════════════════════
//     //  DROP FLAG ON DEATH RULE
//     //  Uso: CTF
//     //  Si el portador muere → la bandera cae al suelo
//     // ════════════════════════════════════════════════════════

//     [CreateAssetMenu(
//         fileName = "DropFlagOnDeathRule",
//         menuName = "GameMode Framework/Rules/Drop Flag On Death Rule")]
//     public class DropFlagOnDeathRule : RuleBaseSO
//     {
//         public override string RuleID => "drop_flag_on_death";

//         private IGameModeContext _ctx;

//         public override void Initialize(IGameModeContext ctx) => _ctx = ctx;
//         public override void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

//         public override void OnPlayerEliminated(PlayerEliminatedEvt e)
//         {
//             if (!IsEnabled || !e.WasCarryingObjective) return;

//             var flag = _ctx?.Objectives.Get(e.CarriedObjectiveID) as Flag;
//             if (flag == null) return;

//             flag.Drop(e.VictimID);

//             CoreLogger.LogSystem("DropFlagOnDeathRule",
//                 $"P{e.VictimID} murió portando '{e.CarriedObjectiveID}'");
//         }
//     }
// }
