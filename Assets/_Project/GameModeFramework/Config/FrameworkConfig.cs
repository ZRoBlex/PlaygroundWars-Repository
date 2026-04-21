// ============================================================
//  FrameworkConfig.cs
//  GameModeFramework/Config/FrameworkConfig.cs
//
//  ScriptableObjects de configuración del framework.
//  Toda configuración vive aquí — cero hardcoding en código.
// ============================================================

using UnityEngine;

namespace GameMode.Framework.Config
{
    // ── Configuración de equipos ──────────────────────────────

    [System.Serializable]
    public class TeamConfig
    {
        [Tooltip("Cuántos equipos. 1 = FFA (cada jugador = equipo propio).")]
        [Range(1, 8)]
        public int   TeamCount     = 2;

        [Range(1, 16)]
        public int   MaxPerTeam    = 5;

        [Tooltip("Balancear automáticamente al inicio de ronda.")]
        public bool  AutoBalance   = true;

        [Tooltip("Nombres de los equipos (debe coincidir con TeamCount).")]
        public string[] TeamNames  = { "Red", "Blue" };

        public Color[] TeamColors  = { Color.red, Color.blue };
    }

    // ── Configuración de rondas ───────────────────────────────

    [System.Serializable]
    public class RoundConfig
    {
        [Range(1, 10)]
        public int   TotalRounds      = 1;

        [Range(1, 10)]
        public int   RoundsToWinMatch = 1;

        [Tooltip("Duración en segundos. 0 = sin límite de tiempo.")]
        [Range(0f, 600f)]
        public float RoundDuration    = 300f;

        [Range(0f, 30f)]
        public float WarmUpDuration   = 5f;

        [Range(2f, 15f)]
        public float RoundEndDuration = 5f;
    }

    // ── Configuración de puntuación ───────────────────────────

    [System.Serializable]
    public class ScoreConfig
    {
        [Tooltip("Si true, cada jugador también tiene score individual.")]
        public bool  TrackIndividualScore = true;

        [Tooltip("Puntos necesarios para ganar la ronda (WinCondition los usa).")]
        [Range(1, 100)]
        public int   ScoreToWin           = 3;
    }

    // ── Configuración de objetivo ─────────────────────────────

    [System.Serializable]
    public class ObjectiveConfig
    {
        public string ObjectiveID;
        public int    OwnerTeamID  = -1;   // -1 = neutral
        public bool   StartsActive = true;
    }

    // ════════════════════════════════════════════════════════
    //  GameModeDefinitionSO — La "ficha" de un modo de juego
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Define completamente un modo de juego via Inspector.
    /// Solo necesitas crear este asset y asignar:
    /// - qué reglas usa (IGameRule[])
    /// - qué condiciones de victoria (IWinCondition[])
    /// - configuración de equipos, rondas y score
    ///
    /// Para añadir un modo nuevo: crear un nuevo asset de este tipo.
    /// Sin tocar código existente.
    ///
    /// CREAR: Assets → Create → GameMode Framework → GameModeDefinition
    /// </summary>
    // [CreateAssetMenu(
    //     fileName = "NewGameMode",
    //     menuName = "GameMode Framework/GameModeDefinition",
    //     order    = 0)]
    // public class GameModeDefinitionSO : ScriptableObject, IGameModeDefinition
    // {
    //     [Header("Identidad")]
    //     [SerializeField] private string _modeID      = "new_mode";
    //     [SerializeField] private string _displayName = "New Mode";

    //     [Header("Reglas del juego")]
    //     [Tooltip("Lista de reglas que rigen este modo. Cada regla reacciona a eventos.")]
    //     [SerializeReference]
    //     private IGameRule[] _rules = System.Array.Empty<IGameRule>();

    //     [Header("Condiciones de victoria")]
    //     [Tooltip("Cuándo y cómo se gana. Evaluadas solo cuando el score cambia.")]
    //     [SerializeReference]
    //     private IWinCondition[] _winConditions = System.Array.Empty<IWinCondition>();

    //     [Header("Configuración")]
    //     [SerializeField] private TeamConfig  _teamConfig  = new();
    //     [SerializeField] private RoundConfig _roundConfig = new();
    //     [SerializeField] private ScoreConfig _scoreConfig = new();

    //     // ── IGameModeDefinition ───────────────────────────────

    //     public string ModeID      => _modeID;
    //     public string DisplayName => _displayName;

    //     public IGameRule[]     GetRules()          => _rules         ?? System.Array.Empty<IGameRule>();
    //     public IWinCondition[] GetWinConditions()  => _winConditions ?? System.Array.Empty<IWinCondition>();

    //     public TeamConfig  TeamConfig  => _teamConfig;
    //     public RoundConfig RoundConfig => _roundConfig;
    //     public ScoreConfig ScoreConfig => _scoreConfig;
    // }
}
