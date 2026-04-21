// ============================================================
//  FrameworkEditorWindow.cs
//  GameModeFramework/Editor/FrameworkEditorWindow.cs
//
//  ABRIR: Window → GameMode Framework → Framework Debug
//
//  TABS: GameMode | Equipos | Objetivos | Score | Logs
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using GameMode.Framework.Events;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace GameMode.Framework.Editor
{
    public class FrameworkEditorWindow : EditorWindow
    {
        private int    _tab;
        private readonly string[] _tabs = { "GameMode", "Equipos", "Objetivos", "Score", "Logs" };
        private Vector2 _scroll, _logScroll;

        private GameModeBase    _gameMode;
        private GameModeManager _manager;

        private readonly List<string> _logs = new();
        private const    int          MAX  = 60;
        private bool _autoScroll = true, _subbed;

        [MenuItem("Window/GameMode Framework/Framework Debug")]
        public static void Open()
        {
            var w = GetWindow<FrameworkEditorWindow>("Framework Debug");
            w.minSize = new Vector2(440, 520);
            w.Show();
        }

        private void OnEnable()
        {
            Subscribe();
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private void OnDisable()
        {
            Unsubscribe();
            EditorApplication.playModeStateChanged -= OnPlayMode;
        }

        private void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode)  Subscribe();
            if (s == PlayModeStateChange.ExitingPlayMode) { Unsubscribe(); _logs.Clear(); }
        }

        private void Subscribe()
        {
            if (_subbed) return;
            EventBus<GameStartedEvent>.Subscribe(e         => Log($"<color=lime>[START]</color> {e.ModeID} R{e.Round}"));
            EventBus<GameEndedEvent>.Subscribe(e           => Log($"<color=cyan>[END]</color> {e.ModeID} Winner=T{e.WinnerTeamID} ({e.Reason})"));
            EventBus<RoundStartedEvent>.Subscribe(e        => Log($"<color=#aaffaa>[ROUND {e.Round}]</color> {e.Duration:F0}s"));
            EventBus<RoundEndedEvent>.Subscribe(e          => Log($"<color=gold>[ROUND END]</color> Winner=T{e.WinnerTeamID}"));
            EventBus<ObjectiveInteractedEvent>.Subscribe(e => Log($"<color=yellow>[OBJ]</color> '{e.ObjectiveID}' {e.InteractionType} P{e.PlayerID}(T{e.PlayerTeamID})"));
            EventBus<ObjectiveScoredEvent>.Subscribe(e     => Log($"<color=orange>⚡[SCORE OBJ]</color> T{e.ScoringTeamID} +{e.Points} '{e.ObjectiveID}'"));
            EventBus<ScoreChangedEvent>.Subscribe(e        => Log($"<color=#88ff88>[SCORE]</color> T{e.TeamID} +{e.Delta} = {e.NewTeamTotal} ({e.Reason})"));
            EventBus<PlayerJoinedTeamEvent>.Subscribe(e    => Log($"[TEAM] P{e.PlayerID} → {e.TeamName}"));
            EventBus<RoundTimerTickEvent>.Subscribe(e      => { /* No loguear timer — muy verbose */ });
            _subbed = true;
        }

        private void Unsubscribe()
        {
            if (!_subbed) return;
            EventBus<GameStartedEvent>.Clear();
            EventBus<GameEndedEvent>.Clear();
            EventBus<RoundStartedEvent>.Clear();
            EventBus<RoundEndedEvent>.Clear();
            EventBus<ObjectiveInteractedEvent>.Clear();
            EventBus<ObjectiveScoredEvent>.Clear();
            EventBus<ScoreChangedEvent>.Clear();
            EventBus<PlayerJoinedTeamEvent>.Clear();
            _subbed = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🏆 Framework Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
            {
                _gameMode = FindFirstObjectByType<GameModeBase>();
                _manager  = FindFirstObjectByType<GameModeManager>();
            }
            GUI.color = Application.isPlaying ? Color.green : Color.gray;
            GUILayout.Label(Application.isPlaying ? "● PLAY" : "● EDIT", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawGameModeTab();    break;
                case 1: DrawTeamsTab();       break;
                case 2: DrawObjectivesTab();  break;
                case 3: DrawScoreTab();       break;
                case 4: DrawLogsTab();        break;
            }
            EditorGUILayout.EndScrollView();
            if (Application.isPlaying) Repaint();
        }

        private void DrawGameModeTab()
        {
            if (!Check()) return;

            EditorGUILayout.LabelField($"Modo: {_gameMode.ModeID}", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Fase",      _gameMode.Context?.Phase ?? GameModePhase.Idle);
            EditorGUILayout.IntField("Ronda",      _gameMode.Context?.CurrentRound ?? 0);
            EditorGUILayout.FloatField("Tiempo",   _gameMode.Context?.ElapsedTime  ?? 0f);
            EditorGUILayout.Toggle("IsRunning",    _gameMode.IsRunning);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("▶ StartGame"))    _gameMode.StartGame();
            if (GUILayout.Button("⏹ EndGame T0"))   _gameMode.EndGame(0, "Editor");
            if (GUILayout.Button("⏹ EndGame T1"))   _gameMode.EndGame(1, "Editor");
            if (GUILayout.Button("↺ ResetGame"))    _gameMode.ResetGame();
        }

        private void DrawTeamsTab()
        {
            if (!Check()) return;
            var teams = _gameMode.Context?.Teams;
            if (teams == null) { EditorGUILayout.HelpBox("Sin contexto.", MessageType.Info); return; }

            EditorGUILayout.LabelField("Equipos", EditorStyles.boldLabel);
            for (int t = 0; t < teams.TeamCount; t++)
            {
                var players = teams.GetPlayersInTeam(t);
                EditorGUILayout.LabelField($"Team {t} ({players.Count} jugadores)", EditorStyles.boldLabel);
                foreach (int id in players)
                    EditorGUILayout.LabelField($"  • P{id}");
            }
        }

        private void DrawObjectivesTab()
        {
            if (!Check()) return;
            var objs = _gameMode.Context?.Objectives?.GetAll();
            if (objs == null || objs.Count == 0)
            { EditorGUILayout.HelpBox("Sin objetivos registrados.", MessageType.Info); return; }

            foreach (var obj in objs)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(obj.ObjectiveID, EditorStyles.boldLabel);

                GUI.color = obj.IsActive ? Color.white : Color.gray;
                EditorGUILayout.LabelField($"Estado:  {obj.CurrentState}");
                EditorGUILayout.LabelField($"Equipo:  {(obj.OwnerTeamID < 0 ? "Neutral" : $"T{obj.OwnerTeamID}")}");
                GUI.color = Color.white;

                if (GUILayout.Button("↺ Reset Objetivo"))
                    obj.Reset();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawScoreTab()
        {
            if (!Check()) return;
            var score = _gameMode.Context?.Score;
            var teams = _gameMode.Context?.Teams;
            if (score == null) return;

            EditorGUILayout.LabelField("Puntuación por Equipo", EditorStyles.boldLabel);
            for (int t = 0; t < (teams?.TeamCount ?? 2); t++)
            {
                string bar = new string('█', Mathf.Min(score.GetTeamScore(t), 20));
                EditorGUILayout.LabelField($"T{t}: {score.GetTeamScore(t):D3}  {bar}");
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("+ Punto T0"))
                // (_gameMode.Context as GameModeContext)?._score.AddTeamScore(0, 1, -1, "Editor");
            GUI.backgroundColor = new Color(0.5f, 0.5f, 1f);
            if (GUILayout.Button("+ Punto T1"))
                // (_gameMode.Context as GameModeContext)?._score.AddTeamScore(1, 1, -1, "Editor");
            GUI.backgroundColor = Color.white;
        }

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(350));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);
            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        private bool Check()
        {
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return false; }
            if (_gameMode == null)
            { EditorGUILayout.HelpBox("GameModeBase no encontrado. Pulsa 'Find'.", MessageType.Warning); return false; }
            return true;
        }

        private void Log(string msg)
        {
            _logs.Add($"<color=#888>{System.DateTime.Now:HH:mm:ss}</color> {msg}");
            if (_logs.Count > MAX) _logs.RemoveAt(0);
        }
    }
}
#endif
