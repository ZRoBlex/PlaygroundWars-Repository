// ============================================================
//  WinConditionEvaluator.cs
//  GameModeFramework/Rules/WinConditionEvaluator.cs
//
//  RESPONSABILIDAD ÚNICA: Evaluar condiciones de victoria.
//  Solo se activa cuando el estado del juego cambia (score, ronda).
//  NUNCA en Update.
// ============================================================

namespace GameMode.Framework.Rules
{
    using System;
    using System.Collections.Generic;
    using GameMode.Framework.Events;
    using Core.Events;

    public class WinConditionEvaluator
    {
        private readonly List<IWinCondition> _conditions = new();
        private          IGameModeContext    _ctx;

        // Callback para notificar al GameModeBase cuando alguien gana
        public Action<WinResult> OnWinDetected;

        public void Initialize(IGameModeContext ctx, IWinCondition[] conditions)
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

            // Solo evaluar cuando el estado podría haber cambiado
            EventBus<ScoreChangedEvent>.Subscribe(OnStateChanged);
            EventBus<RoundEndedEvent>.Subscribe(OnRoundEnded);
            EventBus<PlayerEliminatedEvent>.Subscribe(OnPlayerEliminated);
        }

        public void Dispose()
        {
            EventBus<ScoreChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<RoundEndedEvent>.Unsubscribe(OnRoundEnded);
            EventBus<PlayerEliminatedEvent>.Unsubscribe(OnPlayerEliminated);
        }

        private void OnStateChanged(ScoreChangedEvent _)     => Evaluate();
        private void OnRoundEnded(RoundEndedEvent _)         => Evaluate();
        private void OnPlayerEliminated(PlayerEliminatedEvent _) => Evaluate();

        private void Evaluate()
        {
            foreach (var condition in _conditions)
            {
                var result = condition.Evaluate(_ctx);
                if (result.Won)
                {
                    OnWinDetected?.Invoke(result);
                    return;
                }
            }
        }
    }
}
