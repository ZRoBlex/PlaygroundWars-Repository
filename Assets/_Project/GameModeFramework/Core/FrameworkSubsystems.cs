// ============================================================
//  FrameworkSubsystems.cs
//  GameModeFramework/Core/FrameworkSubsystems.cs
//
//  Subsistemas puros (sin MonoBehaviour) del framework.
//  GameModeContext los posee y los expone de solo lectura.
//
//  Contenido:
//  • TeamRegistry      — equipos y asignación de jugadores
//  • ScoreSystem       — puntuación por equipo e individual
//  • ObjectiveRegistry — registro de objetivos en escena
// ============================================================

using System.Collections.Generic;
using GameMode.Framework.Config;
using GameMode.Framework.Events;
using Core.Debug;
using Core.Events;

namespace GameMode.Framework
{
    // ════════════════════════════════════════════════════════
    //  TEAM REGISTRY
    // ════════════════════════════════════════════════════════

    public class TeamRegistry : IReadOnlyTeamRegistry
    {
        private readonly Dictionary<int, int>        _playerTeam   = new(); // playerID → teamID
        private readonly Dictionary<int, List<int>>  _teamPlayers  = new(); // teamID  → [playerIDs]
        private readonly TeamConfig                  _config;

        public TeamRegistry(TeamConfig config)
        {
            _config = config;
            for (int i = 0; i < config.TeamCount; i++)
                _teamPlayers[i] = new List<int>();
        }

        public int  TeamCount => _config.TeamCount;

        public void AssignPlayer(int playerID, int teamID)
        {
            int prev = GetTeamOf(playerID);
            if (prev >= 0) _teamPlayers[prev].Remove(playerID);

            _playerTeam[playerID]    = teamID;
            _teamPlayers[teamID].Add(playerID);

            EventBus<PlayerJoinedTeamEvent>.Raise(new PlayerJoinedTeamEvent
            {
                PlayerID = playerID,
                TeamID   = teamID,
                TeamName = GetTeamName(teamID)
            });
        }

        public void AutoAssign(int playerID)
        {
            // Asignar al equipo con menos jugadores
            int target = 0, min = int.MaxValue;
            for (int i = 0; i < _config.TeamCount; i++)
            {
                int count = _teamPlayers[i].Count;
                if (count < min) { min = count; target = i; }
            }
            AssignPlayer(playerID, target);
        }

        public void RemovePlayer(int playerID)
        {
            int team = GetTeamOf(playerID);
            if (team >= 0) _teamPlayers[team].Remove(playerID);
            _playerTeam.Remove(playerID);

            EventBus<PlayerLeftTeamEvent>.Raise(new PlayerLeftTeamEvent
            {
                PlayerID = playerID, TeamID = team
            });
        }

        public void Rebalance()
        {
            var all = new List<int>(_playerTeam.Keys);
            foreach (var t in _teamPlayers.Values) t.Clear();
            _playerTeam.Clear();
            for (int i = 0; i < all.Count; i++)
                AssignPlayer(all[i], i % _config.TeamCount);
        }

        // IReadOnlyTeamRegistry
        public int GetTeamOf(int pid)
            => _playerTeam.TryGetValue(pid, out int t) ? t : -1;

        public bool AreEnemies(int a, int b)
        {
            int ta = GetTeamOf(a), tb = GetTeamOf(b);
            return ta >= 0 && tb >= 0 && ta != tb;
        }

        public bool AreTeammates(int a, int b)
        {
            int ta = GetTeamOf(a), tb = GetTeamOf(b);
            return ta >= 0 && ta == tb;
        }

        public IReadOnlyList<int> GetPlayersInTeam(int teamID)
            => _teamPlayers.TryGetValue(teamID, out var l)
               ? l.AsReadOnly()
               : new List<int>().AsReadOnly();

        public int GetScore(int teamID) => 0; // Delegado a ScoreSystem

        private string GetTeamName(int id)
            => id < _config.TeamNames?.Length ? _config.TeamNames[id] : $"Team {id}";
    }

    // ════════════════════════════════════════════════════════
    //  SCORE SYSTEM
    // ════════════════════════════════════════════════════════

    public class ScoreSystem : IReadOnlyScoreSystem
    {
        private readonly Dictionary<int, int> _teamScore   = new();
        private readonly Dictionary<int, int> _playerScore = new();
        private readonly ScoreConfig          _config;

        public ScoreSystem(ScoreConfig config)
        {
            _config = config;
        }

        public void AddTeamScore(int teamID, int delta, int playerID = -1, string reason = "")
        {
            if (!_teamScore.ContainsKey(teamID)) _teamScore[teamID] = 0;
            _teamScore[teamID] += delta;

            int playerTotal = 0;
            if (_config.TrackIndividualScore && playerID >= 0)
            {
                if (!_playerScore.ContainsKey(playerID)) _playerScore[playerID] = 0;
                _playerScore[playerID] += delta;
                playerTotal = _playerScore[playerID];
            }

            CoreLogger.LogSystem("ScoreSystem",
                $"T{teamID}: +{delta} = {_teamScore[teamID]} (reason={reason})");

            EventBus<ScoreChangedEvent>.Raise(new ScoreChangedEvent
            {
                TeamID         = teamID,
                PlayerID       = playerID,
                Delta          = delta,
                NewTeamTotal   = _teamScore[teamID],
                NewPlayerTotal = playerTotal,
                Reason         = reason
            });
        }

        public void Reset()
        {
            _teamScore.Clear();
            _playerScore.Clear();
        }

        // IReadOnlyScoreSystem
        public int GetTeamScore(int teamID)
            => _teamScore.TryGetValue(teamID, out int s) ? s : 0;

        public int GetPlayerScore(int pid)
            => _playerScore.TryGetValue(pid, out int s) ? s : 0;

        public int GetLeadingTeam()
        {
            int best = -1, bestScore = int.MinValue;
            foreach (var (id, score) in _teamScore)
                if (score > bestScore) { bestScore = score; best = id; }
            return best;
        }
    }

    // ════════════════════════════════════════════════════════
    //  OBJECTIVE REGISTRY
    // ════════════════════════════════════════════════════════

    public class ObjectiveRegistry : IReadOnlyObjectiveRegistry
    {
        private readonly Dictionary<string, IObjective> _objectives = new();

        public void Register(IObjective objective)
        {
            if (objective == null || string.IsNullOrEmpty(objective.ObjectiveID)) return;
            _objectives[objective.ObjectiveID] = objective;
            CoreLogger.LogSystemDebug("ObjectiveRegistry",
                $"Objetivo registrado: '{objective.ObjectiveID}'");
        }

        public void UnRegister(string id) => _objectives.Remove(id);

        public void ResetAll()
        {
            foreach (var obj in _objectives.Values) obj.Reset();
        }

        // IReadOnlyObjectiveRegistry
        public IObjective GetObjective(string id)
            => _objectives.TryGetValue(id, out var o) ? o : null;

        public IReadOnlyList<IObjective> GetAll()
            => new List<IObjective>(_objectives.Values).AsReadOnly();

        public IReadOnlyList<IObjective> GetByTeam(int teamID)
        {
            var result = new List<IObjective>();
            foreach (var o in _objectives.Values)
                if (o.OwnerTeamID == teamID) result.Add(o);
            return result.AsReadOnly();
        }
    }
}
