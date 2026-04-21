// ============================================================
//  PlayerEditorWindow.cs
//  PlayerSystem/Editor/PlayerEditorWindow.cs
//
//  Herramienta de debug del Player System en el Editor.
//
//  ABRIR: Window > Player > Player Debug Window
//
//  TABS:
//  • Stats:     Vida, velocidad, estado en tiempo real
//  • Inputs:    Visualización del input actual (move, look, botones)
//  • Movement:  Velocity, grounded, jump state, teleport
//  • Health:    HP slider, aplicar daño/curación, matar
//  • Respawn:   Estado, forzar respawn, spawn points
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using Player.Controller;
using Player.Events;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace Player.Editor
{
    public class PlayerEditorWindow : EditorWindow
    {
        // ── Layout ────────────────────────────────────────────

        private int     _selectedTab;
        private readonly string[] _tabs = { "Stats", "Input", "Movement", "Health", "Respawn" };
        private Vector2 _scroll;
        private Vector2 _logScroll;

        // ── Selección de jugador ──────────────────────────────

        private PlayerController _selectedPlayer;

        // ── Live Logs ─────────────────────────────────────────

        private readonly List<string> _logs = new();
        private const int MAX_LOGS = 40;
        private bool _autoScroll = true;
        private bool _subscribed;

        // ── Inputs temporales para Health ─────────────────────

        private float _damageAmount = 25f;
        private float _healAmount   = 25f;

        // ── Apertura ──────────────────────────────────────────

        [MenuItem("Window/Player/Player Debug Window")]
        public static void Open()
        {
            var w = GetWindow<PlayerEditorWindow>("Player Debug");
            w.minSize = new Vector2(420, 500);
            w.Show();
        }

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeEvents();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) SubscribeEvents();
            if (state == PlayModeStateChange.ExitingPlayMode) { UnsubscribeEvents(); _logs.Clear(); }
        }

        private void SubscribeEvents()
        {
            if (_subscribed) return;
            EventBus<PlayerDamagedEvent>.Subscribe(e   => AddLog($"<color=red>[DMG]</color> P{e.PlayerID} -{e.Amount:F0} HP → {e.RemainingHealth:F0}"));
            EventBus<PlayerHealedEvent>.Subscribe(e    => AddLog($"<color=green>[HEAL]</color> P{e.PlayerID} +{e.Amount:F0} HP → {e.CurrentHealth:F0}"));
            EventBus<PlayerDiedEvent>.Subscribe(e      => AddLog($"<color=orange>[DIED]</color> P{e.PlayerID} por P{e.KillerID}"));
            EventBus<PlayerRespawnedEvent>.Subscribe(e => AddLog($"<color=cyan>[SPAWN]</color> P{e.PlayerID} en {e.SpawnPosition}"));
            EventBus<PlayerJumpedEvent>.Subscribe(e    => AddLog($"<color=yellow>[JUMP]</color> P{e.PlayerID}"));
            EventBus<PlayerLandedEvent>.Subscribe(e    => AddLog($"[LAND] P{e.PlayerID} caída={e.FallDistance:F1}u"));
            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed) return;
            EventBus<PlayerDamagedEvent>.Clear();
            EventBus<PlayerHealedEvent>.Clear();
            EventBus<PlayerDiedEvent>.Clear();
            EventBus<PlayerRespawnedEvent>.Clear();
            EventBus<PlayerJumpedEvent>.Clear();
            EventBus<PlayerLandedEvent>.Clear();
            _subscribed = false;
        }

        // ── GUI ───────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_selectedTab)
            {
                case 0: DrawStatsTab();    break;
                case 1: DrawInputTab();    break;
                case 2: DrawMovementTab(); break;
                case 3: DrawHealthTab();   break;
                case 4: DrawRespawnTab();  break;
            }

            EditorGUILayout.EndScrollView();

            // Live log al fondo
            DrawLiveLog();

            if (Application.isPlaying)
                Repaint();
        }

        // ── Header ────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🎮 Player Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Selector de jugador
            _selectedPlayer = (PlayerController)EditorGUILayout.ObjectField(
                _selectedPlayer, typeof(PlayerController), true, GUILayout.Width(180)
            );

            // Auto-find
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
                _selectedPlayer = FindFirstObjectByType<PlayerController>();

            GUI.color = Application.isPlaying ? Color.green : Color.gray;
            GUILayout.Label(Application.isPlaying ? "● PLAY" : "● EDIT", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Stats ────────────────────────────────────────

        private void DrawStatsTab()
        {
            if (!CheckPlayer()) return;
            var p = _selectedPlayer;

            EditorGUILayout.LabelField("Información del Jugador", EditorStyles.boldLabel);

            var auth = p.Authority;
            EditorGUILayout.LabelField($"Player ID:      {auth?.PlayerID}");
            EditorGUILayout.LabelField($"IsLocalPlayer:  {auth?.IsLocalPlayer}");
            EditorGUILayout.LabelField($"HasAuthority:   {auth?.HasAuthority}");
            EditorGUILayout.LabelField($"Network Mode:   {p.Config?.NetworkMode}");

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Vida", EditorStyles.boldLabel);

            var h = p.Health;
            if (h != null)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                    h.HealthPercent,
                    $"{h.CurrentHealth:F0} / {h.MaxHealth:F0}"
                );
                EditorGUILayout.LabelField($"Vivo:          {h.IsAlive}");
                EditorGUILayout.LabelField($"Invencible:    {h.IsInvincible}");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Movimiento", EditorStyles.boldLabel);

            var m = p.Movement;
            if (m != null)
            {
                EditorGUILayout.LabelField($"Velocidad:     {m.Velocity.magnitude:F2} u/s");
                EditorGUILayout.LabelField($"En suelo:      {m.IsGroundedReal}");
                EditorGUILayout.LabelField($"Corriendo:     {m.IsRunning}");
                EditorGUILayout.LabelField($"Agachado:      {m.IsCrouchingReal}");
            }
        }

        // ── Tab: Input ────────────────────────────────────────

        private void DrawInputTab()
        {
            if (!CheckPlayer()) return;

            var inp = _selectedPlayer.Input;
            if (inp == null) { EditorGUILayout.HelpBox("PlayerInput no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Estado del Input", EditorStyles.boldLabel);

            DrawVector2Field("Move Input",  inp.MoveInput);
            DrawVector2Field("Look Input",  inp.LookInput);

            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Sprinting", inp.IsSprinting);
            EditorGUILayout.Toggle("Crouching", inp.IsCrouching);
            EditorGUILayout.Toggle("Shoot Held", inp.IsShootHeld);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Control Manual", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Deshabilitar Input")) inp.DisableInput();
            if (GUILayout.Button("Habilitar Input"))    inp.EnableInput();
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Movement ─────────────────────────────────────

        private void DrawMovementTab()
        {
            if (!CheckPlayer()) return;

            var m = _selectedPlayer.Movement;
            if (m == null) { EditorGUILayout.HelpBox("PlayerMovement no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Velocidad y Estado", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Velocity", m.Velocity);
            EditorGUILayout.FloatField("Speed", m.Velocity.magnitude);
            EditorGUILayout.Toggle("Is Grounded", m.IsGroundedReal);
            EditorGUILayout.Toggle("Is Running",  m.IsRunning);
            EditorGUILayout.Toggle("Is Crouching",m.IsCrouchingReal);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Teleport", EditorStyles.boldLabel);

            if (GUILayout.Button("Teleport al origen (0,1,0)"))
                m.Teleport(new Vector3(0f, 1f, 0f), Quaternion.identity);

            if (GUILayout.Button("Aplicar impulso hacia arriba"))
                m.AddImpulse(new Vector3(0f, 8f, 0f));
        }

        // ── Tab: Health ───────────────────────────────────────

        private void DrawHealthTab()
        {
            if (!CheckPlayer()) return;

            var h = _selectedPlayer.Health;
            if (h == null) { EditorGUILayout.HelpBox("PlayerHealth no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Estado de Salud", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Current HP", h.CurrentHealth);
            EditorGUILayout.FloatField("Max HP",     h.MaxHealth);
            EditorGUILayout.Toggle("Is Alive",       h.IsAlive);
            EditorGUILayout.Toggle("Is Invincible",  h.IsInvincible);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Aplicar Daño", EditorStyles.boldLabel);
            _damageAmount = EditorGUILayout.Slider("Cantidad", _damageAmount, 1f, 200f);
            if (GUILayout.Button($"💥 Aplicar {_damageAmount:F0} de daño"))
                _selectedPlayer.RequestDamage(_damageAmount, -1, Vector3.zero, Vector3.up);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Curar", EditorStyles.boldLabel);
            _healAmount = EditorGUILayout.Slider("Cantidad", _healAmount, 1f, 200f);
            if (GUILayout.Button($"💚 Curar {_healAmount:F0}"))
                _selectedPlayer.RequestHeal(_healAmount);

            EditorGUILayout.Space(4);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("☠️ Matar (daño = MaxHP)"))
                _selectedPlayer.RequestDamage(h.MaxHealth + 9999f, -1, Vector3.zero, Vector3.up);
            GUI.backgroundColor = Color.white;
        }

        // ── Tab: Respawn ──────────────────────────────────────

        private void DrawRespawnTab()
        {
            if (!CheckPlayer()) return;

            var r = _selectedPlayer.Respawn;
            if (r == null) { EditorGUILayout.HelpBox("PlayerRespawn no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Estado del Respawn", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Is Respawning", r.IsRespawning);
            EditorGUILayout.IntField("Respawn Count", r.RespawnCount);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            if (GUILayout.Button("🔄 Forzar Respawn Inmediato"))
                r.ForceRespawn();

            if (GUILayout.Button("📍 Respawn en origen (0,1,0)"))
                r.ForceRespawnAt(new Vector3(0f, 1f, 0f), Quaternion.identity);
        }

        // ── Live Log ──────────────────────────────────────────

        private void DrawLiveLog()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(100));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true };

            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);

            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        // ── Helpers ───────────────────────────────────────────

        private bool CheckPlayer()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entra en Play Mode.", MessageType.Info);
                return false;
            }
            if (_selectedPlayer == null)
            {
                EditorGUILayout.HelpBox("Selecciona un PlayerController arriba o pulsa Find.", MessageType.Warning);
                return false;
            }
            return true;
        }

        private void DrawVector2Field(string label, Vector2 value)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector2Field(label, value);
            EditorGUI.EndDisabledGroup();
        }

        private void AddLog(string msg)
        {
            string time = System.DateTime.Now.ToString("HH:mm:ss");
            _logs.Add($"<color=#888>{time}</color> {msg}");
            if (_logs.Count > MAX_LOGS) _logs.RemoveAt(0);
        }
    }
}
#endif
