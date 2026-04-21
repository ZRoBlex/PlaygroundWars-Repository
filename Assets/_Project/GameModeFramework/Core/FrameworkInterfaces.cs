// ============================================================
//  FrameworkInterfaces.cs
//  GameModeFramework/Core/FrameworkInterfaces.cs
//
//  Contratos del GameMode Framework.
//  Estos son los únicos puntos de extensión del sistema.
//  Para añadir un nuevo modo: implementar estas interfaces.
//  NUNCA modificar estas interfaces si ya hay implementaciones.
// ============================================================

using GameMode.Framework.Events;

namespace GameMode.Framework
{
    // ════════════════════════════════════════════════════════
    //  REGLAS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Una regla reacciona a eventos del juego y puede emitir
    /// nuevos eventos (ej: ObjectiveScoredEvent).
    ///
    /// RESPONSABILIDAD: Interpretar qué pasó, no decidir quién ganó.
    /// RESTRICCIÓN: No aplica daño, no mueve objetos directamente.
    ///              Solo emite eventos que otros sistemas procesan.
    /// </summary>
    public interface IGameRule
    {
        string  RuleID   { get; }
        bool    IsEnabled { get; set; }

        void Initialize(IGameModeContext ctx);
        void OnEvent(ObjectiveInteractedEvent evt);
        void OnEvent(PlayerEliminatedEvent evt);
        void Dispose();
    }

    // ════════════════════════════════════════════════════════
    //  CONDICIONES DE VICTORIA
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Evalúa el estado del juego y determina si alguien ha ganado.
    ///
    /// CUÁNDO SE EVALÚA: Solo cuando ScoreChangedEvent,
    /// RoundEndedEvent, o PlayerEliminatedEvent ocurren.
    /// NUNCA en Update.
    ///
    /// Retorna WinResult. Si nadie ganó, WinResult.NoWinner.
    /// </summary>
    public interface IWinCondition
    {
        string     ConditionID { get; }
        WinResult  Evaluate(IGameModeContext ctx);
        void       Initialize(IGameModeContext ctx);
    }

    /// <summary>Resultado de evaluar una condición de victoria.</summary>
    public readonly struct WinResult
    {
        public bool   Won          { get; }
        public int    WinnerTeamID { get; }
        public string Reason       { get; }

        public static WinResult NoWinner    => new(false, -1, "");
        public static WinResult Draw        => new(true,  -1, "Draw");

        public WinResult(bool won, int winnerTeamID, string reason)
        {
            Won          = won;
            WinnerTeamID = winnerTeamID;
            Reason       = reason;
        }
    }

    // ════════════════════════════════════════════════════════
    //  OBJETIVOS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Una entidad de escena que puede ser interactuada.
    /// Flag, CaptureZone, ControlPoint, Payload... todos implementan esto.
    ///
    /// RESPONSABILIDAD: Detectar interacciones físicas y emitir eventos.
    /// RESTRICCIÓN: No conoce las reglas del modo. No sabe qué pasa
    ///              cuando es capturado. Solo reporta que fue interactuado.
    /// </summary>
    public interface IObjective
    {
        string ObjectiveID    { get; }
        int    OwnerTeamID    { get; }     // -1 = neutral
        bool   IsActive       { get; }
        string CurrentState   { get; }     // "Idle", "Carried", "Dropped", "Contested"

        void   Initialize(GameMode.Framework.Config.ObjectiveConfig cfg);
        void   Reset();
        void   SetActive(bool active);
    }

    // ════════════════════════════════════════════════════════
    //  CONTEXTO (solo lectura para reglas y condiciones)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Vista de solo lectura del estado del juego.
    /// Las reglas y condiciones de victoria solo pueden LEER el contexto.
    /// Solo GameModeBase puede modificarlo (via GameModeContext interno).
    /// </summary>
    public interface IGameModeContext
    {
        string          ModeID       { get; }
        GameModePhase   Phase        { get; }
        int             CurrentRound { get; }
        float           ElapsedTime  { get; }

        IReadOnlyTeamRegistry      Teams      { get; }
        IReadOnlyScoreSystem       Score      { get; }
        IReadOnlyObjectiveRegistry Objectives { get; }
    }

    // ════════════════════════════════════════════════════════
    //  INTERFACES DE SOLO LECTURA para subsistemas
    // ════════════════════════════════════════════════════════

    public interface IReadOnlyTeamRegistry
    {
        int   TeamCount                          { get; }
        int   GetTeamOf(int playerID);
        bool  AreEnemies(int playerA, int playerB);
        bool  AreTeammates(int playerA, int playerB);
        System.Collections.Generic.IReadOnlyList<int> GetPlayersInTeam(int teamID);
        int   GetScore(int teamID);
    }

    public interface IReadOnlyScoreSystem
    {
        int   GetTeamScore(int teamID);
        int   GetPlayerScore(int playerID);
        int   GetLeadingTeam();
    }

    public interface IReadOnlyObjectiveRegistry
    {
        IObjective GetObjective(string objectiveID);
        System.Collections.Generic.IReadOnlyList<IObjective> GetAll();
        System.Collections.Generic.IReadOnlyList<IObjective> GetByTeam(int teamID);
    }

    // ════════════════════════════════════════════════════════
    //  DEFINICIÓN DE UN MODO DE JUEGO
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Todo lo necesario para definir un modo de juego.
    /// Implementado como ScriptableObject (GameModeDefinitionSO).
    /// El GameModeBase lo lee para configurarse.
    /// </summary>
    public interface IGameModeDefinition
    {
        string         ModeID         { get; }
        string         DisplayName    { get; }

        IGameRule[]       GetRules();
        IWinCondition[]   GetWinConditions();

        GameMode.Framework.Config.TeamConfig   TeamConfig  { get; }
        GameMode.Framework.Config.RoundConfig  RoundConfig { get; }
        GameMode.Framework.Config.ScoreConfig  ScoreConfig { get; }
    }

    // Enum compartido
    public enum GameModePhase { Idle, WarmUp, Playing, RoundEnd, PostGame }
}
