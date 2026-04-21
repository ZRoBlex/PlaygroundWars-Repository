// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_ScoreSystem.cs                             ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • ScoreSystem (class, C# puro — sin MonoBehaviour)    ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Acumular y consultar puntuación por equipo/jugador.   ║
// ║    Publicar ScoreChangedEvt al cambiar score.            ║
// ║                                                          ║
// ║  QUIÉN LO POSEE:  GameModeContext (internal)             ║
// ║  QUIÉN ESCRIBE:   IGameRule via ObjectiveScoredEvt       ║
// ║                   (el ScoreSystem escucha ese evento)    ║
// ║  QUIÉN LEE:       IWinCondition via IScoreSystem         ║
// ║                                                          ║
// ║  SERVER AUTHORITY:                                       ║
// ║    AddScore() solo se llama en el servidor.              ║
// ║    El cliente recibe el score vía ScoreChangedEvt.       ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF.Config;

namespace GMF
{
    internal sealed class ScoreSystem : IScoreSystem
    {
        private readonly Dictionary<int, int> _teamScore   = new();
        private readonly Dictionary<int, int> _playerScore = new();
        private readonly ScoreConfig          _cfg;

        internal ScoreSystem(ScoreConfig cfg)
        {
            _cfg = cfg;
        }

        // ── Escritura (solo servidor) ─────────────────────────

        /// <summary>
        /// Añade puntos a un equipo.
        /// Llama a este método SOLO en el servidor/host.
        /// </summary>
        internal void AddScore(int teamID, int delta, int playerID = -1, string reason = "")
        {
            if (!_teamScore.ContainsKey(teamID)) _teamScore[teamID] = 0;
            _teamScore[teamID] += delta;

            int playerTotal = 0;
            if (_cfg.TrackIndividual && playerID >= 0)
            {
                if (!_playerScore.ContainsKey(playerID)) _playerScore[playerID] = 0;
                _playerScore[playerID] += delta;
                playerTotal = _playerScore[playerID];
            }

            CoreLogger.LogSystem("ScoreSystem",
                $"T{teamID} +{delta} = {_teamScore[teamID]} [{reason}]");

            EventBus<ScoreChangedEvt>.Raise(new ScoreChangedEvt
            {
                TeamID         = teamID,
                PlayerID       = playerID,
                Delta          = delta,
                NewTeamTotal   = _teamScore[teamID],
                NewPlayerTotal = playerTotal,
                Reason         = reason
            });
        }

        internal void ResetRound()
        {
            _teamScore.Clear();
            _playerScore.Clear();
        }

        // ── IScoreSystem (solo lectura) ───────────────────────

        public int GetTeamScore(int tid)
            => _teamScore.TryGetValue(tid, out int s) ? s : 0;

        public int GetPlayerScore(int pid)
            => _playerScore.TryGetValue(pid, out int s) ? s : 0;

        public int GetLeadingTeam()
        {
            int best = -1, bestScore = int.MinValue;
            foreach (var (id, sc) in _teamScore)
                if (sc > bestScore) { bestScore = sc; best = id; }
            return best;
        }
    }
}
