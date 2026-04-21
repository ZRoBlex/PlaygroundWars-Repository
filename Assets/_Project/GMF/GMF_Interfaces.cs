// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Interfaces.cs                              ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • IGameRule         (interface)                       ║
// ║    • IWinCondition     (interface)                       ║
// ║    • IObjective        (interface)                       ║
// ║    • IGameModeContext  (interface — solo lectura)        ║
// ║    • ITeamSystem       (interface — solo lectura)        ║
// ║    • IScoreSystem      (interface — solo lectura)        ║
// ║    • IObjectiveRegistry (interface — solo lectura)       ║
// ║    • WinResult         (struct)                          ║
// ║    • GameModePhase     (enum)                            ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Definir los contratos del framework.                  ║
// ║    Para añadir un nuevo modo: implementar estas          ║
// ║    interfaces. NUNCA modificar las interfaces existentes ║
// ║    si ya hay implementaciones (Open/Closed Principle).   ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA: Ninguna. Las interfaces son       ║
// ║    contratos, no implementaciones. Un archivo es         ║
// ║    correcto y evita dependencias circulares.             ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using GMF.Config;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  ENUMS Y STRUCTS COMPARTIDOS
    // ════════════════════════════════════════════════════════

    public enum GameModePhase { Idle, WarmUp, Playing, RoundEnd, PostGame }

    /// <summary>
    /// Resultado de evaluar una condición de victoria.
    /// Uso: var r = condition.Evaluate(ctx); if (r.Won) EndGame(r.WinnerTeamID);
    /// </summary>
    public readonly struct WinResult
    {
        public bool   Won          { get; }
        public int    WinnerTeamID { get; }  // -1 = empate
        public string Reason       { get; }

        private WinResult(bool won, int teamID, string reason)
        {
            Won = won; WinnerTeamID = teamID; Reason = reason;
        }

        public static WinResult NoWinner => new(false, -1, string.Empty);
        public static WinResult Draw     => new(true,  -1, "Draw");
        public static WinResult Team(int teamID, string reason)
            => new(true, teamID, reason);
    }

    // ════════════════════════════════════════════════════════
    //  CONTRATO: IGameRule
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Una regla reacciona a eventos del juego y puede emitir nuevos eventos.
    ///
    /// RESPONSABILIDAD: Interpretar interacciones, NO decidir quién ganó.
    /// RESTRICCIÓN: No aplica daño ni mueve objetos directamente.
    ///              Solo emite eventos (ObjectiveScoredEvt, ObjectiveResetEvt).
    ///
    /// IMPLEMENTACIONES EN: GMF_RuleCatalog.cs, GMF_CTFRules.cs
    /// </summary>
    public interface IGameRule
    {
        string RuleID    { get; }
        bool   IsEnabled { get; set; }

        /// <summary>Inyectar contexto de solo lectura al inicializar el modo.</summary>
        void Initialize(IGameModeContext ctx);

        /// <summary>Reaccionar a interacción con un objetivo.</summary>
        void OnObjectiveInteracted(ObjectiveInteractedEvt evt);

        /// <summary>Reaccionar a eliminación de jugador.</summary>
        void OnPlayerEliminated(PlayerEliminatedEvt evt);

        /// <summary>Limpiar suscripciones al desactivar el modo.</summary>
        void Dispose();
    }

    // ════════════════════════════════════════════════════════
    //  CONTRATO: IWinCondition
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Evalúa el estado del juego y determina si alguien ganó.
    ///
    /// CUÁNDO SE EVALÚA: Solo cuando ScoreChangedEvt ocurre.
    ///                   NUNCA en Update().
    ///
    /// IMPLEMENTACIONES EN: GMF_WinConditions.cs
    /// </summary>
    public interface IWinCondition
    {
        string    ConditionID { get; }
        WinResult Evaluate(IGameModeContext ctx);
        void      Initialize(IGameModeContext ctx);
    }

    // ════════════════════════════════════════════════════════
    //  CONTRATO: IObjective
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Una entidad de escena que puede ser interactuada.
    ///
    /// RESPONSABILIDAD: Detectar interacciones físicas, emitir ObjectiveInteractedEvt.
    /// RESTRICCIÓN: No conoce las reglas. No sabe qué significa ser capturado.
    ///
    /// IMPLEMENTACIONES EN: GMF_Flag.cs, GMF_CaptureZone.cs, GMF_ControlPoint.cs
    /// </summary>
    public interface IObjective
    {
        string ObjectiveID  { get; }
        int    TeamID       { get; }     // equipo dueño; -1 = neutral
        bool   IsActive     { get; }
        string State        { get; }     // "Idle","Carried","Dropped","Contested"

        void   Initialize(ObjectiveConfig cfg);
        void   Reset();
        void   SetActive(bool active);
    }

    // ════════════════════════════════════════════════════════
    //  CONTRATO: IGameModeContext (solo lectura)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Vista de solo lectura del estado del juego.
    /// Las reglas y condiciones de victoria solo leen de aquí.
    /// Solo GameModeBase puede mutar el estado real.
    /// </summary>
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

    // ════════════════════════════════════════════════════════
    //  CONTRATOS: subsistemas de solo lectura
    // ════════════════════════════════════════════════════════

    public interface ITeamSystem
    {
        int    TeamCount                         { get; }
        int    GetTeam(int playerID);
        bool   AreEnemies(int playerA, int playerB);
        bool   AreTeammates(int playerA, int playerB);
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
        IObjective            Get(string id);
        IReadOnlyList<IObjective> GetAll();
        IReadOnlyList<IObjective> GetByTeam(int teamID);
    }
}
