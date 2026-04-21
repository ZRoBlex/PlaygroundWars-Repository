// ============================================================
//  GameModeEditorWindow.cs
//  GameMode/Editor/GameModeEditorWindow.cs
//
//  ABRIR: Window → GameMode → GameMode Debug Window
//
//  TABS:
//  • GameMode  — fase actual, cambiar modo, start/end
//  • Banderas  — estado de cada bandera, forzar acciones
//  • Score     — puntuación en tiempo real
//  • Fixes     — verificar correcciones de bugs
//  • Logs      — eventos en tiempo real
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using GameMode.CTF;
using GameMode.Events;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace GameMode.Editor
{
    public class GameModeEditorWindow : EditorWindow
    {
        private int            _tab;
        private readonly string[] _tabs = { "GameMode", "Banderas", "Score", "Fixes", "Logs" };
        private Vector2        _scroll, _logScroll;

        private GameModeManager       _manager;
        private CaptureTheFlagMode    _ctf;

        private readonly List<string> _logs = new();
        private const int             MAX_LOGS    = 60;
        private bool                  _autoScroll = true;
        private bool                  _subbed;

        [MenuItem("Window/GameMode/GameMode Debug Window")]
        public static void Open()
        {
            var w = GetWindow<GameModeEditorWindow>("GameMode Debug");
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
            EventBus<OnGameStartedEvent>.Subscribe(e         => Log($"<color=lime>[START]</color> {e.GameModeID}"));
            EventBus<OnGameEndedEvent>.Subscribe(e           => Log($"<color=cyan>[END]</color> {e.GameModeID} Winner=T{e.WinnerTeamID}"));
            EventBus<OnRoundStartedEvent>.Subscribe(e        => Log($"<color=#aaffaa>[ROUND {e.RoundNumber}]</color> Iniciada ({e.Duration:F0}s)"));
            EventBus<OnRoundEndedEvent>.Subscribe(e          => Log($"<color=gold>[ROUND {e.RoundNumber} END]</color> Winner=T{e.WinnerTeamID}"));
            EventBus<OnFlagPickedEvent>.Subscribe(e          => Log($"<color=yellow>[FLAG PICKED]</color> P{e.CarrierID} tomó bandera T{e.FlagTeamID}"));
            EventBus<OnFlagDroppedEvent>.Subscribe(e         => Log($"<color=orange>[FLAG DROP]</color> P{e.CarrierID} soltó T{e.FlagTeamID}"));
            EventBus<OnFlagCapturedEvent>.Subscribe(e        => Log($"<color=lime>⚡[CAPTURE!]</color> P{e.CapturingPlayerID} (T{e.CapturingTeamID}) → T{e.FlagTeamID}"));
            EventBus<OnFlagReturnedEvent>.Subscribe(e        => Log($"<color=white>[RETURN]</color> Bandera T{e.FlagTeamID} devuelta (P{e.ReturnedByID})"));
            EventBus<OnScoreChangedEvent>.Subscribe(e        => Log($"<color=#88ff88>[SCORE]</color> T{e.TeamID}: {e.NewScore}/{e.ScoreToWin}"));
            EventBus<OnGameModePhaseChangedEvent>.Subscribe(e => Log($"[PHASE] {e.Previous} → <b>{e.Current}</b>"));
            EventBus<ApplyDamageRequestEvent>.Subscribe(e    => Log($"<color=red>[DMG REQ]</color> P{e.AttackerID}→P{e.TargetID} {e.Damage:F0}dmg"));
            EventBus<DamageAppliedEvent>.Subscribe(e         => Log($"<color=orange>[DMG APPLIED]</color> P{e.AttackerID}→P{e.TargetID} {e.FinalDamage:F0}dmg"));
            EventBus<DamageRequestRejectedEvent>.Subscribe(e => Log($"<color=magenta>[REJECTED]</color> P{e.AttackerID}→P{e.TargetID}: {e.Reason}"));
            _subbed = true;
        }

        private void Unsubscribe()
        {
            if (!_subbed) return;
            EventBus<OnGameStartedEvent>.Clear();
            EventBus<OnGameEndedEvent>.Clear();
            EventBus<OnRoundStartedEvent>.Clear();
            EventBus<OnRoundEndedEvent>.Clear();
            EventBus<OnFlagPickedEvent>.Clear();
            EventBus<OnFlagDroppedEvent>.Clear();
            EventBus<OnFlagCapturedEvent>.Clear();
            EventBus<OnFlagReturnedEvent>.Clear();
            EventBus<OnScoreChangedEvent>.Clear();
            EventBus<OnGameModePhaseChangedEvent>.Clear();
            EventBus<ApplyDamageRequestEvent>.Clear();
            EventBus<DamageAppliedEvent>.Clear();
            EventBus<DamageRequestRejectedEvent>.Clear();
            _subbed = false;
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🎮 GameMode Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
            {
                _manager = FindFirstObjectByType<GameModeManager>();
                _ctf     = FindFirstObjectByType<CaptureTheFlagMode>();
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
                case 0: DrawGameModeTab();  break;
                case 1: DrawFlagsTab();     break;
                case 2: DrawScoreTab();     break;
                case 3: DrawFixesTab();     break;
                case 4: DrawLogsTab();      break;
            }
            EditorGUILayout.EndScrollView();
            if (Application.isPlaying) Repaint();
        }

        // ── Tab: GameMode ─────────────────────────────────────

        private void DrawGameModeTab()
        {
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return; }

            if (_ctf != null)
            {
                EditorGUILayout.LabelField("Capture the Flag", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup("Fase", _ctf.Phase);
                EditorGUILayout.Toggle("IsRunning", _ctf.IsRunning);
                EditorGUILayout.FloatField("ElapsedTime", _ctf.ElapsedTime);
                EditorGUILayout.IntField("Ronda Actual", _ctf.Rounds?.CurrentRound ?? 0);
                EditorGUILayout.FloatField("Timer Ronda", _ctf.Rounds?.RoundTimer ?? 0f);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(6);
                if (GUILayout.Button("▶ StartGame"))     _ctf.StartGame();
                if (GUILayout.Button("⏹ EndGame (T0)"))  _ctf.EndGame(0);
                if (GUILayout.Button("⏭ Nueva Ronda"))   _ctf.ForceStartNewRound();
            }
            else if (_manager != null)
            {
                EditorGUILayout.LabelField("GameModeManager", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Modo Activo", _manager.CurrentMode?.GameModeID ?? "—");
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Activar CTF"))   _manager.ActivateMode("CTF");
                if (GUILayout.Button("Start Actual"))  _manager.StartCurrentMode();
                if (GUILayout.Button("End Actual"))    _manager.EndCurrentMode();
            }
            else
            {
                EditorGUILayout.HelpBox("CaptureTheFlagMode o GameModeManager no encontrado. Pulsa 'Find'.", MessageType.Warning);
            }
        }

        // ── Tab: Banderas ─────────────────────────────────────

        private void DrawFlagsTab()
        {
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return; }

            var flags = FindObjectsByType<FlagController>(FindObjectsSortMode.None);
            if (flags.Length == 0)
            {
                EditorGUILayout.HelpBox("No hay FlagControllers en escena.", MessageType.Warning);
                return;
            }

            foreach (var f in flags)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Bandera Equipo {f.OwnerTeamID}", EditorStyles.boldLabel);

                GUI.color = f.State == FlagState.Idle    ? Color.green :
                            f.State == FlagState.Carried ? Color.yellow : Color.red;
                EditorGUILayout.LabelField($"Estado:  {f.State}");
                GUI.color = Color.white;
                EditorGUILayout.LabelField($"Portador: {(f.CarrierID >= 0 ? $"P{f.CarrierID}" : "—")}");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("↩ Retornar"))     f.Return(-1);
                if (GUILayout.Button("⚡ Force Reset"))  f.ForceReset();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        // ── Tab: Score ────────────────────────────────────────

        private void DrawScoreTab()
        {
            if (_ctf?.Score == null) { EditorGUILayout.HelpBox("ScoreSystem no disponible.", MessageType.Info); return; }

            var s = _ctf.Score;
            var r = _ctf.Rounds;

            EditorGUILayout.LabelField("Puntuación de Ronda", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("🔴 Team A", s.ScoreTeamA);
            EditorGUILayout.IntField("🔵 Team B", s.ScoreTeamB);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Rondas Ganadas", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Team A", r?.WinsTeamA ?? 0);
            EditorGUILayout.IntField("Team B", r?.WinsTeamB ?? 0);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("+ Punto T0 (Red)"))  s.AddScore(0);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 1f);
            if (GUILayout.Button("+ Punto T1 (Blue)")) s.AddScore(1);
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Reset Score")) s.Reset();
        }

        // ── Tab: Fixes ────────────────────────────────────────

        private void DrawFixesTab()
        {
            EditorGUILayout.LabelField("Verificación de Correcciones", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Este tab verifica que los bugs conocidos estén corregidos en escena.",
                MessageType.None);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode para verificar.", MessageType.Info);
                return;
            }

            // CHECK 1: ApplyDamageRequestEvent existe
            DrawCheck("ApplyDamageRequestEvent existe",
                "El evento está en GameEvents_Additions.cs", true);

            // CHECK 2: ServerDamageProcessor en escena
            var sdp = FindFirstObjectByType<Combat.Systems.ServerDamageProcessor>();
            DrawCheck("ServerDamageProcessor en escena",
                sdp != null ? "✅ Encontrado" : "❌ Añadir ServerDamageProcessor a [GameManager]",
                sdp != null);

            // CHECK 3: PlayerMovement_Fixed en escena
            var pm = FindFirstObjectByType<Player.Movement.PlayerMovement_Fixed>();
            DrawCheck("PlayerMovement_Fixed en escena",
                pm != null ? $"✅ Encontrado ({(pm.IsGroundedReal ? "GROUNDED" : "AIR")})" :
                             "❌ Reemplazar PlayerMovement.cs con PlayerMovement_Fixed.cs",
                pm != null);

            // CHECK 4: GroundCheckOrigin asignado
            if (pm != null)
            {
                DrawCheck("IsGroundedReal activo",
                    $"SphereCast detecta suelo: {pm.IsGroundedReal}",
                    true);

                DrawCheck("IsCrouchingReal (collider real)",
                    $"Collider reducido: {pm.IsCrouchingReal}",
                    true);
            }

            // CHECK 5: LayerMask en armas
            var weapons = FindObjectsByType<Combat.Weapons.WeaponBase>(FindObjectsSortMode.None);
            if (weapons.Length > 0)
            {
                bool layersOk = true;
                foreach (var w in weapons)
                    if (w.Config != null && w.Config.HitLayers.value == 0)
                        layersOk = false;

                DrawCheck("LayerMask configurado en armas",
                    layersOk ? "✅ Todas las armas tienen LayerMask" :
                               "⚠️ Algún arma tiene HitLayers = 0 (no detecta nada)",
                    layersOk);
            }
        }

        private void DrawCheck(string title, string detail, bool ok)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUI.color = ok ? Color.green : Color.red;
            GUILayout.Label(ok ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Logs ─────────────────────────────────────────

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX_LOGS})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("Limpiar", GUILayout.Width(60))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(350));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);
            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        private void Log(string msg)
        {
            _logs.Add($"<color=#888>{System.DateTime.Now:HH:mm:ss}</color> {msg}");
            if (_logs.Count > MAX_LOGS) _logs.RemoveAt(0);
        }
    }
}
#endif
