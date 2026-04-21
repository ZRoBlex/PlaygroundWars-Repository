// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_TeamSystem.cs                              ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • TeamSystem (class, C# puro — sin MonoBehaviour)     ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Asignación y consulta de equipos.                     ║
// ║    Sabe qué jugador está en qué equipo.                  ║
// ║                                                          ║
// ║  QUIÉN LO POSEE: GameModeContext (internal)              ║
// ║  QUIÉN LO USA:   IGameRule (via ITeamSystem readonly)    ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA: Ninguna. Clase única.             ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • AutoAssign con 0 jugadores → no asigna nada         ║
// ║    • Rebalance durante partida → puede confundir reglas  ║
// ║      (solo rebalancear entre rondas)                     ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using Core.Events;
using GMF.Config;

namespace GMF
{
    internal sealed class TeamSystem : ITeamSystem
    {
        // playerID → teamID
        private readonly Dictionary<int, int>       _playerTeam  = new();
        // teamID   → [playerIDs]
        private readonly Dictionary<int, List<int>> _teamPlayers = new();
        private readonly TeamConfig                 _cfg;

        internal TeamSystem(TeamConfig cfg)
        {
            _cfg = cfg;
            for (int i = 0; i < cfg.TeamCount; i++)
                _teamPlayers[i] = new List<int>();
        }

        // ── Asignación ────────────────────────────────────────

        internal void Assign(int playerID, int teamID)
        {
            // Remover del equipo anterior si existía
            int prev = GetTeam(playerID);
            if (prev >= 0 && _teamPlayers.ContainsKey(prev))
                _teamPlayers[prev].Remove(playerID);

            _playerTeam[playerID] = teamID;
            if (!_teamPlayers.ContainsKey(teamID))
                _teamPlayers[teamID] = new List<int>();
            if (!_teamPlayers[teamID].Contains(playerID))
                _teamPlayers[teamID].Add(playerID);

            string name = teamID < _cfg.TeamNames?.Length
                ? _cfg.TeamNames[teamID] : $"Team {teamID}";

            CoreLogger.LogSystem("TeamSystem",
                $"[P{playerID}] → {name}");

            EventBus<PlayerJoinedTeamEvt>.Raise(new PlayerJoinedTeamEvt
            {
                PlayerID = playerID,
                TeamID   = teamID,
                TeamName = name
            });
        }

        /// <summary>Asigna al equipo con menos jugadores.</summary>
        internal void AutoAssign(int playerID)
        {
            int best = 0, min = int.MaxValue;
            for (int i = 0; i < _cfg.TeamCount; i++)
            {
                int cnt = _teamPlayers.ContainsKey(i) ? _teamPlayers[i].Count : 0;
                if (cnt < min) { min = cnt; best = i; }
            }
            Assign(playerID, best);
        }

        /// <summary>Redistribuye todos los jugadores equitativamente.</summary>
        internal void Rebalance()
        {
            var all = new List<int>(_playerTeam.Keys);
            foreach (var list in _teamPlayers.Values) list.Clear();
            _playerTeam.Clear();
            for (int i = 0; i < all.Count; i++)
                Assign(all[i], i % _cfg.TeamCount);
        }

        internal void Remove(int playerID)
        {
            int t = GetTeam(playerID);
            if (t >= 0 && _teamPlayers.ContainsKey(t))
                _teamPlayers[t].Remove(playerID);
            _playerTeam.Remove(playerID);
        }

        // ── ITeamSystem ───────────────────────────────────────

        public int TeamCount => _cfg.TeamCount;

        public int GetTeam(int pid)
            => _playerTeam.TryGetValue(pid, out int t) ? t : -1;

        public bool AreEnemies(int a, int b)
        {
            int ta = GetTeam(a), tb = GetTeam(b);
            return ta >= 0 && tb >= 0 && ta != tb;
        }

        public bool AreTeammates(int a, int b)
        {
            int ta = GetTeam(a), tb = GetTeam(b);
            return ta >= 0 && ta == tb;
        }

        public IReadOnlyList<int> GetPlayers(int teamID)
            => _teamPlayers.TryGetValue(teamID, out var list)
               ? list.AsReadOnly()
               : (IReadOnlyList<int>)System.Array.Empty<int>();
    }
}
