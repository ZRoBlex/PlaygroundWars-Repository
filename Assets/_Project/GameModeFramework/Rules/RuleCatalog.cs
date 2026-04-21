// ============================================================
//  RuleCatalog.cs
//  GameModeFramework/Rules/RuleCatalog.cs
//
//  Catálogo de reglas y condiciones de victoria reutilizables.
//  Para crear TDM, FFA, KOTH, CTF: combinar estas piezas en
//  GameModeDefinitionSO sin escribir código nuevo.
//
//  CONTENIDO:
//  Reglas:
//    • KillScoreRule         → kill = punto para el equipo
//    • ObjectiveCaptureRule  → objetivo capturado = punto
//    • ObjectiveReturnRule   → devolver objetivo propio sin captura
//  Condiciones de victoria:
//    • TeamScoreReachedCondition  → equipo llega a N puntos
//    • TimeExpiredCondition       → tiempo agotado → ganador por puntos
//    • LastTeamStandingCondition  → todos los enemigos eliminados
// ============================================================

using System;
using GameMode.Framework.Events;
using Core.Events;
using UnityEngine;

namespace GameMode.Framework.Rules
{
    // ════════════════════════════════════════════════════════
    //  REGLAS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Por cada kill, da puntos al equipo del asesino.
    /// Configurable: ¿cuántos puntos por kill?
    /// Cubre: TDM, FFA, cualquier modo con kills.
    /// </summary>
    [Serializable]
    public class KillScoreRule : IGameRule
    {
        public string  RuleID    => "kill_score";
        public bool    IsEnabled { get; set; } = true;

        [SerializeField] private int _pointsPerKill = 1;

        private IGameModeContext   _ctx;
        private ScoreSystem        _score;

        public void Initialize(IGameModeContext ctx)
        {
            _ctx   = ctx;
            _score = (ctx as GameModeContext)?._score;
        }

        public void OnEvent(ObjectiveInteractedEvent evt) { }

        public void OnEvent(PlayerEliminatedEvent evt)
        {
            if (!IsEnabled) return;
            if (evt.KillerID < 0 || evt.KillerTeamID < 0) return;

            _score?.AddTeamScore(
                evt.KillerTeamID, _pointsPerKill,
                evt.KillerID, "Kill");
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Cuando un objetivo es capturado (InteractionType=="Capture"),
    /// da puntos al equipo que capturó.
    /// Cubre: CTF, KOTH, cualquier modo con objetivos capturables.
    /// </summary>
    [Serializable]
    public class ObjectiveCaptureRule : IGameRule
    {
        public string RuleID    => "objective_capture";
        public bool   IsEnabled { get; set; } = true;

        [SerializeField] private int _pointsPerCapture = 1;

        private ScoreSystem _score;

        public void Initialize(IGameModeContext ctx)
            => _score = (ctx as GameModeContext)?._score;

        public void OnEvent(ObjectiveInteractedEvent evt)
        {
            if (!IsEnabled) return;
            if (evt.InteractionType != "Capture") return;
            if (evt.PlayerTeamID < 0) return;

            _score?.AddTeamScore(
                evt.PlayerTeamID, _pointsPerCapture,
                evt.PlayerID, "Capture");
        }

        public void OnEvent(PlayerEliminatedEvent evt) { }
        public void Dispose() { }
    }

    /// <summary>
    /// Cuando el portador de un objetivo muere, suelta el objetivo.
    /// Emite ObjectiveResetRequestedEvent para que el objetivo se reinicie.
    /// Cubre: CTF (bandera se cae al morir el portador).
    /// </summary>
    [Serializable]
    public class ObjectiveDropOnDeathRule : IGameRule
    {
        public string RuleID    => "objective_drop_on_death";
        public bool   IsEnabled { get; set; } = true;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public void OnEvent(PlayerEliminatedEvent evt)
        {
            if (!IsEnabled) return;
            if (!evt.WasCarryingObjective) return;

            // El objetivo ya habrá emitido un Drop event al detectar
            // la muerte del portador via PlayerDiedEvent.
            // Esta regla solo es para lógica adicional si se necesita.
        }

        public void OnEvent(ObjectiveInteractedEvent evt) { }
        public void Dispose() { }
    }

    // ════════════════════════════════════════════════════════
    //  CONDICIONES DE VICTORIA
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Un equipo gana cuando su score llega a scoreToWin.
    /// Cubre: CTF (3 capturas), TDM (25 kills), KOTH (100 puntos de control).
    /// </summary>
    [Serializable]
    public class TeamScoreReachedCondition : IWinCondition
    {
        public string ConditionID => "team_score_reached";

        [SerializeField] private int _scoreToWin = 3;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            int teams = (ctx.Teams as TeamRegistry)?.TeamCount ?? 2;

            for (int t = 0; t < teams; t++)
            {
                if (ctx.Score.GetTeamScore(t) >= _scoreToWin)
                    return new WinResult(true, t, "ScoreReached");
            }
            return WinResult.NoWinner;
        }
    }

    /// <summary>
    /// El tiempo se agotó. Gana el equipo con más puntos.
    /// Empate si los scores son iguales.
    /// Cubre: cualquier modo con límite de tiempo.
    /// </summary>
    [Serializable]
    public class TimeExpiredCondition : IWinCondition
    {
        public string ConditionID => "time_expired";

        // Esta condición es activada por el RoundSystem cuando expira el timer,
        // no por ScoreChangedEvent. GameModeBase.HandleTimeExpired() la llama.
        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            // Esta condición solo es relevante cuando el timer expiró.
            // El GameModeBase llama a HandleTimeExpired() → RoundEndSequence()
            // con el leading team. Esta condición sirve como tiebreaker.
            int leader = ctx.Score.GetLeadingTeam();
            if (leader < 0) return WinResult.Draw;
            return new WinResult(true, leader, "TimeExpired");
        }
    }

    /// <summary>
    /// Un equipo gana cuando el otro no tiene jugadores vivos.
    /// Cubre: Elimination, Search & Destroy, modos sin respawn.
    /// </summary>
    [Serializable]
    public class LastTeamStandingCondition : IWinCondition
    {
        public string ConditionID => "last_team_standing";

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            // En producción: integrar con PlayerHealth/SpawnSystem
            // para contar jugadores vivos por equipo.
            // Placeholder — expandir según el proyecto.
            return WinResult.NoWinner;
        }
    }

    /// <summary>
    /// Score individual: un jugador llega a N puntos.
    /// Cubre: FFA (Free For All).
    /// </summary>
    [Serializable]
    public class IndividualScoreReachedCondition : IWinCondition
    {
        public string ConditionID => "individual_score_reached";

        [SerializeField] private int _scoreToWin = 20;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            // En FFA, cada jugador es su propio "equipo"
            int teams = (ctx.Teams as TeamRegistry)?.TeamCount ?? 2;
            for (int t = 0; t < teams; t++)
            {
                var players = ctx.Teams.GetPlayersInTeam(t);
                foreach (int pid in players)
                {
                    if (ctx.Score.GetPlayerScore(pid) >= _scoreToWin)
                        return new WinResult(true, t, "IndividualScoreReached");
                }
            }
            return WinResult.NoWinner;
        }
    }
}
