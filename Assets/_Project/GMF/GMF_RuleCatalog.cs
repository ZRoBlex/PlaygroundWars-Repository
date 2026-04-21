// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RuleCatalog.cs                             ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • KillScoreRule           (IGameRule)                 ║
// ║    • ObjectiveCaptureRule    (IGameRule)                 ║
// ║    • ObjectiveTickRule       (IGameRule) ← para KOTH     ║
// ║    • DropFlagOnDeathRule     (IGameRule) ← para CTF      ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Catálogo de reglas reutilizables entre modos.         ║
// ║    Cada regla implementa IGameRule.                      ║
// ║    Para crear TDM, FFA, CTF: combinar estas reglas       ║
// ║    en GameModeDefinitionSO SIN código nuevo.             ║
// ║                                                          ║
// ║  EXTENSIÓN:                                              ║
// ║    Para una regla nueva: implementar IGameRule en un     ║
// ║    archivo separado (ej: GMF_MyCustomRule.cs).           ║
// ║    No modificar este archivo.                            ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using Core.Events;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  KILL SCORE RULE
    //  Uso: TDM, FFA, cualquier modo con kills
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Por cada kill, da _points puntos al equipo del asesino.
    /// CONFIGURA: _points (1 = estándar, 2 = doble kill, etc.)
    /// </summary>
    [Serializable]
    public class KillScoreRule : IGameRule
    {
        public string RuleID    => "kill_score";
        public bool   IsEnabled { get; set; } = true;

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

        public void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            if (!IsEnabled || e.KillerID < 0 || e.KillerTeamID < 0) return;

            (_ctx as GameModeContext)?._score.AddScore(
                e.KillerTeamID, _points, e.KillerID, "Kill");
        }

        public void Dispose() { }
    }

    // ════════════════════════════════════════════════════════
    //  OBJECTIVE CAPTURE RULE
    //  Uso: CTF (capturar bandera), cualquier modo con objetivos
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Cuando un objetivo emite "Capture", da _points al equipo que capturó.
    /// CONFIGURA: _objectiveID para filtrar un objetivo específico.
    ///            Dejar vacío para reaccionar a cualquier captura.
    /// </summary>
    [Serializable]
    public class ObjectiveCaptureRule : IGameRule
    {
        public string RuleID    => "objective_capture";
        public bool   IsEnabled { get; set; } = true;

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        [Tooltip("ID del objetivo al que reacciona. Vacío = cualquiera.")]
        [SerializeField]
        private string _filterObjectiveID = "";

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public void OnObjectiveInteracted(ObjectiveInteractedEvt e)
        {
            if (!IsEnabled) return;
            if (e.Interaction != "Capture") return;
            if (!string.IsNullOrEmpty(_filterObjectiveID)
                && e.ObjectiveID != _filterObjectiveID) return;
            if (e.PlayerTeamID < 0) return;

            (_ctx as GameModeContext)?._score.AddScore(
                e.PlayerTeamID, _points, e.PlayerID, "Capture");

            // Solicitar reset del objetivo (Flag vuelve a base)
            EventBus<ObjectiveResetEvt>.Raise(new ObjectiveResetEvt
            {
                ObjectiveID = e.ObjectiveID
            });
        }

        public void OnPlayerEliminated(PlayerEliminatedEvt e) { }
        public void Dispose() { }
    }

    // ════════════════════════════════════════════════════════
    //  OBJECTIVE TICK RULE
    //  Uso: King of the Hill (punto por segundo en zona)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Cada "Tick" de un ControlPoint da _points al equipo ocupante.
    /// CONFIGURA: _filterObjectiveID para un punto específico.
    /// </summary>
    [Serializable]
    public class ObjectiveTickRule : IGameRule
    {
        public string RuleID    => "objective_tick";
        public bool   IsEnabled { get; set; } = true;

        [SerializeField, Range(1, 10)]
        private int _points = 1;

        [SerializeField]
        private string _filterObjectiveID = "";

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public void OnObjectiveInteracted(ObjectiveInteractedEvt e)
        {
            if (!IsEnabled) return;
            if (e.Interaction != "Tick") return;
            if (!string.IsNullOrEmpty(_filterObjectiveID)
                && e.ObjectiveID != _filterObjectiveID) return;
            if (e.PlayerTeamID < 0) return;

            (_ctx as GameModeContext)?._score.AddScore(
                e.PlayerTeamID, _points, e.PlayerID, "ControlPoint");
        }

        public void OnPlayerEliminated(PlayerEliminatedEvt e) { }
        public void Dispose() { }
    }

    // ════════════════════════════════════════════════════════
    //  DROP FLAG ON DEATH RULE
    //  Uso: CTF (si mueres portando la bandera, se cae)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Si el jugador muere portando un objetivo, lo suelta.
    /// Busca el FlagCarrierBridge del jugador y llama a Drop.
    /// </summary>
    [Serializable]
    public class DropFlagOnDeathRule : IGameRule
    {
        public string RuleID    => "drop_flag_on_death";
        public bool   IsEnabled { get; set; } = true;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public void OnObjectiveInteracted(ObjectiveInteractedEvt e) { }

        public void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            if (!IsEnabled) return;
            if (!e.WasCarryingObjective) return;

            // Buscar el objetivo que portaba y soltarlo
            var flag = (_ctx.Objectives.Get(e.CarriedObjectiveID) as Flag);
            flag?.Drop(e.VictimID);

            Core.Debug.CoreLogger.LogSystem("DropFlagOnDeathRule",
                $"P{e.VictimID} murió portando '{e.CarriedObjectiveID}'");
        }

        public void Dispose() { }
    }
}
