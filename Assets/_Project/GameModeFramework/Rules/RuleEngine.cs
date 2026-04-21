// ============================================================
//  RuleEngine.cs
//  GameModeFramework/Rules/RuleEngine.cs
//
//  RESPONSABILIDAD ÚNICA: Distribuir eventos a las reglas activas.
//
//  El RuleEngine NO evalúa reglas. Solo las recorre y les pasa
//  el evento. Cada IGameRule decide qué hacer con él.
//  El WinConditionEvaluator es quien decide si alguien ganó.
// ============================================================

using System.Collections.Generic;
using GameMode.Framework.Events;
using Core.Debug;
using Core.Events;

namespace GameMode.Framework.Rules
{
    public class RuleEngine
    {
        private readonly List<IGameRule>    _rules      = new();
        private          IGameModeContext   _ctx;

        public void Initialize(IGameModeContext ctx, IGameRule[] rules)
        {
            _ctx = ctx;
            _rules.Clear();

            if (rules == null) return;

            foreach (var rule in rules)
            {
                if (rule == null) continue;
                rule.Initialize(ctx);
                _rules.Add(rule);
                CoreLogger.LogSystemDebug("RuleEngine", $"Regla cargada: '{rule.RuleID}'");
            }

            // Suscribir a eventos del bus
            EventBus<ObjectiveInteractedEvent>.Subscribe(OnObjectiveInteracted);
            EventBus<PlayerEliminatedEvent>.Subscribe(OnPlayerEliminated);
        }

        public void Dispose()
        {
            EventBus<ObjectiveInteractedEvent>.Unsubscribe(OnObjectiveInteracted);
            EventBus<PlayerEliminatedEvent>.Unsubscribe(OnPlayerEliminated);

            foreach (var rule in _rules) rule.Dispose();
            _rules.Clear();
        }

        // ── Distribución de eventos ───────────────────────────

        private void OnObjectiveInteracted(ObjectiveInteractedEvent evt)
        {
            foreach (var rule in _rules)
                if (rule.IsEnabled) rule.OnEvent(evt);
        }

        private void OnPlayerEliminated(PlayerEliminatedEvent evt)
        {
            foreach (var rule in _rules)
                if (rule.IsEnabled) rule.OnEvent(evt);
        }
    }
}