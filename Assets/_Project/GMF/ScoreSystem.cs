// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_ScoreSystem.cs  (REEMPLAZA el anterior)    ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + Tracking de kills por equipo y por jugador          ║
// ║      (para tiebreaker por eliminaciones)                 ║
// ║    + GetTeamKills() / GetPlayerKills()                   ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF.Config;

namespace GMF
{
    internal sealed class ScoreSystem : IScoreSystem
    {
        private readonly Dictionary<int, int> _teamScore  = new();
        private readonly Dictionary<int, int> _teamKills  = new();  // kills por equipo
        private readonly Dictionary<int, int> _playerScore = new();
        private readonly Dictionary<int, int> _playerKills = new();  // kills por jugador
        private readonly ScoreConfig          _cfg;

        internal ScoreSystem(ScoreConfig cfg) => _cfg = cfg;

        // ── Puntos ────────────────────────────────────────────

        internal void AddScore(int teamID, int delta, int playerID = -1, string reason = "")
        {
            Increment(_teamScore, teamID, delta);

            int playerTotal = 0;
            if (_cfg.TrackIndividual && playerID >= 0)
            {
                Increment(_playerScore, playerID, delta);
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

        // ── Kills ─────────────────────────────────────────────

        internal void AddKill(int killerTeamID, int killerPlayerID)
        {
            if (!_cfg.TrackKills) return;

            Increment(_teamKills,   killerTeamID,   1);
            Increment(_playerKills, killerPlayerID, 1);
        }

        // ── Reset ─────────────────────────────────────────────

        internal void ResetRound()
        {
            _teamScore.Clear();
            _playerScore.Clear();
            _teamKills.Clear();
            _playerKills.Clear();
        }

        // ── IScoreSystem ──────────────────────────────────────

        public int GetTeamScore(int tid)
            => _teamScore.TryGetValue(tid,  out int s) ? s : 0;

        public int GetPlayerScore(int pid)
            => _playerScore.TryGetValue(pid, out int s) ? s : 0;

        public int GetLeadingTeam()
        {
            int best = -1, bestScore = int.MinValue;
            foreach (var (id, sc) in _teamScore)
                if (sc > bestScore) { bestScore = sc; best = id; } 
            return best;
        }

        public int GetTeamKills(int teamID)
            => _teamKills.TryGetValue(teamID,   out int k) ? k : 0;

        public int GetPlayerKills(int playerID)
            => _playerKills.TryGetValue(playerID, out int k) ? k : 0;

        /// <summary>Devuelve el teamID con más kills. -1 si empate.</summary>
        public int GetLeadingTeamByKills(int teamCount)
        {
            int best = -1, bestK = -1;
            bool tie = false;

            for (int t = 0; t < teamCount; t++)
            {
                int k = GetTeamKills(t);
                if (k > bestK) { bestK = k; best = t; tie = false; }
                else if (k == bestK && bestK >= 0) { tie = true; }
            }
            return tie ? -1 : best;
        }

        // ── Helper ────────────────────────────────────────────

        private static void Increment(Dictionary<int, int> d, int key, int delta)
        {
            d.TryGetValue(key, out int cur);
            d[key] = cur + delta;
        }
    }
}