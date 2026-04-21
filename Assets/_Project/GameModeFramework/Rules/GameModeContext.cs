// ============================================================
//  GameModeContext.cs
//  GameModeFramework/Core/GameModeContext.cs
//
//  Estado mutable (interno) + vista de solo lectura (pública).
//  Solo GameModeBase puede mutar el estado.
//  Todos los demás sistemas reciben IGameModeContext (solo lectura).
// ============================================================

namespace GameMode.Framework
{
    using GameMode.Framework.Config;

    public class GameModeContext : IGameModeContext
    {
        // ── Solo lectura pública (IGameModeContext) ───────────

        public string          ModeID       { get; private set; }
        public GameModePhase   Phase        { get; private set; }
        public int             CurrentRound { get; private set; }
        public float           ElapsedTime  { get; private set; }

        public IReadOnlyTeamRegistry      Teams      => _teams;
        public IReadOnlyScoreSystem       Score      => _score;
        public IReadOnlyObjectiveRegistry Objectives => _objectives;

        // ── Acceso interno mutable (solo GameModeBase) ────────

        internal TeamRegistry      _teams;
        internal ScoreSystem       _score;
        internal ObjectiveRegistry _objectives;

        internal void Init(string modeID, TeamConfig tc, ScoreConfig sc)
        {
            ModeID       = modeID;
            Phase        = GameModePhase.Idle;
            CurrentRound = 1;
            ElapsedTime  = 0f;
            _teams       = new TeamRegistry(tc);
            _score       = new ScoreSystem(sc);
            _objectives  = new ObjectiveRegistry();
        }

        internal void SetPhase(GameModePhase phase) => Phase = phase;
        internal void SetRound(int round)            => CurrentRound = round;
        internal void Tick(float dt)                 => ElapsedTime  += dt;
        internal void ResetScore()                   => _score.Reset();
    }
}
