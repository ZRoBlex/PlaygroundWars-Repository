// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RuleEngine.cs                              ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • RuleEngine              (class, C# puro) ← principal║
// ║    • WinConditionEvaluator   (class, C# puro)            ║
// ║                                                          ║
// ║  ⚠️ SEPARACIÓN REQUERIDA:                               ║
// ║    Ambas clases están aquí porque son pequeñas y         ║
// ║    comparten la misma responsabilidad: "procesar         ║
// ║    lógica de juego al ocurrir eventos".                  ║
// ║    Si cualquiera supera 150 líneas → separar.            ║
// ║                                                          ║
// ║  RuleEngine — RESPONSABILIDAD:                           ║
// ║    Distribuir eventos a las IGameRule activas.           ║
// ║    NO evalúa victoria. Solo reparte eventos.             ║
// ║                                                          ║
// ║  WinConditionEvaluator — RESPONSABILIDAD:                ║
// ║    Evaluar IWinCondition[] al cambiar el score.          ║
// ║    NUNCA en Update(). Solo cuando ScoreChangedEvt ocurre.║
// ║                                                          ║
// ║  SERVER AUTHORITY:                                       ║
// ║    Ambas clases solo se instancian en el servidor.       ║
// ║    El cliente no tiene RuleEngine ni WinConditionEvaluator.
// ╚══════════════════════════════════════════════════════════╝

using System;
using System.Collections.Generic;
using Core.Debug;
using Core.Events;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  RULE ENGINE
    // ════════════════════════════════════════════════════════

    internal sealed class RuleEngine
    {
        private readonly List<IGameRule> _rules = new();
        private IGameModeContext         _ctx;

        internal void Initialize(IGameModeContext ctx, IGameRule[] rules)
        {
            _ctx = ctx;
            _rules.Clear();

            if (rules == null) return;
            foreach (var r in rules)
            {
                if (r == null) continue;
                r.Initialize(ctx);
                _rules.Add(r);
                CoreLogger.LogSystemDebug("RuleEngine", $"Regla cargada: '{r.RuleID}'");
            }

            EventBus<ObjectiveInteractedEvt>.Subscribe(OnObjectiveInteracted);
            EventBus<PlayerEliminatedEvt>.Subscribe(OnPlayerEliminated);
        }

        internal void Dispose()
        {
            EventBus<ObjectiveInteractedEvt>.Unsubscribe(OnObjectiveInteracted);
            EventBus<PlayerEliminatedEvt>.Unsubscribe(OnPlayerEliminated);
            foreach (var r in _rules) r.Dispose();
            _rules.Clear();
        }

        private void OnObjectiveInteracted(ObjectiveInteractedEvt e)
        {
            foreach (var r in _rules)
                if (r.IsEnabled) r.OnObjectiveInteracted(e);
        }

        private void OnPlayerEliminated(PlayerEliminatedEvt e)
        {
            foreach (var r in _rules)
                if (r.IsEnabled) r.OnPlayerEliminated(e);
        }
    }

    // ════════════════════════════════════════════════════════
    //  WIN CONDITION EVALUATOR
    // ════════════════════════════════════════════════════════

    internal sealed class WinConditionEvaluator
    {
        private readonly List<IWinCondition> _conditions = new();
        private IGameModeContext             _ctx;

        /// <summary>Callback al detectar ganador. GameModeBase lo asigna.</summary>
        internal Action<WinResult> OnWinDetected;

        internal void Initialize(IGameModeContext ctx, IWinCondition[] conditions)
        {
            _ctx = ctx;
            _conditions.Clear();

            if (conditions == null) return;
            foreach (var c in conditions)
            {
                if (c == null) continue;
                c.Initialize(ctx);
                _conditions.Add(c);
            }

            // Evaluar SOLO cuando el score cambia, no en Update
            EventBus<ScoreChangedEvt>.Subscribe(OnScoreChanged);
            EventBus<PlayerEliminatedEvt>.Subscribe(OnPlayerEliminated);
        }

        internal void Dispose()
        {
            EventBus<ScoreChangedEvt>.Unsubscribe(OnScoreChanged);
            EventBus<PlayerEliminatedEvt>.Unsubscribe(OnPlayerEliminated);
        }

        private void OnScoreChanged(ScoreChangedEvt _)     => Evaluate();
        private void OnPlayerEliminated(PlayerEliminatedEvt _) => Evaluate();

        private void Evaluate()
        {
            // Fase Idle o RoundEnd → no evaluar
            if (_ctx.Phase != GameModePhase.Playing) return;

            foreach (var c in _conditions)
            {
                var r = c.Evaluate(_ctx);
                if (r.Won)
                {
                    CoreLogger.LogSystem("WinEvaluator",
                        $"Condición '{c.ConditionID}' → T{r.WinnerTeamID} gana ({r.Reason})");
                    OnWinDetected?.Invoke(r);
                    return;
                }
            }
        }

        /// <summary>
        /// Llamado por GameModeBase cuando el timer expira.
        /// Fuerza evaluación independientemente del score.
        /// </summary>
        internal WinResult EvaluateTimeOut()
        {
            int leader = _ctx.Score.GetLeadingTeam();
            return leader < 0 ? WinResult.Draw : WinResult.Team(leader, "TimeExpired");
        }
    }
}
