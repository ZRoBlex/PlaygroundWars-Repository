// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_ObjectiveRegistry.cs                       ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • ObjectiveRegistry (class, C# puro)                  ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Registro y acceso O(1) a todos los IObjective         ║
// ║    activos en la escena.                                 ║
// ║                                                          ║
// ║  QUIÉN LO POSEE: GameModeContext (internal)              ║
// ║  QUIÉN REGISTRA: ObjectiveBase.Start() → auto-registro   ║
// ║  QUIÉN CONSULTA: IGameRule (via IObjectiveRegistry)      ║
// ║                                                          ║
// ║  NOTA SOBRE FindObjectOfType:                            ║
// ║    ObjectiveBase.Start() busca el GameModeBase en escena ║
// ║    UNA SOLA VEZ al inicializar.  No en Update.           ║
// ║    Es aceptable porque Init ocurre una vez por sesión.   ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;

namespace GMF
{
    internal sealed class ObjectiveRegistry : IObjectiveRegistry
    {
        private readonly Dictionary<string, IObjective> _map = new();

        internal void Register(IObjective obj)
        {
            if (obj == null || string.IsNullOrEmpty(obj.ObjectiveID)) return;
            _map[obj.ObjectiveID] = obj;
            CoreLogger.LogSystemDebug("ObjectiveRegistry",
                $"Registrado: '{obj.ObjectiveID}' (T{obj.TeamID})");
        }

        internal void Unregister(string id)
        {
            _map.Remove(id);
        }

        internal void ResetAll()
        {
            foreach (var obj in _map.Values) obj.Reset();
        }

        // ── IObjectiveRegistry (solo lectura) ────────────────

        public IObjective Get(string id)
            => _map.TryGetValue(id, out var o) ? o : null;

        public IReadOnlyList<IObjective> GetAll()
            => new List<IObjective>(_map.Values).AsReadOnly();

        public IReadOnlyList<IObjective> GetByTeam(int teamID)
        {
            var r = new List<IObjective>();
            foreach (var o in _map.Values)
                if (o.TeamID == teamID) r.Add(o);
            return r.AsReadOnly();
        }
    }
}
