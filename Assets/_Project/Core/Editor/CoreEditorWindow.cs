// ============================================================
//  CoreEditorWindow.cs
//  Core/Editor/CoreEditorWindow.cs
//
//  RESPONSABILIDAD ÚNICA: Herramienta de debug del Core en el Editor.
//
//  CARACTERÍSTICAS:
//  • Cambiar GameState en runtime con un click
//  • Controlar TimeScale con slider en vivo
//  • Activar/desactivar SlowMotion
//  • Ver historial de estados
//  • Logs en vivo con auto-scroll
//  • Reiniciar sistemas
//
//  ABRIR: Window > Core > Core Debug Window
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    public class CoreEditorWindow : EditorWindow
    {
        // ── Constantes de layout ──────────────────────────────

        private const int   MAX_LOGS     = 50;
        private const float PANEL_WIDTH  = 200f;

        // ── Estado interno ────────────────────────────────────

        private Vector2 _logScrollPos;
        private Vector2 _mainScrollPos;

        private readonly List<string> _liveLogs = new();
        private bool _autoScroll  = true;
        private bool _subscribed  = false;

        // TimeScale slider
        private float _targetTimeScale = 1f;

        // Tabs
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Estado", "Tiempo", "Escenas", "Logs" };

        // ── Apertura ──────────────────────────────────────────

        [MenuItem("Window/Core/Core Debug Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<CoreEditorWindow>("Core Debug");
            window.minSize = new Vector2(480, 400);
            window.Show();
        }

        // ── Lifecycle del Editor Window ───────────────────────

        private void OnEnable()
        {
            SubscribeToEvents();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                SubscribeToEvents();
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                UnsubscribeFromEvents();
                _liveLogs.Clear();
            }
        }

        // ── Suscripciones a eventos ───────────────────────────

        private void SubscribeToEvents()
        {
            if (_subscribed) return;

            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
            EventBus<TimeScaleChangedEvent>.Subscribe(OnTimeScaleChanged);
            EventBus<GameInitializedEvent>.Subscribe(OnGameInitialized);

            _subscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_subscribed) return;

            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
            EventBus<TimeScaleChangedEvent>.Unsubscribe(OnTimeScaleChanged);
            EventBus<GameInitializedEvent>.Unsubscribe(OnGameInitialized);

            _subscribed = false;
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnStateChanged(GameStateChangedEvent e)
        {
            AddLog($"<color=cyan>[State]</color> {e.Previous} → <b>{e.Current}</b>");
            Repaint();
        }

        private void OnSceneLoaded(SceneLoadedEvent e)
        {
            AddLog($"<color=green>[Scene]</color> Cargada: <b>{e.SceneName}</b> (Additive={e.IsAdditive})");
            Repaint();
        }

        private void OnTimeScaleChanged(TimeScaleChangedEvent e)
        {
            AddLog($"<color=yellow>[Time]</color> TimeScale: {e.PreviousScale:F2} → {e.NewScale:F2}");
            Repaint();
        }

        private void OnGameInitialized(GameInitializedEvent e)
        {
            AddLog($"<color=lime>[Core]</color> Sistema inicializado en t={e.Timestamp:F2}s");
            Repaint();
        }

        // ── Draw ──────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space(4);

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            switch (_selectedTab)
            {
                case 0: DrawStateTab();   break;
                case 1: DrawTimeTab();    break;
                case 2: DrawSceneTab();   break;
                case 3: DrawLogsTab();    break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Header ────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("🎮 Core Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            bool playing = Application.isPlaying;
            GUI.color = playing ? Color.green : Color.red;
            GUILayout.Label(playing ? "● PLAYING" : "● EDITOR", EditorStyles.miniLabel);
            GUI.color = Color.white;

            if (playing && GameManager.Instance != null)
            {
                GUILayout.Label(
                    $"| State: {GameManager.Instance.StateManager.CurrentState}",
                    EditorStyles.miniLabel
                );
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Estado ───────────────────────────────────────

        private void DrawStateTab()
        {
            EditorGUILayout.LabelField("Cambiar GameState", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode para controlar el estado.", MessageType.Info);
                return;
            }

            if (GameManager.Instance == null)
            {
                EditorGUILayout.HelpBox("GameManager.Instance es null.", MessageType.Warning);
                return;
            }

            var sm = GameManager.Instance.StateManager;

            // Estado actual
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Estado Actual", sm.CurrentState);
            EditorGUILayout.EnumPopup("Estado Anterior", sm.PreviousState);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Solicitar Cambio:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawStateButton(GameState.MainMenu,  sm);
            DrawStateButton(GameState.Lobby,     sm);
            DrawStateButton(GameState.Playing,   sm);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawStateButton(GameState.Paused,   sm);
            DrawStateButton(GameState.GameOver, sm);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("⚠️ Forzar → MainMenu (ignora validación)"))
                sm.ForceStateChange(GameState.MainMenu);

            // Historial
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Historial de Estados:", EditorStyles.boldLabel);

            var history = sm.History;
            for (int i = history.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField($"  {history.Count - i - 1}. {history[i]}");
        }

        private void DrawStateButton(GameState state, GameStateManager sm)
        {
            bool isCurrent = sm.CurrentState == state;
            GUI.backgroundColor = isCurrent ? Color.cyan : Color.white;
            if (GUILayout.Button(state.ToString()))
                sm.RequestStateChange(state);
            GUI.backgroundColor = Color.white;
        }

        // ── Tab: Tiempo ───────────────────────────────────────

        private void DrawTimeTab()
        {
            EditorGUILayout.LabelField("Control de Tiempo", EditorStyles.boldLabel);

            if (!Application.isPlaying || GameManager.Instance == null)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode para controlar el tiempo.", MessageType.Info);
                return;
            }

            var tm = GameManager.Instance.TimeManager;

            // Estado
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("TimeScale Actual", tm.CurrentTimeScale);
            EditorGUILayout.Toggle("Pausado", tm.IsPaused);
            EditorGUILayout.Toggle("SlowMotion Activo", tm.IsSlowMotionActive);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // Slider de TimeScale
            EditorGUILayout.LabelField("Ajustar TimeScale:");
            _targetTimeScale = EditorGUILayout.Slider(_targetTimeScale, 0f, 5f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Aplicar"))        tm.SetTimeScale(_targetTimeScale);
            if (GUILayout.Button("Reset (1x)"))     { _targetTimeScale = 1f; tm.ResetTimeScale(); }
            if (GUILayout.Button("Fast (2x)"))      { _targetTimeScale = 2f; tm.SetTimeScale(2f); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Pausa:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏸ Pausar"))       tm.Pause();
            if (GUILayout.Button("▶ Reanudar"))     tm.Resume();
            if (GUILayout.Button("⏯ Toggle"))       tm.TogglePause();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Slow Motion:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🐌 Slow (0.25x, 2s)")) tm.SetSlowMotion(0.25f, 2f);
            if (GUILayout.Button("🛑 Stop SlowMo"))       tm.StopSlowMotion();
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Escenas ──────────────────────────────────────

        private void DrawSceneTab()
        {
            EditorGUILayout.LabelField("Control de Escenas", EditorStyles.boldLabel);

            if (!Application.isPlaying || GameManager.Instance == null)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode para controlar las escenas.", MessageType.Info);
                return;
            }

            var sl = GameManager.Instance.SceneLoader;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Escena Actual", sl.CurrentScene);
            EditorGUILayout.Toggle("Cargando", sl.IsLoading);
            EditorGUILayout.Slider("Progreso", sl.LoadProgress, 0f, 1f);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Cargar Escenas Configuradas:", EditorStyles.boldLabel);

            var config = GameManager.Instance.StateManager != null
                ? Resources.Load<Core.Config.CoreConfig>("CoreConfig")
                : null;

            if (GUILayout.Button("📋 MainMenu"))  sl.LoadScene("MainMenu");
            if (GUILayout.Button("🏟 Lobby"))      sl.LoadScene("Lobby");
            if (GUILayout.Button("🎮 Game"))       sl.LoadScene("Game");

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🔄 Recargar Escena Actual"))
                sl.ReloadCurrentScene();
        }

        // ── Tab: Logs ─────────────────────────────────────────

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Logs en Vivo ({_liveLogs.Count}/{MAX_LOGS})", EditorStyles.boldLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto-scroll", _autoScroll, GUILayout.Width(90));
            if (GUILayout.Button("Limpiar", GUILayout.Width(60)))
                _liveLogs.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            _logScrollPos = EditorGUILayout.BeginScrollView(
                _logScrollPos,
                GUILayout.Height(position.height - 120)
            );

            var style = new GUIStyle(EditorStyles.label)
            {
                richText  = true,
                wordWrap  = true,
                fontSize  = 11
            };

            for (int i = _liveLogs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_liveLogs[i], style);

            if (_autoScroll)
                _logScrollPos = new Vector2(0, float.MaxValue);

            EditorGUILayout.EndScrollView();
        }

        // ── Utilidades ────────────────────────────────────────

        private void AddLog(string message)
        {
            string time = System.DateTime.Now.ToString("HH:mm:ss");
            _liveLogs.Add($"<color=#888888>{time}</color> {message}");

            if (_liveLogs.Count > MAX_LOGS)
                _liveLogs.RemoveAt(0);
        }
    }
}
#endif
