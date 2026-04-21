// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Interfaces.cs  (REEMPLAZA el anterior)     ║
// ║                                                          ║
// ║  CAMBIOS EN ESTA VERSIÓN:                                ║
// ║    + RuleBaseSO (abstract ScriptableObject)              ║
// ║    + WinConditionBaseSO (abstract ScriptableObject)      ║
// ║                                                          ║
// ║  POR QUÉ:                                                ║
// ║    [SerializeReference] con interfaces no muestra picker ║
// ║    en Unity Inspector. Solo muestra "Element 0".         ║
// ║    La solución es que las implementaciones sean SOs.     ║
// ║    El Inspector de SOs sí permite drag & drop.           ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using GMF.Config;
using UnityEngine;

namespace GMF
{
    public enum GameModePhase { Idle, WarmUp, Playing, RoundEnd, PostGame }

    public readonly struct WinResult
    {
        public bool   Won          { get; }
        public int    WinnerTeamID { get; }
        public string Reason       { get; }

        private WinResult(bool won, int teamID, string reason)
        {
            Won = won; WinnerTeamID = teamID; Reason = reason;
        }

        public static WinResult NoWinner => new(false, -1, string.Empty);
        public static WinResult Draw     => new(true,  -1, "Draw");
        public static WinResult Team(int teamID, string reason) => new(true, teamID, reason);
    }

    // ── Contratos ─────────────────────────────────────────────

    public interface IGameRule
    {
        string RuleID    { get; }
        bool   IsEnabled { get; set; }
        void   Initialize(IGameModeContext ctx);
        void   OnObjectiveInteracted(ObjectiveInteractedEvt evt);
        void   OnPlayerEliminated(PlayerEliminatedEvt evt);
        void   Dispose();
    }

    public interface IWinCondition
    {
        string    ConditionID { get; }
        WinResult Evaluate(IGameModeContext ctx);
        void      Initialize(IGameModeContext ctx);
    }

    public interface IObjective
    {
        string ObjectiveID { get; }
        int    TeamID      { get; }
        bool   IsActive    { get; }
        string State       { get; }
        void   Initialize(ObjectiveConfig cfg);
        void   Reset();
        void   SetActive(bool active);
    }

    public interface IGameModeContext
    {
        string        ModeID       { get; }
        GameModePhase Phase        { get; }
        int           CurrentRound { get; }
        float         ElapsedTime  { get; }
        ITeamSystem      Teams      { get; }
        IScoreSystem     Score      { get; }
        IObjectiveRegistry Objectives { get; }
    }

    public interface ITeamSystem
    {
        int TeamCount { get; }
        int GetTeam(int playerID);
        bool AreEnemies(int playerA, int playerB);
        bool AreTeammates(int playerA, int playerB);
        IReadOnlyList<int> GetPlayers(int teamID);
    }

    public interface IScoreSystem
    {
        int GetTeamScore(int teamID);
        int GetPlayerScore(int playerID);
        int GetLeadingTeam();
    }

    public interface IObjectiveRegistry
    {
        IObjective Get(string id);
        IReadOnlyList<IObjective> GetAll();
        IReadOnlyList<IObjective> GetByTeam(int teamID);
    }

    // ════════════════════════════════════════════════════════
    //  BASES ABSTRACTAS COMO SCRIPTABLEOBJECT
    //  ► Las implementaciones concretas heredan de estas.
    //  ► Así pueden ser creadas como assets y asignadas en
    //    Inspector con drag & drop, sin [SerializeReference].
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Base abstracta para todas las reglas del framework.
    /// Para crear una regla: heredar de RuleBaseSO e implementar
    /// los métodos abstractos. Añadir [CreateAssetMenu] en la subclase.
    /// </summary>
    public abstract class RuleBaseSO : ScriptableObject, IGameRule
    {
        public abstract string RuleID    { get; }
        public          bool   IsEnabled { get; set; } = true;

        public abstract void Initialize(IGameModeContext ctx);
        public abstract void OnObjectiveInteracted(ObjectiveInteractedEvt evt);
        public abstract void OnPlayerEliminated(PlayerEliminatedEvt evt);
        public virtual  void Dispose() { }
    }

    /// <summary>
    /// Base abstracta para todas las condiciones de victoria.
    /// Para crear una condición: heredar de WinConditionBaseSO.
    /// Añadir [CreateAssetMenu] en la subclase.
    /// </summary>
    public abstract class WinConditionBaseSO : ScriptableObject, IWinCondition
    {
        public abstract string    ConditionID { get; }
        public abstract WinResult Evaluate(IGameModeContext ctx);
        public virtual  void      Initialize(IGameModeContext ctx) { }
    }
}
