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
    [CreateAssetMenu(
        fileName = "NewGameMode",
        menuName = "GameMode Framework/GameModeDefinition",
        order    = 0)]
    public class GameModeDefinitionSO : ScriptableObject, IGameModeDefinition
    {
        [Header("Identidad")]
        [SerializeField] private string _modeID      = "new_mode";
        [SerializeField] private string _displayName = "New Mode";

        [Header("Reglas del juego")]
        [Tooltip("Lista de reglas que rigen este modo. Cada regla reacciona a eventos.")]
        [SerializeReference]
        private IGameRule[] _rules = System.Array.Empty<IGameRule>();

        [Header("Condiciones de victoria")]
        [Tooltip("Cuándo y cómo se gana. Evaluadas solo cuando el score cambia.")]
        [SerializeReference]
        private IWinCondition[] _winConditions = System.Array.Empty<IWinCondition>();

        [Header("Configuración")]
        [SerializeField] private TeamConfig  _teamConfig  = new();
        [SerializeField] private RoundConfig _roundConfig = new();
        [SerializeField] private ScoreConfig _scoreConfig = new();

        // ── IGameModeDefinition ───────────────────────────────

        public string ModeID      => _modeID;
        public string DisplayName => _displayName;

        public IGameRule[]     GetRules()          => _rules         ?? System.Array.Empty<IGameRule>();
        public IWinCondition[] GetWinConditions()  => _winConditions ?? System.Array.Empty<IWinCondition>();

        public TeamConfig  TeamConfig  => _teamConfig;
        public RoundConfig RoundConfig => _roundConfig;
        public ScoreConfig ScoreConfig => _scoreConfig;
    }
}
