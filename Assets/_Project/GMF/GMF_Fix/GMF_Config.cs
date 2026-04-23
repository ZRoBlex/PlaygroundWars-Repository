// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Config.cs  (REEMPLAZA el anterior)         ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + MatchConfig (config global de partida)              ║
// ║      - RoundsToWinMatch                                  ║
// ║      - SuddenDeath toggle                                ║
// ║      - KillTiebreaker toggle                             ║
// ║    + RoundConfig mejorado                                ║
// ║    + ScoreConfig con kill tracking                       ║
// ╚══════════════════════════════════════════════════════════╝

using System;
using UnityEngine;

namespace GMF.Config
{
    // ── Team ──────────────────────────────────────────────────

    [Serializable]
    public class TeamConfig
    {
        [Range(1, 8)]   public int     TeamCount  = 2;
        [Range(1, 16)]  public int     MaxPerTeam = 5;
        public bool    AutoBalance = true;
        public string[] TeamNames  = { "Red", "Blue" };
        public Color[]  TeamColors = { Color.red, Color.blue };
    }

    // ── Round ─────────────────────────────────────────────────

    [Serializable]
    public class RoundConfig
    {
        [Tooltip("Duración en segundos de cada ronda. 0 = sin límite.")]
        [Range(0f, 600f)] public float RoundDuration = 300f;

        [Tooltip("Segundos de warm-up al inicio de cada ronda.")]
        [Range(0f, 30f)]  public float WarmUpDuration = 5f;

        [Tooltip("Segundos de pantalla de resultado antes de iniciar la siguiente ronda.")]
        [Range(2f, 30f)]  public float RoundEndDuration = 6f;

        [Tooltip("Segundos de pantalla de resultado final al acabar la partida.")]
        [Range(2f, 30f)]  public float EndGameDuration = 8f;
    }

    // ── Match (partida completa) ───────────────────────────────

    [Serializable]
    public class MatchConfig
    {
        [Tooltip("Cuántas rondas debe ganar un equipo para ganar la partida.")]
        [Range(1, 10)]
        public int RoundsToWinMatch = 2;

        [Header("Empate")]
        [Tooltip("Si quedan equipos empatados en rondas, se agrega una ronda de muerte súbita.")]
        public bool SuddenDeathOnTie = true;

        [Tooltip(
            "Si el tiempo se acaba con empate en PUNTOS, el equipo con más kills gana la RONDA.\n" +
            "Si está desactivado, la ronda se declara empate.")]
        public bool KillTiebreakerOnTimeOut = true;
    }

    // ── Score ─────────────────────────────────────────────────

    [Serializable]
    public class ScoreConfig
    {
        [Range(1, 200)] public int  ScoreToWin      = 3;
        public          bool        TrackIndividual  = true;
        [Tooltip("Trackear kills por jugador para tiebreaker y estadísticas.")]
        public          bool        TrackKills       = true;
    }

    // ── Objective ─────────────────────────────────────────────

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

    [CreateAssetMenu(
        fileName = "NewGameMode",
        menuName = "GameMode Framework/GameMode Definition")]
    public class GMF_Config : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private string _modeID      = "new_mode";
        [SerializeField] private string _displayName = "New Mode";

        [Header("Reglas del juego")]
        [SerializeField] private RuleBaseSO[] _rules = Array.Empty<RuleBaseSO>();

        [Header("Condiciones de victoria")]
        [SerializeField] private WinConditionBaseSO[] _winConditions = Array.Empty<WinConditionBaseSO>();

        [Header("Configuración de Partida")]
        [SerializeField] private MatchConfig _matchConfig = new();

        [Header("Configuración de Equipos")]
        [SerializeField] private TeamConfig  _teamConfig  = new();

        [Header("Configuración de Rondas")]
        [SerializeField] private RoundConfig _roundConfig = new();

        [Header("Configuración de Puntos")]
        [SerializeField] private ScoreConfig _scoreConfig = new();

        // ── Acceso ────────────────────────────────────────────

        public string     ModeID      => _modeID;
        public string     DisplayName => _displayName;

        public MatchConfig  MatchConfig  => _matchConfig;
        public TeamConfig   TeamConfig   => _teamConfig;
        public RoundConfig  RoundConfig  => _roundConfig;
        public ScoreConfig  ScoreConfig  => _scoreConfig;

        public IGameRule[]     GetRules()
            => _rules == null
               ? Array.Empty<IGameRule>()
               : Array.ConvertAll(_rules, r => (IGameRule)r);

        public IWinCondition[] GetWinConditions()
            => _winConditions == null
               ? Array.Empty<IWinCondition>()
               : Array.ConvertAll(_winConditions, c => (IWinCondition)c);
    }
}