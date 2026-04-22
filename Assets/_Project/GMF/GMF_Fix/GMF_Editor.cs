// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Editor.cs  (REEMPLAZA el anterior)         ║
// ║                                                          ║
// ║  FIXES:                                                  ║
// ║    ✅ Toma referencia del GameModeManager primero        ║
// ║       luego lee ActiveMode — no depende de Find          ║
// ║    ✅ Score y Objetivos siempre visibles desde el mgr    ║
// ║    ✅ Tab "Equipos" con lista de jugadores               ║
// ║    ✅ Botón "Forzar Cambio Equipo" para tests            ║
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
        private readonly string[] _tabs = { "GameMode", "Score", "Objetivos", "Equipos", "Logs" };
        private Vector2 _scroll, _logScroll;

        // ✅ Referencia via GameModeManager — no directa a GameModeBase
        private GameModeManager _mgr;
        private GameModeBase    _manualGM; // solo si no hay manager

        private readonly List<string> _logs = new();
        private const    int          MAX = 80;
        private bool _autoScroll = true, _subbed;

        // Para forzar cambio de equipo en editor
        private int _forcePlayerID  = 0;
        private int _forceTeamID    = 0;

        [MenuItem("Window/GameMode Framework/Debug Window")]
        public static void Open()
        {
            var w = GetWindow<GMFEditorWindow>("GMF Debug");
            w.minSize = new Vector2(420, 520);
            w.Show();
        }

        private void OnEnable()  { Subscribe();   EditorApplication.playModeStateChanged += OnPlayMode; }
        private void OnDisable() { Unsubscribe(); EditorApplication.playModeStateChanged -= OnPlayMode; }

        private void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { Subscribe(); AutoFind(); }
            if (s == PlayModeStateChange.ExitingPlayMode) { Unsubscribe(); _logs.Clear(); _mgr = null; _manualGM = null; }
        }

        private void AutoFind()
        {
            _mgr = Object.FindFirstObjectByType<GameModeManager>(FindObjectsInactive.Include);
            if (_mgr == null)
                _manualGM = Object.FindFirstObjectByType<GameModeBase>(FindObjectsInactive.Include);
        }

        // ── Acceso centralizado ───────────────────────────────

        private GameModeBase ActiveGM
            => _mgr?.ActiveMode ?? _manualGM ?? GameModeBase.Instance;

        private void Subscribe()
        {
            if (_subbed) return;
            EventBus<GameStartedEvt>.Subscribe(e        => Log($"<color=lime>[START]</color> {e.ModeID}"));
            EventBus<GameEndedEvt>.Subscribe(e          => Log($"<color=cyan>[END]</color> T{e.WinnerTeamID} ({e.Reason})"));
            EventBus<RoundStartedEvt>.Subscribe(e       => Log($"<color=#aaffaa>[ROUND {e.Round}]</color> {e.Duration:F0}s"));
            EventBus<RoundEndedEvt>.Subscribe(e         => Log($"<color=gold>[ROUND END]</color> T{e.WinnerTeamID}"));
            EventBus<ObjectiveInteractedEvt>.Subscribe(e=> Log($"<color=yellow>[OBJ]</color> '{e.ObjectiveID}' <b>{e.Interaction}</b> P{e.PlayerID}(T{e.PlayerTeamID})"));
            EventBus<ScoreChangedEvt>.Subscribe(e       => Log($"<color=#88ff88>[SCORE]</color> T{e.TeamID} +{e.Delta}={e.NewTeamTotal} ({e.Reason})"));
            EventBus<PlayerJoinedTeamEvt>.Subscribe(e   => Log($"[TEAM] P{e.PlayerID}→{e.TeamName}"));
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
            DrawHeader();
            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawGameModeTab();   break;
                case 1: DrawScoreTab();      break;
                case 2: DrawObjectivesTab(); break;
                case 3: DrawTeamsTab();      break;
                case 4: DrawLogsTab();       break;
            }
            EditorGUILayout.EndScrollView();
            if (Application.isPlaying) Repaint();
        }

        // ── Header ────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🏆 GMF Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Auto Find", GUILayout.Width(70)) && Application.isPlaying)
                AutoFind();

            // Indicador de estado
            bool hasGM = ActiveGM != null;
            bool running = hasGM && ActiveGM.IsRunning;
            GUI.color = running ? Color.green : hasGM ? Color.yellow : Color.gray;
            GUILayout.Label(running ? "● RUNNING" : hasGM ? "● IDLE" : "● NO GM",
                EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // Segunda fila: referencias
            if (!Application.isPlaying) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _mgr = (GameModeManager)EditorGUILayout.ObjectField(
                _mgr, typeof(GameModeManager), true, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            if (_mgr != null)
                GUILayout.Label($"Activo: {_mgr.ActiveMode?.ModeID ?? "—"}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: GameMode ─────────────────────────────────────

        private void DrawGameModeTab()
        {
            if (!Check()) return;
            var gm  = ActiveGM;
            var ctx = gm.Context;

            EditorGUILayout.LabelField($"Modo: {gm.ModeID}  [{(gm.IsRunning ? "RUNNING" : "IDLE")}]",
                EditorStyles.boldLabel);

            if (ctx != null)
            {
                Color phaseColor = ctx.Phase switch
                {
                    GameModePhase.Playing  => Color.green,
                    GameModePhase.WarmUp   => Color.yellow,
                    GameModePhase.RoundEnd => new Color(1f,0.6f,0f),
                    GameModePhase.PostGame => Color.cyan,
                    _                      => Color.gray
                };

                var phaseStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = phaseColor } };
                EditorGUILayout.LabelField($"Fase: {ctx.Phase}", phaseStyle);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Ronda",  ctx.CurrentRound);
                EditorGUILayout.FloatField("Tiempo", ctx.ElapsedTime);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Start")) gm.StartGame();
            if (GUILayout.Button("⏹ End T0")) gm.EndGame(0, "Editor");
            if (GUILayout.Button("⏹ End T1")) gm.EndGame(1, "Editor");
            if (GUILayout.Button("↺ Reset"))  gm.ResetGame();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("👥 Asignar todos los jugadores a equipos"))
                gm.AssignAllPlayersToTeams();

            if (_mgr != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("GameModeManager", EditorStyles.boldLabel);
                if (GUILayout.Button("[Manager] Start Default")) _mgr.StartDefault();
            }
        }

        // ── Tab: Score ────────────────────────────────────────

        private void DrawScoreTab()
        {
            if (!Check()) return;
            var gm  = ActiveGM;
            var ctx = gm.Context;

            if (ctx?.Score == null)
            {
                EditorGUILayout.HelpBox("Score no disponible. Inicia el juego primero.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Puntuación por Equipo", EditorStyles.boldLabel);

            int tc = ctx.Teams?.TeamCount ?? 2;
            for (int t = 0; t < tc; t++)
            {
                int    s    = ctx.Score.GetTeamScore(t);
                string bar  = new string('█', Mathf.Min(s, 25));
                Color  col  = t == 0 ? new Color(1f,0.4f,0.4f) : new Color(0.4f,0.6f,1f);
                var    style = new GUIStyle(EditorStyles.label) { normal = { textColor = col } };
                EditorGUILayout.LabelField($"T{t}: {s:D3}  {bar}", style);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("+ Punto T0"))
                (ctx as GameModeContext)?._score.AddScore(0, 1, -1, "Editor");
            GUI.backgroundColor = new Color(0.4f, 0.5f, 1f);
            if (GUILayout.Button("+ Punto T1"))
                (ctx as GameModeContext)?._score.AddScore(1, 1, -1, "Editor");
            GUI.backgroundColor = Color.white;

            // Player scores
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Score Individual", EditorStyles.boldLabel);
            for (int t = 0; t < tc; t++)
            {
                var players = ctx.Teams?.GetPlayers(t);
                if (players == null) continue;
                foreach (int pid in players)
                    EditorGUILayout.LabelField($"  P{pid} (T{t}): {ctx.Score.GetPlayerScore(pid)}");
            }
        }

        // ── Tab: Objetivos ────────────────────────────────────

        private void DrawObjectivesTab()
        {
            if (!Check()) return;
            var gm  = ActiveGM;
            var objs = gm.Context?.Objectives?.GetAll();

            if (objs == null || objs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Sin objetivos registrados.\n" +
                    "Los ObjectiveBase se registran en Start().\n" +
                    "Asegura que hay un GameModeBase activo.",
                    MessageType.Info);
                return;
            }

            foreach (var obj in objs)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"{obj.ObjectiveID}  [T{obj.TeamID}]", EditorStyles.boldLabel);

                Color stateCol = obj.State switch
                {
                    "Idle"     => Color.green,
                    "Carried"  => Color.yellow,
                    "Dropped"  => new Color(1f,0.5f,0f),
                    "Captured" => Color.cyan,
                    _          => Color.white
                };
                var st = new GUIStyle(EditorStyles.label) { normal = { textColor = stateCol } };
                EditorGUILayout.LabelField($"Estado: {obj.State}", st);

                if (obj is Flag flag && flag.IsBeingCarried)
                    EditorGUILayout.LabelField($"Portador: P{flag.CarrierID}");

                if (GUILayout.Button("↺ Reset")) obj.Reset();
                EditorGUILayout.EndVertical();
            }
        }

        // ── Tab: Equipos ──────────────────────────────────────

        private void DrawTeamsTab()
        {
            if (!Check()) return;
            var ctx = ActiveGM.Context;
            if (ctx?.Teams == null) { EditorGUILayout.HelpBox("Sin contexto.", MessageType.Info); return; }

            int tc = ctx.Teams.TeamCount;
            for (int t = 0; t < tc; t++)
            {
                Color col   = t == 0 ? new Color(1f,0.4f,0.4f) : new Color(0.4f,0.6f,1f);
                var   style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = col } };
                var   players = ctx.Teams.GetPlayers(t);

                EditorGUILayout.LabelField($"T{t}: {players.Count} jugadores", style);
                foreach (int pid in players)
                {
                    int sc = ctx.Score?.GetPlayerScore(pid) ?? 0;
                    EditorGUILayout.LabelField($"  • P{pid}  (score:{sc})");
                }
            }

            // Forzar asignación manual
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Forzar asignación", EditorStyles.boldLabel);
            _forcePlayerID = EditorGUILayout.IntField("Player ID", _forcePlayerID);
            _forceTeamID   = EditorGUILayout.IntField("Team ID",   _forceTeamID);
            if (GUILayout.Button("Asignar ahora"))
            {
                var c = ctx as GameModeContext;
                c?._teams.Assign(_forcePlayerID, _forceTeamID);
            }
            if (GUILayout.Button("Auto-asignar todos"))
                ActiveGM.AssignAllPlayersToTeams();
        }

        // ── Tab: Logs ─────────────────────────────────────────

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(380));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);
            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        // ── Helpers ───────────────────────────────────────────

        private bool Check()
        {
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return false; }

            if (ActiveGM == null)
            {
                EditorGUILayout.HelpBox(
                    "GameModeBase no encontrado.\n" +
                    "Pulsa 'Auto Find' o asigna el GameModeManager.",
                    MessageType.Warning);
                return false;
            }
            return true;
        }

        private void Log(string msg)
        {
            _logs.Add($"<color=#888>{System.DateTime.Now:HH:mm:ss.ff}</color> {msg}");
            if (_logs.Count > MAX) _logs.RemoveAt(0);
        }
    }
}
#endif