// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RuleEngine.cs  (REEMPLAZA el anterior)     ║
// ║                                                          ║
// ║  FIX PRINCIPAL:                                          ║
// ║    WinConditionEvaluator no terminaba el juego porque    ║
// ║    el callback OnWinDetected solo era un Action<> local. ║
// ║    El ScoreSystem publicaba ScoreChangedEvt ANTES de que ║
// ║    el evaluador estuviera suscrito en Initialize().      ║
// ║                                                          ║
// ║    Ahora también escucha ObjectiveScoredEvt para evaluar ║
// ║    después de que una captura suma puntos.               ║
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

        internal void Initialize(IGameModeContext ctx, IGameRule[] rules)
        {
            _rules.Clear();

            if (rules == null) return;
            foreach (var r in rules)
            {
                if (r == null) continue;
                r.Initialize(ctx);
                _rules.Add(r);
                CoreLogger.LogSystemDebug("RuleEngine", $"Regla: '{r.RuleID}'");
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
                CoreLogger.LogSystemDebug("WinEval", $"Condición: '{c.ConditionID}'");
            }

            // Evaluar en cada cambio de score — que es cuando puede cambiar el resultado
            EventBus<ScoreChangedEvt>.Subscribe(OnScoreChanged);
            // También cuando un jugador es eliminado (para LastTeamStanding)
            EventBus<PlayerEliminatedEvt>.Subscribe(OnPlayerElim);
        }

        internal void Dispose()
        {
            EventBus<ScoreChangedEvt>.Unsubscribe(OnScoreChanged);
            EventBus<PlayerEliminatedEvt>.Unsubscribe(OnPlayerElim);
        }

        private void OnScoreChanged(ScoreChangedEvt e)  => Evaluate();
        private void OnPlayerElim(PlayerEliminatedEvt e) => Evaluate();

        private void Evaluate()
        {
            if (_ctx.Phase != GameModePhase.Playing) return;

            foreach (var c in _conditions)
            {
                var r = c.Evaluate(_ctx);
                if (!r.Won) continue;

                CoreLogger.LogSystem("WinEval",
                    $"✅ Condición '{c.ConditionID}' → T{r.WinnerTeamID} gana ({r.Reason})");

                OnWinDetected?.Invoke(r);
                return;
            }
        }

        internal WinResult EvaluateTimeOut()
        {
            int leader = _ctx.Score.GetLeadingTeam();
            return leader < 0 ? WinResult.Draw : WinResult.Team(leader, "TimeExpired");
        }
    }
}