// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_WinConditions.cs                           ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • TeamScoreWin    (IWinCondition)                     ║
// ║    • PlayerScoreWin  (IWinCondition) ← para FFA          ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Catálogo de condiciones de victoria reutilizables.    ║
// ║    Evaluadas SOLO cuando ScoreChangedEvt ocurre.         ║
// ║    NUNCA en Update().                                     ║
// ║                                                          ║
// ║  EXTENSIÓN:                                              ║
// ║    Para una condición nueva: implementar IWinCondition   ║
// ║    en archivo separado. No modificar este archivo.       ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  TEAM SCORE WIN
    //  Uso: CTF (3 capturas), TDM (25 kills), KOTH (100 puntos)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Un equipo gana cuando su score llega a _scoreToWin.
    /// CONFIGURA: _scoreToWin según el modo.
    /// </summary>
    [Serializable]
    public class TeamScoreWin : IWinCondition
    {
        public string ConditionID => "team_score_win";

        [SerializeField, Range(1, 200)]
        private int _scoreToWin = 3;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            int teams = ctx.Teams.TeamCount;
            for (int t = 0; t < teams; t++)
                if (ctx.Score.GetTeamScore(t) >= _scoreToWin)
                    return WinResult.Team(t, "ScoreReached");

            return WinResult.NoWinner;
        }
    }

    // ════════════════════════════════════════════════════════
    //  PLAYER SCORE WIN
    //  Uso: FFA (cada jugador es su propio equipo)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Un jugador gana cuando su score individual llega a _scoreToWin.
    /// Pensado para FFA donde TeamCount = N y MaxPerTeam = 1.
    /// </summary>
    [Serializable]
    public class PlayerScoreWin : IWinCondition
    {
        public string ConditionID => "player_score_win";

        [SerializeField, Range(1, 200)]
        private int _scoreToWin = 20;

        private IGameModeContext _ctx;

        public void Initialize(IGameModeContext ctx) => _ctx = ctx;

        public WinResult Evaluate(IGameModeContext ctx)
        {
            int teams = ctx.Teams.TeamCount;
            for (int t = 0; t < teams; t++)
            {
                var players = ctx.Teams.GetPlayers(t);
                foreach (int pid in players)
                    if (ctx.Score.GetPlayerScore(pid) >= _scoreToWin)
                        return WinResult.Team(t, "IndividualScoreReached");
            }
            return WinResult.NoWinner;
        }
    }
}
