// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Config.cs  (REEMPLAZA el anterior)         ║
// ║                                                          ║
// ║  CAMBIO PRINCIPAL:                                       ║
// ║    ANTES: [SerializeReference] IGameRule[] _rules        ║
// ║    AHORA: [SerializeField]    RuleBaseSO[] _rules        ║
// ║                                                          ║
// ║  POR QUÉ:                                                ║
// ║    Con [SerializeField] + ScriptableObject puedes:       ║
// ║    • Crear el asset en Project window                    ║
// ║    • Arrastrarlo al array en Inspector                   ║
// ║    • Tener múltiples modos usando la MISMA instancia     ║
// ║      de una regla (no se duplican datos)                 ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using UnityEngine;

namespace GMF.Config
{
    [Serializable]
    public class TeamConfig
    {
        [Range(1, 8)]  public int     TeamCount  = 2;
        [Range(1, 16)] public int     MaxPerTeam = 5;
        public bool    AutoBalance = true;
        public string[] TeamNames  = { "Red", "Blue" };
        public Color[]  TeamColors = { Color.red, Color.blue };
    }

    [Serializable]
    public class RoundConfig
    {
        [Range(1, 10)]   public int   TotalRounds      = 1;
        [Range(1, 10)]   public int   RoundsToWinMatch = 1;
        [Range(0, 600f)] public float RoundDuration    = 300f;
        [Range(0, 30f)]  public float WarmUpDuration   = 5f;
        [Range(2, 15f)]  public float RoundEndDuration = 5f;
    }

    [Serializable]
    public class ScoreConfig
    {
        [Range(1, 200)] public int  ScoreToWin       = 3;
        public          bool        TrackIndividual   = true;
    }

    [Serializable]
    public class ObjectiveConfig
    {
        public string ObjectiveID  = "obj_default";
        public int    TeamID       = -1;
        public bool   StartsActive = true;
    }

    // ════════════════════════════════════════════════════════
    //  GAMEMODE DEFINITION SO
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Cómo usar en Unity:
    /// 1. Assets → Create → GameMode Framework → GameMode Definition
    /// 2. Para Rules: Assets → Create → GameMode Framework → Rules → [tipo]
    ///    Arrastra el asset al array Rules del GameModeDefinitionSO
    /// 3. Para WinConditions: igual, en WinConditions menu
    /// 4. Arrastrar el GameModeDefinitionSO al campo _def del GameModeBase
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewGameMode",
        menuName = "GameMode Framework/GameMode Definition")]
    public class GMF_Config : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private string _modeID      = "new_mode";
        [SerializeField] private string _displayName = "New Mode";

        [Header("Reglas (assets que hereden de RuleBaseSO)")]
        [SerializeField] private RuleBaseSO[] _rules = Array.Empty<RuleBaseSO>();

        [Header("Condiciones de victoria (assets que hereden de WinConditionBaseSO)")]
        [SerializeField] private WinConditionBaseSO[] _winConditions = Array.Empty<WinConditionBaseSO>();

        [Header("Configuración")]
        [SerializeField] private TeamConfig  _teamConfig  = new();
        [SerializeField] private RoundConfig _roundConfig = new();
        [SerializeField] private ScoreConfig _scoreConfig = new();

        public string ModeID      => _modeID;
        public string DisplayName => _displayName;
        public TeamConfig  TeamConfig  => _teamConfig;
        public RoundConfig RoundConfig => _roundConfig;
        public ScoreConfig ScoreConfig => _scoreConfig;

        public IGameRule[] GetRules()
            => _rules == null
               ? Array.Empty<IGameRule>()
               : Array.ConvertAll(_rules, r => (IGameRule)r);

        public IWinCondition[] GetWinConditions()
            => _winConditions == null
               ? Array.Empty<IWinCondition>()
               : Array.ConvertAll(_winConditions, c => (IWinCondition)c);
    }
}
