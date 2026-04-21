// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Config.cs                                  ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • TeamConfig        (Serializable class)              ║
// ║    • RoundConfig       (Serializable class)              ║
// ║    • ScoreConfig       (Serializable class)              ║
// ║    • ObjectiveConfig   (Serializable class)              ║
// ║    • GameModeDefinitionSO (ScriptableObject) ← PRINCIPAL ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Toda la configuración de un modo de juego vive aquí.  ║
// ║    Para crear un modo nuevo: crear un asset de           ║
// ║    GameModeDefinitionSO y asignar reglas + condiciones.  ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA: Ninguna.                          ║
// ║    Las configs son POCOs — sin lógica.                   ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    Assets → Right Click → Create →                       ║
// ║    GameMode Framework → GameMode Definition              ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF.Config
{
    // ════════════════════════════════════════════════════════
    //  GAMEMODE DEFINITION SO — el "blueprint" de un modo
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Define completamente un modo de juego.
    /// Para añadir un modo nuevo: crear este asset y asignar
    /// rules + winConditions. SIN modificar código.
    ///
    /// CÓMO CREAR:
    ///   Assets → Right Click → Create →
    ///   GameMode Framework → GameMode Definition
    ///
    /// CÓMO CONECTAR:
    ///   Asignar al campo _definition del componente GameModeBase
    ///   en tu GameObject de modo de juego en la escena.
    ///
    /// ERRORES COMUNES:
    ///   • Rules vacío → el modo no reacciona a nada
    ///   • WinConditions vacío → la partida nunca termina
    ///   • TeamConfig.TeamNames.Length != TeamCount → NullRef
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewGameMode",
        menuName = "GameMode Framework/GameMode Definition")]
    public class GameModeDefinitionSO : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private string _modeID      = "new_mode";
        [SerializeField] private string _displayName = "New Mode";

        [Header("Reglas — reaccionan a eventos")]
        [Tooltip("Cada regla implementa IGameRule. Usa [SerializeReference].")]
        [SerializeReference]
        private IGameRule[] _rules = System.Array.Empty<IGameRule>();

        [Header("Condiciones de victoria")]
        [Tooltip("Cada condición implementa IWinCondition. Evaluadas al cambiar score.")]
        [SerializeReference]
        private IWinCondition[] _winConditions = System.Array.Empty<IWinCondition>();

        [Header("Configuración")]
        [SerializeField] private TeamConfig  _teamConfig  = new();
        [SerializeField] private RoundConfig _roundConfig = new();
        [SerializeField] private ScoreConfig _scoreConfig = new();

        // ── Acceso de solo lectura ────────────────────────────

        public string     ModeID      => _modeID;
        public string     DisplayName => _displayName;

        public IGameRule[]     GetRules()
            => _rules         ?? System.Array.Empty<IGameRule>();

        public IWinCondition[] GetWinConditions()
            => _winConditions ?? System.Array.Empty<IWinCondition>();

        public TeamConfig  TeamConfig  => _teamConfig;
        public RoundConfig RoundConfig => _roundConfig;
        public ScoreConfig ScoreConfig => _scoreConfig;
    }
}
