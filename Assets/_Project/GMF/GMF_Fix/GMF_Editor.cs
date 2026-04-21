// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Editor.cs  (REEMPLAZA el anterior)         ║
// ║  CARPETA: Assets/_Project/GameModeFramework/Editor/      ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + FindObjectsInactive.Include en Find()               ║
// ║      Antes no encontraba GameModeBase si estaba inactivo ║
// ║    + Muestra si GameModeBase.Instance está activo        ║
// ╚══════════════════════════════════════════════════════════╝

#if UNITY_EDITOR
using System.Collections.Generic;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace GMF.Editor
{
    public class GMFEditorWindow : EditorWindow
    {
        private int    _tab;
        private readonly string[] _tabs = { "GameMode", "Score", "Objetivos", "Logs" };
        private Vector2 _scroll, _logScroll;

        private GameModeBase    _gm;
        private GameModeManager _mgr;

        private readonly List<string> _logs = new();
        private const    int          MAX = 60;
        private bool _autoScroll = true, _subbed;

        [MenuItem("Window/GameMode Framework/Debug Window")]
        public static void Open()
        {
            var w = GetWindow<GMFEditorWindow>("GMF Debug");
            w.minSize = new Vector2(400, 480);
            w.Show();
        }

        private void OnEnable()  { Subscribe();   EditorApplication.playModeStateChanged += OnPlayMode; }
        private void OnDisable() { Unsubscribe(); EditorApplication.playModeStateChanged -= OnPlayMode; }

        private void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) Subscribe();
            if (s == PlayModeStateChange.ExitingPlayMode) { Unsubscribe(); _logs.Clear(); }
        }

        private void Subscribe()
        {
            if (_subbed) return;
            EventBus<GameStartedEvt>.Subscribe(e     => Log($"<color=lime>[START]</color> {e.ModeID}"));
            EventBus<GameEndedEvt>.Subscribe(e       => Log($"<color=cyan>[END]</color> T{e.WinnerTeamID} ({e.Reason})"));
            EventBus<RoundStartedEvt>.Subscribe(e    => Log($"<color=#aaffaa>[ROUND {e.Round}]</color>"));
            EventBus<RoundEndedEvt>.Subscribe(e      => Log($"<color=gold>[ROUND END]</color> T{e.WinnerTeamID}"));
            EventBus<ObjectiveInteractedEvt>.Subscribe(e => Log($"<color=yellow>[OBJ]</color> '{e.ObjectiveID}' {e.Interaction} P{e.PlayerID}(T{e.PlayerTeamID})"));
            EventBus<ScoreChangedEvt>.Subscribe(e    => Log($"<color=#88ff88>[SCORE]</color> T{e.TeamID} +{e.Delta}={e.NewTeamTotal}"));
            EventBus<PlayerJoinedTeamEvt>.Subscribe(e => Log($"[TEAM] P{e.PlayerID}→{e.TeamName}"));
            _subbed = true;
        }

        private void Unsubscribe()
        {
            if (!_subbed) return;
            EventBus<GameStartedEvt>.Clear();
            EventBus<GameEndedEvt>.Clear();
            EventBus<RoundStartedEvt>.Clear();
            EventBus<RoundEndedEvt>.Clear();
            EventBus<ObjectiveInteractedEvt>.Clear();
            EventBus<ScoreChangedEvt>.Clear();
            EventBus<PlayerJoinedTeamEvt>.Clear();
            _subbed = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🏆 GMF Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // ✅ FIX: FindObjectsInactive.Include → encuentra aunque esté inactivo
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
            {
                _gm  = Object.FindFirstObjectByType<GameModeBase>(FindObjectsInactive.Include);
                _mgr = Object.FindFirstObjectByType<GameModeManager>(FindObjectsInactive.Include);
            }

            // Indicador: ¿hay un Instance activo?
            bool hasInstance = GameModeBase.Instance != null;
            GUI.color = hasInstance ? Color.green : (Application.isPlaying ? Color.yellow : Color.gray);
            GUILayout.Label(hasInstance ? "● RUNNING" : (Application.isPlaying ? "● IDLE" : "● EDIT"),
                EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawGameModeTab();   break;
                case 1: DrawScoreTab();      break;
                case 2: DrawObjectivesTab(); break;
                case 3: DrawLogsTab();       break;
            }
            EditorGUILayout.EndScrollView();
            if (Application.isPlaying) Repaint();
        }

        private void DrawGameModeTab()
        {
            if (!Check()) return;

            EditorGUILayout.LabelField($"Modo: {_gm.ModeID}", EditorStyles.boldLabel);

            // Mostrar si es el Instance activo
            bool isActive = GameModeBase.Instance == _gm;
            GUI.color = isActive ? Color.green : Color.gray;
            EditorGUILayout.LabelField(isActive ? "✓ Instance activo" : "○ No es el activo");
            GUI.color = Color.white;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Fase",     _gm.Context?.Phase ?? GameModePhase.Idle);
            EditorGUILayout.IntField("Ronda",     _gm.Context?.CurrentRound ?? 0);
            EditorGUILayout.FloatField("Tiempo",  _gm.Context?.ElapsedTime  ?? 0f);
            EditorGUILayout.Toggle("IsRunning",   _gm.IsRunning);
            EditorGUILayout.Toggle("IsAuthority", _gm.IsAuthority);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("▶ StartGame"))  _gm.StartGame();
            if (GUILayout.Button("⏹ EndGame T0")) _gm.EndGame(0, "Editor");
            if (GUILayout.Button("⏹ EndGame T1")) _gm.EndGame(1, "Editor");
            if (GUILayout.Button("↺ ResetGame"))  _gm.ResetGame();

            if (_mgr != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("GameModeManager", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Activo", _mgr.ActiveMode?.ModeID ?? "—");
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("[Manager] Start Default"))
                    _mgr.StartDefault();
            }
        }

        private void DrawScoreTab()
        {
            if (!Check()) return;
            var score = _gm.Context?.Score;
            var teams = _gm.Context?.Teams;
            if (score == null) return;

            EditorGUILayout.LabelField("Puntuación", EditorStyles.boldLabel);
            int tc = teams?.TeamCount ?? 2;
            for (int t = 0; t < tc; t++)
            {
                int s   = score.GetTeamScore(t);
                string bar = new string('█', Mathf.Min(s, 30));
                EditorGUILayout.LabelField($"T{t}: {s:D3}  {bar}");
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("+ Punto T0"))
                (_gm.Context as GameModeContext)?._score.AddScore(0, 1, -1, "Editor");
            GUI.backgroundColor = new Color(0.5f, 0.5f, 1f);
            if (GUILayout.Button("+ Punto T1"))
                (_gm.Context as GameModeContext)?._score.AddScore(1, 1, -1, "Editor");
            GUI.backgroundColor = Color.white;
        }

        private void DrawObjectivesTab()
        {
            if (!Check()) return;
            var objs = _gm.Context?.Objectives?.GetAll();
            if (objs == null || objs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Sin objetivos registrados.\n" +
                    "Los ObjectiveBase se registran en Start().\n" +
                    "Asegura que GameModeBase está en la escena y el modo ha arrancado.",
                    MessageType.Info);
                return;
            }

            foreach (var obj in objs)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(obj.ObjectiveID, EditorStyles.boldLabel);
                GUI.color = obj.State == "Idle"    ? Color.green  :
                            obj.State == "Carried" ? Color.yellow : Color.red;
                EditorGUILayout.LabelField($"Estado: {obj.State}  |  T{obj.TeamID}");
                GUI.color = Color.white;
                if (GUILayout.Button("↺ Reset")) obj.Reset();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(320));
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
            if (_gm == null)
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
