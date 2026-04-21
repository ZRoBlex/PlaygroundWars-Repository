// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_WinConditions.cs  (REEMPLAZA el anterior)  ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • TeamScoreWin   (WinConditionBaseSO)                 ║
// ║    • PlayerScoreWin (WinConditionBaseSO)                 ║
// ║                                                          ║
// ║  ⚠️ SEPARAR: cada clase en su propio archivo             ║
// ║    GMF_TeamScoreWin.cs   y   GMF_PlayerScoreWin.cs       ║
// ║    Misma razón que las reglas: [CreateAssetMenu] es más  ║
// ║    claro con archivos separados.                         ║
// ║                                                          ║
// ║  CÓMO USAR:                                              ║
// ║    Assets → Create → GameMode Framework → Win Conditions ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  TEAM SCORE WIN
    //  Uso: CTF (3 capturas), TDM (25 kills), KOTH (100 puntos)
    // ════════════════════════════════════════════════════════

    [CreateAssetMenu(
        fileName = "TeamScoreWin",
        menuName = "GameMode Framework/Win Conditions/Team Score Win")]
    public class TeamScoreWin : WinConditionBaseSO
    {
        public override string ConditionID => "team_score_win";

        [SerializeField, Range(1, 200)]
        private int _scoreToWin = 3;

        public override WinResult Evaluate(IGameModeContext ctx)
        {
            for (int t = 0; t < ctx.Teams.TeamCount; t++)
                if (ctx.Score.GetTeamScore(t) >= _scoreToWin)
                    return WinResult.Team(t, "ScoreReached");

            return WinResult.NoWinner;
        }
    }

    // ════════════════════════════════════════════════════════
    //  PLAYER SCORE WIN
    //  Uso: FFA (TeamCount = N, MaxPerTeam = 1)
    // ════════════════════════════════════════════════════════

    // [CreateAssetMenu(
    //     fileName = "PlayerScoreWin",
    //     menuName = "GameMode Framework/Win Conditions/Player Score Win")]
    // public class PlayerScoreWin : WinConditionBaseSO
    // {
    //     public override string ConditionID => "player_score_win";

    //     [SerializeField, Range(1, 200)]
    //     private int _scoreToWin = 20;

    //     public override WinResult Evaluate(IGameModeContext ctx)
    //     {
    //         for (int t = 0; t < ctx.Teams.TeamCount; t++)
    //         {
    //             var players = ctx.Teams.GetPlayers(t);
    //             foreach (int pid in players)
    //                 if (ctx.Score.GetPlayerScore(pid) >= _scoreToWin)
    //                     return WinResult.Team(t, "IndividualScoreReached");
    //         }
    //         return WinResult.NoWinner;
    //     }
    // }
}
