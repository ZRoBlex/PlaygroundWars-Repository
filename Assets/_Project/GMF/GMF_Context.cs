// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Context.cs                                 ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • GameModeContext (class, INTERNAL — solo GameModeBase)║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Estado mutable del modo de juego.                     ║
// ║    Solo GameModeBase puede escribir en él.               ║
// ║    Todo lo demás recibe IGameModeContext (solo lectura). ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA:                                   ║
// ║    GameModeContext → queda en este archivo               ║
// ║    TeamSystem, ScoreSystem, ObjectiveRegistry →          ║
// ║    están en sus propios archivos                         ║
// ╚══════════════════════════════════════════════════════════╝

using GMF.Config;

namespace GMF
{
    /// <summary>
    /// Estado mutable del modo de juego.
    ///
    /// ACCESO:
    ///   GameModeBase posee la instancia GameModeContext (mutable).
    ///   Todos los demás sistemas reciben IGameModeContext (solo lectura).
    ///
    /// PATRÓN:
    ///   GameModeContext implementa IGameModeContext.
    ///   Los subsistemas (TeamSystem, ScoreSystem) son internos aquí
    ///   y se exponen como interfaces de solo lectura.
    /// </summary>
    internal sealed class GameModeContext : IGameModeContext
    {
        // ── IGameModeContext (solo lectura pública) ───────────

        public string        ModeID        { get; private set; }
        public GameModePhase Phase         { get; private set; }
        public int           CurrentRound  { get; private set; }
        public float         ElapsedTime   { get; private set; }

        public ITeamSystem       Teams      => _teams;
        public IScoreSystem      Score      => _score;
        public IObjectiveRegistry Objectives => _objectives;

        // ── Instancias mutables (acceso interno) ──────────────

        internal TeamSystem      _teams;
        internal ScoreSystem     _score;
        internal ObjectiveRegistry _objectives;

        // ── Construcción ──────────────────────────────────────

        internal void Build(string modeID, TeamConfig tc, ScoreConfig sc)
        {
            ModeID       = modeID;
            Phase        = GameModePhase.Idle;
            CurrentRound = 1;
            ElapsedTime  = 0f;
            _teams       = new TeamSystem(tc);
            _score       = new ScoreSystem(sc);
            _objectives  = new ObjectiveRegistry();
        }

        // ── Mutadores internos (solo GameModeBase los llama) ──

        internal void SetPhase(GameModePhase p)  => Phase        = p;
        internal void SetRound(int r)            => CurrentRound = r;
        internal void Tick(float dt)             => ElapsedTime += dt;
        internal void ResetRoundScore()          => _score.ResetRound();
    }
}
